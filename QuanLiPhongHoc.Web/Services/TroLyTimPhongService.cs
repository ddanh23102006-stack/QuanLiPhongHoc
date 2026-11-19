using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

/// <summary>Một phòng còn trống mà trợ lý gợi ý, kèm khung giờ nên điền sẵn vào phiếu.</summary>
public class TroLyPhongVm
{
    public int MaPhong { get; init; }
    public string CodePhong { get; init; } = string.Empty;
    public string? Toa { get; init; }
    public string LoaiPhong { get; init; } = string.Empty;
    public string LoaiPhongTen { get; init; } = string.Empty;
    public int SucChua { get; init; }
    public IReadOnlyList<string> ThietBi { get; init; } = [];
    public IReadOnlyList<string> KhungTrong { get; init; } = [];
    public string? Tu { get; init; }
    public string? Den { get; init; }
    public string? GhiChu { get; init; }
}

/// <summary>Phương án thay thế khi không có phòng trống đúng yêu cầu.</summary>
public class TroLyGoiYVm
{
    public int MaPhong { get; init; }
    public string CodePhong { get; init; } = string.Empty;
    public string? Toa { get; init; }
    public int SucChua { get; init; }
    public IReadOnlyList<string> ThietBi { get; init; } = [];
    public string Tu { get; init; } = string.Empty;
    public string Den { get; init; } = string.Empty;
    public string MoTa { get; init; } = string.Empty;
}

public class TroLyKetQuaVm
{
    public string TraLoi { get; set; } = string.Empty;
    public string Ngay { get; set; } = string.Empty;
    public string NgayHienThi { get; set; } = string.Empty;
    public string? Tu { get; set; }
    public string? Den { get; set; }
    public int? MaToaNha { get; set; }
    public string? LoaiPhong { get; set; }
    public int? SucChua { get; set; }
    public string? TuKhoaPhong { get; set; }
    public string? Buoi { get; set; }
    public List<string> Hieu { get; set; } = [];
    public List<string> CanhBao { get; set; } = [];
    public List<TroLyPhongVm> Phong { get; set; } = [];
    public List<TroLyGoiYVm> GoiY { get; set; } = [];
}

public interface ITroLyTimPhongService
{
    Task<TroLyKetQuaVm> HoiAsync(string cauHoi, DateOnly homNay);
}

/// <summary>
/// Trợ lý tìm phòng chạy cục bộ: đọc câu tiếng Việt thành bộ lọc, tra cứu qua
/// <see cref="IPhongTraCuuService"/> (đã tính sẵn lịch học + phiếu đang giữ chỗ)
/// nên không gọi dịch vụ ngoài, không cần API key và trả kết quả ngay.
/// </summary>
public class TroLyTimPhongService : ITroLyTimPhongService
{
    private const int SoPhongToiDa = 8;
    private const int SoGoiYToiDa = 4;

    private readonly IPhongTraCuuService _traCuu;
    private readonly QuanLiPhongHocContext _db;

    public TroLyTimPhongService(IPhongTraCuuService traCuu, QuanLiPhongHocContext db)
    {
        _traCuu = traCuu;
        _db = db;
    }

    public async Task<TroLyKetQuaVm> HoiAsync(string cauHoi, DateOnly homNay)
    {
        var toaNhas = await _db.ToaNhas.AsNoTracking()
            .OrderBy(x => x.CodeToaNha)
            .ToListAsync();
        var y = BoPhanTich.PhanTich(cauHoi, homNay, toaNhas);

        var ketQua = new TroLyKetQuaVm
        {
            Ngay = y.Ngay.ToString("yyyy-MM-dd"),
            NgayHienThi = $"{BoPhanTich.ThuTrongTuan(y.Ngay)}, {y.Ngay:dd/MM/yyyy}",
            Tu = y.Tu?.ToString("HH\\:mm"),
            Den = y.Den?.ToString("HH\\:mm"),
            MaToaNha = y.MaToaNha,
            LoaiPhong = y.LoaiPhong,
            SucChua = y.SucChua,
            TuKhoaPhong = y.PhongCode,
            Buoi = y.Buoi,
            Hieu = y.MoTa,
            CanhBao = y.CanhBao
        };

        var filter = new PhongTraCuuFilter
        {
            Ngay = y.Ngay,
            Tu = y.Tu,
            Den = y.Den,
            Q = y.PhongCode,
            MaToaNha = y.MaToaNha,
            LoaiPhong = y.LoaiPhong,
            TrangThai = Catalogs.PhongTrong
        };

        var khop = Loc(await _traCuu.TraCuuAsync(filter), y);
        if (khop.Count > 0)
        {
            ketQua.Phong = khop.Take(SoPhongToiDa).Select(p => TaoThe(p, y)).ToList();
            ketQua.TraLoi = TaoTraLoiCoPhong(khop.Count, y);
            if (khop.Count > SoPhongToiDa)
                ketQua.CanhBao.Add($"Còn {khop.Count - SoPhongToiDa} phòng phù hợp khác — bạn nới bộ lọc để xem thêm.");
            return ketQua;
        }

        if (y.PhongCode is not null)
            await GiaiThichPhongBiChanAsync(ketQua, y);

        var goiY = await TimGoiYAsync(y);
        foreach (var g in goiY)
            if (ketQua.GoiY.All(x => x.MaPhong != g.MaPhong))
                ketQua.GoiY.Add(g);
        ketQua.GoiY = ketQua.GoiY.Take(SoGoiYToiDa).ToList();

        ketQua.TraLoi = TaoTraLoiKhongCoPhong(y, ketQua.GoiY.Count);
        return ketQua;
    }

    /// <summary>Lọc bổ sung các điều kiện mà bảng tra cứu phòng chưa xử lý (sức chứa, thiết bị, buổi).</summary>
    private static List<PhongTraCuuVm> Loc(IEnumerable<PhongTraCuuVm> ds, YeuCauTroLy y) =>
        ds.Where(p => y.SucChua is null || p.SucChua >= y.SucChua)
          .Where(p => BoPhanTich.KhopThietBi(p.ThietBi, y.ThietBi))
          .Where(p => y.Tu is not null || BoPhanTich.KhopBuoi(p.KhungConTrong, y.Buoi))
          .OrderBy(p => y.SucChua is null ? 0 : p.SucChua)
          .ThenBy(p => p.CodePhong)
          .ToList();

    private static TroLyPhongVm TaoThe(PhongTraCuuVm p, YeuCauTroLy y)
    {
        var (tu, den) = BoPhanTich.GioDeXuat(p.KhungConTrong, y);
        return new TroLyPhongVm
        {
            MaPhong = p.MaPhong,
            CodePhong = p.CodePhong,
            Toa = p.Toa,
            LoaiPhong = p.LoaiPhong,
            LoaiPhongTen = Catalogs.LabelLoaiPhong(p.LoaiPhong),
            SucChua = p.SucChua,
            ThietBi = p.ThietBi,
            KhungTrong = p.KhungConTrong.Select(k => k.MoTa).ToList(),
            Tu = tu?.ToString("HH\\:mm"),
            Den = den?.ToString("HH\\:mm"),
            GhiChu = y.Tu is not null
                ? "Trống đúng khung giờ bạn cần"
                : tu is not null ? "Gợi ý khung giờ" : null
        };
    }

    /// <summary>
    /// Người dùng nêu đích danh một phòng nhưng phòng đó không trả về: giải thích lý do
    /// (bảo trì, trùng lịch, trùng phiếu, thiếu chỗ/thiết bị) và đề xuất khung còn trống.
    /// </summary>
    private async Task GiaiThichPhongBiChanAsync(TroLyKetQuaVm ketQua, YeuCauTroLy y)
    {
        var tatCa = await _traCuu.TraCuuAsync(new PhongTraCuuFilter
        {
            Ngay = y.Ngay,
            Tu = y.Tu,
            Den = y.Den,
            Q = y.PhongCode,
            TrangThai = null
        });
        var p = tatCa.FirstOrDefault(x =>
            string.Equals(x.CodePhong, y.PhongCode, StringComparison.OrdinalIgnoreCase));
        if (p is null)
        {
            ketQua.CanhBao.Add($"Không tìm thấy phòng {y.PhongCode} trong hệ thống.");
            return;
        }

        if (p.TrangThai == Catalogs.PhongBaoTri)
        {
            ketQua.CanhBao.Add($"Phòng {p.CodePhong} đang {Catalogs.LabelTrangThaiPhong(p.TrangThaiPhong).ToLower()} nên không nhận đăng ký.");
            return;
        }

        if (p.TrangThai == Catalogs.PhongBan && p.LyDoBan is not null)
        {
            ketQua.CanhBao.Add($"Phòng {p.CodePhong} không trống: {p.LyDoBan}");
        }
        else if (y.Tu is null && y.Buoi is not null && !BoPhanTich.KhopBuoi(p.KhungConTrong, y.Buoi))
        {
            // Tra cứu cả ngày không gắn lý do bận, nên hỏi lại đúng khung của buổi
            // để chỉ ra môn học / phiếu đang giữ chỗ thay vì chỉ báo "không trống".
            var period = BoPhanTich.BuoiPeriod(y.Buoi);
            var theoBuoi = await _traCuu.TraCuuAsync(new PhongTraCuuFilter
            {
                Ngay = y.Ngay,
                Tu = period.Tu,
                Den = period.Den,
                Q = y.PhongCode,
                TrangThai = null
            });
            var lyDo = theoBuoi
                .FirstOrDefault(x => string.Equals(x.CodePhong, y.PhongCode, StringComparison.OrdinalIgnoreCase))
                ?.LyDoBan;
            var tenBuoi = BoPhanTich.TenBuoi(y.Buoi).ToLower();
            ketQua.CanhBao.Add(lyDo is null
                ? $"Phòng {p.CodePhong} không còn trống buổi {tenBuoi}."
                : $"Phòng {p.CodePhong} không trống buổi {tenBuoi}: {lyDo}");
        }

        if (y.SucChua is int sc && p.SucChua < sc)
            ketQua.CanhBao.Add($"Phòng {p.CodePhong} chỉ có {p.SucChua} chỗ, ít hơn {sc} chỗ bạn cần.");
        if (!BoPhanTich.KhopThietBi(p.ThietBi, y.ThietBi))
            ketQua.CanhBao.Add($"Phòng {p.CodePhong} không có đủ thiết bị bạn yêu cầu.");

        var khung = BoPhanTich.KhungGanNhat(p.KhungConTrong, y);
        if (khung is not null)
        {
            ketQua.GoiY.Add(new TroLyGoiYVm
            {
                MaPhong = p.MaPhong,
                CodePhong = p.CodePhong,
                Toa = p.Toa,
                SucChua = p.SucChua,
                ThietBi = p.ThietBi,
                Tu = khung.Value.Khung.Tu.ToString("HH\\:mm"),
                Den = khung.Value.Khung.Den.ToString("HH\\:mm"),
                MoTa = $"Phòng {p.CodePhong} còn trống {khung.Value.Khung.MoTa}"
            });
        }
    }

    private async Task<List<TroLyGoiYVm>> TimGoiYAsync(YeuCauTroLy y)
    {
        var ketQua = new List<TroLyGoiYVm>();
        var daCo = new HashSet<int>();

        var cungNgay = await _traCuu.TraCuuAsync(new PhongTraCuuFilter
        {
            Ngay = y.Ngay,
            MaToaNha = y.MaToaNha,
            LoaiPhong = y.LoaiPhong,
            TrangThai = Catalogs.PhongTrong
        });
        ThemGoiYTheoKhung(ketQua, daCo, cungNgay, y);

        if (ketQua.Count == 0 && (y.MaToaNha is not null || y.LoaiPhong is not null))
        {
            var khacToa = await _traCuu.TraCuuAsync(new PhongTraCuuFilter
            {
                Ngay = y.Ngay,
                Tu = y.Tu,
                Den = y.Den,
                TrangThai = Catalogs.PhongTrong
            });
            foreach (var p in Loc(khacToa, y))
            {
                if (!daCo.Add(p.MaPhong)) continue;
                var (tu, den) = BoPhanTich.GioDeXuat(p.KhungConTrong, y);
                if (tu is null || den is null) continue;
                ketQua.Add(TaoGoiY(p, tu.Value, den.Value,
                    p.Toa is null ? "Trống đúng khung giờ bạn cần" : $"Trống đúng khung giờ bạn cần tại {p.Toa}"));
                if (ketQua.Count >= SoGoiYToiDa) break;
            }
        }

        if (ketQua.Count == 0)
        {
            var toanTruong = await _traCuu.TraCuuAsync(new PhongTraCuuFilter
            {
                Ngay = y.Ngay,
                TrangThai = Catalogs.PhongTrong
            });
            ThemGoiYTheoKhung(ketQua, daCo, toanTruong, y);
        }

        return ketQua.Take(SoGoiYToiDa).ToList();
    }

    private static void ThemGoiYTheoKhung(
        List<TroLyGoiYVm> ketQua, HashSet<int> daCo, IEnumerable<PhongTraCuuVm> nguon, YeuCauTroLy y)
    {
        var ds = Loc(nguon, y)
            .Select(p => (p, khung: BoPhanTich.KhungGanNhat(p.KhungConTrong, y)))
            .Where(x => x.khung is not null)
            .OrderBy(x => x.khung!.Value.KhoangCach)
            .ThenBy(x => x.p.SucChua)
            .ThenBy(x => x.p.CodePhong);

        foreach (var (p, khung) in ds)
        {
            if (!daCo.Add(p.MaPhong)) continue;
            ketQua.Add(TaoGoiY(p, khung!.Value.Khung.Tu, khung.Value.Khung.Den,
                $"Còn trống {khung.Value.Khung.MoTa}"));
            if (ketQua.Count >= SoGoiYToiDa) break;
        }
    }

    private static TroLyGoiYVm TaoGoiY(PhongTraCuuVm p, TimeOnly tu, TimeOnly den, string moTa) => new()
    {
        MaPhong = p.MaPhong,
        CodePhong = p.CodePhong,
        Toa = p.Toa,
        SucChua = p.SucChua,
        ThietBi = p.ThietBi,
        Tu = tu.ToString("HH\\:mm"),
        Den = den.ToString("HH\\:mm"),
        MoTa = moTa
    };

    private static string TaoTraLoiCoPhong(int soPhong, YeuCauTroLy y) =>
        $"Mình hiểu bạn cần {MoTaNhuCau(y)}. Có {soPhong} phòng còn trống phù hợp — bấm \"Chọn mượn phòng này\" để mở phiếu với đúng ngày giờ đã hiểu.";

    private static string TaoTraLoiKhongCoPhong(YeuCauTroLy y, int soGoiY)
    {
        var sb = new StringBuilder();
        sb.Append($"Không có phòng nào trống {MoTaThoiGian(y)} ngày {y.Ngay:dd/MM/yyyy}");
        var dieuKien = MoTaDieuKien(y);
        if (!string.IsNullOrWhiteSpace(dieuKien)) sb.Append($" ({dieuKien})");
        sb.Append('.');
        sb.Append(soGoiY > 0
            ? $" Mình tìm được {soGoiY} phương án gần nhất bên dưới."
            : " Bạn thử đổi ngày, nới khung giờ hoặc bỏ bớt điều kiện về tòa nhà/loại phòng/thiết bị.");
        return sb.ToString();
    }

    private static string MoTaNhuCau(YeuCauTroLy y)
    {
        var dieuKien = MoTaDieuKien(y);
        return string.IsNullOrWhiteSpace(dieuKien)
            ? $"{MoTaThoiGian(y)} ngày {y.Ngay:dd/MM/yyyy}"
            : $"{MoTaThoiGian(y)} ngày {y.Ngay:dd/MM/yyyy}, {dieuKien}";
    }

    private static string MoTaThoiGian(YeuCauTroLy y) =>
        y.Tu is TimeOnly tu && y.Den is TimeOnly den
            ? $"từ {tu:HH\\:mm} đến {den:HH\\:mm}"
            : y.Buoi is not null
                ? $"buổi {BoPhanTich.TenBuoi(y.Buoi).ToLower()}"
                : "cả ngày";

    private static string? MoTaDieuKien(YeuCauTroLy y)
    {
        var ds = new List<string>();
        if (y.TenToa is not null) ds.Add(y.TenToa);
        if (y.LoaiPhong is not null) ds.Add(Catalogs.LabelLoaiPhong(y.LoaiPhong).ToLower());
        if (y.SucChua is int sc) ds.Add($"ít nhất {sc} chỗ");
        if (y.ThietBiTen.Count > 0) ds.Add("có " + string.Join(", ", y.ThietBiTen));
        return ds.Count == 0 ? null : string.Join(", ", ds);
    }
}

/// <summary>Kết quả đọc câu hỏi trước khi tra cứu phòng.</summary>
internal sealed class YeuCauTroLy
{
    public DateOnly Ngay { get; set; }
    public TimeOnly? Tu { get; set; }
    public TimeOnly? Den { get; set; }
    public string? Buoi { get; set; }
    public int? SucChua { get; set; }
    public int? MaToaNha { get; set; }
    public string? TenToa { get; set; }
    public string? LoaiPhong { get; set; }
    public string? PhongCode { get; set; }
    public List<string> ThietBi { get; } = [];
    public List<string> ThietBiTen { get; } = [];
    public List<string> MoTa { get; } = [];
    public List<string> CanhBao { get; } = [];
}

/// <summary>Bộ đọc câu tiếng Việt không dấu/có dấu thành các tham số tìm phòng.</summary>
internal static class BoPhanTich
{
    private static readonly Regex MaPhongRx = new(
        @"\b([a-z]{1,3}\d?)\s*[.\-]\s*(\d{2,4})\b", RegexOptions.Compiled);

    private static readonly Regex NgaySlashRx = new(
        @"(\d{1,2})\s*/\s*(\d{1,2})(?:\s*/\s*(\d{2,4}))?", RegexOptions.Compiled);

    private static readonly Regex NgayGachRx = new(
        @"ngay\s+(\d{1,2})\s*[-.]\s*(\d{1,2})(?:\s*[-.]\s*(\d{2,4}))?", RegexOptions.Compiled);

    private static readonly Regex KhoangGioRx = new(
        @"(?<!\d)(\d{1,2})\s*(?:h|gio|:)\s*(\d{1,2})?\s*(?:-|\u2013|den|toi(?=\s*\d))\s*(\d{1,2})\s*(?:h|gio|:)\s*(\d{1,2})?",
        RegexOptions.Compiled);

    private static readonly Regex KhoangGioGonRx = new(
        @"(?:tu\s+)?(\d{1,2})\s*(?:den|toi)\s*(\d{1,2})\s*(?:h|gio)\b", RegexOptions.Compiled);

    private static readonly Regex KhoangGioLungRx = new(
        @"(?:tu\s+)?(\d{1,2})\s*(?:h|gio)\s*(?:den|-)\s*(\d{1,2})\b", RegexOptions.Compiled);

    private static readonly Regex MotGioRx = new(
        @"(?<!\d)(\d{1,2})\s*(?:h|gio|:)\s*(\d{1,2})?\b", RegexOptions.Compiled);

    private static readonly (string Khoa, string Loc, string HienThi)[] ThietBiMap =
    [
        ("may chieu", "may chieu", "máy chiếu"),
        ("may dieu hoa", "dieu hoa", "điều hòa"),
        ("dieu hoa", "dieu hoa", "điều hòa"),
        ("may lanh", "dieu hoa", "điều hòa"),
        ("may tinh", "may tinh", "máy tính"),
        ("workstation", "may tinh", "máy tính"),
        ("micro", "micro", "micro"),
        ("loa", "loa", "loa"),
        ("tivi", "tivi", "tivi"),
        ("camera", "camera", "camera"),
        ("may in", "may in", "máy in"),
        ("quat", "quat", "quạt"),
        ("bang", "bang", "bảng")
    ];

    public static YeuCauTroLy PhanTich(string cauHoi, DateOnly homNay, IReadOnlyList<ToaNha> toaNhas)
    {
        var y = new YeuCauTroLy { Ngay = homNay.AddDays(1) };
        var s = ChuanHoa(cauHoi).Replace("tuan toi", "tuan sau");

        // Tách mã phòng trước để "A2-301" không bị hiểu thành ngày 2/30.
        var deDocSo = s;
        var mCode = MaPhongRx.Match(s);
        if (mCode.Success)
        {
            y.PhongCode = $"{mCode.Groups[1].Value}-{mCode.Groups[2].Value}".ToUpperInvariant();
            deDocSo = s[..mCode.Index] + new string(' ', mCode.Length) + s[(mCode.Index + mCode.Length)..];
        }

        DocNgay(deDocSo, s, homNay, y);
        var (tu, den, buoi) = DocGio(deDocSo);
        y.Tu = tu;
        y.Den = den;
        y.Buoi = buoi;

        DocSucChua(deDocSo, y);
        DocToa(s, toaNhas, y);
        DocLoaiPhong(deDocSo, y);
        DocThietBi(deDocSo, y);

        if (y.Ngay < homNay.AddDays(1))
        {
            var ngayMoi = homNay.AddDays(1);
            y.CanhBao.Add($"Phiếu mượn phải gửi trước ngày sử dụng ít nhất 24 giờ nên mình chuyển sang ngày {ngayMoi:dd/MM/yyyy}.");
            y.Ngay = ngayMoi;
        }

        if (y.Tu is TimeOnly t && y.Den is TimeOnly d
            && (t < new TimeOnly(7, 0) || d > new TimeOnly(21, 0)))
            y.CanhBao.Add("Khung giờ nằm ngoài giờ phục vụ 07:00 – 21:00.");

        TaoMoTa(y);
        return y;
    }

    private static void DocNgay(string deDocSo, string s, DateOnly homNay, YeuCauTroLy y)
    {
        var m = NgaySlashRx.Match(deDocSo);
        if (!m.Success) m = NgayGachRx.Match(deDocSo);
        if (m.Success && ThuNgay(m, homNay, out var ngay))
        {
            y.Ngay = ngay;
            return;
        }

        if (Regex.IsMatch(deDocSo, @"\b(hom nay|ngay hom nay)\b"))
        {
            y.Ngay = homNay;
            return;
        }
        if (Regex.IsMatch(deDocSo, @"\b(ngay kia|ngay mot)\b"))
        {
            y.Ngay = homNay.AddDays(2);
            return;
        }
        if (Regex.IsMatch(deDocSo, @"\b(ngay mai|mai)\b"))
        {
            y.Ngay = homNay.AddDays(1);
            return;
        }

        var mThu = Regex.Match(deDocSo, @"\bthu\s*([2-7])\b");
        var chuNhat = Regex.IsMatch(deDocSo, @"\bchu nhat\b");
        if (mThu.Success || chuNhat)
        {
            var thu = mThu.Success ? int.Parse(mThu.Groups[1].Value) : 0;
            var tuanSau = Regex.IsMatch(s, @"\btuan sau\b");
            y.Ngay = NgayThu(homNay, thu, tuanSau);
        }
    }

    private static bool ThuNgay(Match m, DateOnly homNay, out DateOnly ngay)
    {
        ngay = default;
        if (!int.TryParse(m.Groups[1].Value, out var d) || !int.TryParse(m.Groups[2].Value, out var thang))
            return false;

        var nam = homNay.Year;
        if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var n))
            nam = n < 100 ? 2000 + n : n;

        if (thang is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(nam, thang))
            return false;

        ngay = new DateOnly(nam, thang, d);
        if (!m.Groups[3].Success && ngay < homNay.AddDays(-30))
            ngay = ngay.AddYears(1);
        return true;
    }

    private static DateOnly NgayThu(DateOnly homNay, int thu, bool tuanSau)
    {
        var idx = thu == 0 ? 6 : thu - 1;
        var homNayIdx = ((int)homNay.DayOfWeek + 6) % 7;
        if (tuanSau)
            return homNay.AddDays(7 - homNayIdx + idx);

        var delta = (idx - homNayIdx + 7) % 7;
        if (delta == 0) delta = 7;
        return homNay.AddDays(delta);
    }

    private static (TimeOnly? Tu, TimeOnly? Den, string? Buoi) DocGio(string s)
    {
        var coSang = Regex.IsMatch(s, @"\b(buoi sang|sang nay|sang mai)\b")
                     || Regex.IsMatch(s, @"\b\d{1,2}\s*(?:h|gio)\s*\d{0,2}\s*sang\b")
                     || Regex.IsMatch(s, @"\bsang\s+\d{1,2}\s*(?:h|gio)\b");
        var coChieu = Regex.IsMatch(s, @"\b(buoi chieu|chieu nay|chieu mai)\b")
                      || Regex.IsMatch(s, @"\b\d{1,2}\s*(?:h|gio)\s*\d{0,2}\s*chieu\b")
                      || Regex.IsMatch(s, @"\bchieu\s+\d{1,2}\s*(?:h|gio)\b");
        var coToi = Regex.IsMatch(s, @"\b(buoi toi|toi nay|toi mai|vao toi)\b")
                    || Regex.IsMatch(s, @"\b\d{1,2}\s*(?:h|gio)\s*(?:\d{1,2}\s*)?toi\b(?!\s*\d)");
        var buoi = coSang ? "SANG" : coChieu ? "CHIEU" : coToi ? "TOI" : null;

        TimeOnly? tu = null, den = null;
        var m = KhoangGioRx.Match(s);
        if (m.Success)
        {
            tu = TaoGio(int.Parse(m.Groups[1].Value), DocPhut(m.Groups[2]), coChieu, coToi);
            den = TaoGio(int.Parse(m.Groups[3].Value), DocPhut(m.Groups[4]), coChieu, coToi);
        }
        else if ((m = KhoangGioGonRx.Match(s)).Success || (m = KhoangGioLungRx.Match(s)).Success)
        {
            tu = TaoGio(int.Parse(m.Groups[1].Value), 0, coChieu, coToi);
            den = TaoGio(int.Parse(m.Groups[2].Value), 0, coChieu, coToi);
        }
        else if ((m = MotGioRx.Match(s)).Success)
        {
            tu = TaoGio(int.Parse(m.Groups[1].Value), DocPhut(m.Groups[2]), coChieu, coToi);
        }

        var soPhut = DocThoiLuong(s);
        if (tu is not null && den is null)
        {
            var thoiDiem = tu.Value.ToTimeSpan() + TimeSpan.FromMinutes(soPhut ?? 120);
            den = thoiDiem >= TimeSpan.FromDays(1)
                ? new TimeOnly(23, 59)
                : TimeOnly.FromTimeSpan(thoiDiem);
        }
        else if (tu is null && soPhut is not null && buoi is not null)
        {
            var khung = BuoiPeriod(buoi);
            tu = khung.Tu;
            var thoiDiem = tu.Value.ToTimeSpan() + TimeSpan.FromMinutes(soPhut.Value);
            den = thoiDiem >= TimeSpan.FromDays(1)
                ? new TimeOnly(23, 59)
                : TimeOnly.FromTimeSpan(thoiDiem);
        }

        if (tu is not null && den is not null && den <= tu)
        {
            // "11h đến 1h chiều": giờ bắt đầu bị cộng nhầm 12.
            if (coChieu || coToi)
            {
                var thu = tu.Value.AddHours(-12);
                if (thu < den) tu = thu;
            }
        }
        if (tu is not null && den is not null && den <= tu)
            (tu, den) = (den, tu);

        return (tu, den, buoi);
    }

    private static TimeOnly TaoGio(int gio, int phut, bool chieu, bool toi)
    {
        if (gio is < 0 or > 23) gio = Math.Clamp(gio, 0, 23);
        if (phut is < 0 or > 59) phut = 0;

        if ((chieu || toi) && gio is >= 1 and <= 11) gio += 12;
        else if (!chieu && !toi && gio is >= 1 and <= 6) gio += 12; // 1h–6h không ghi buổi: hiểu là chiều
        return new TimeOnly(gio, phut);
    }

    private static int DocPhut(Group g) =>
        g.Success && int.TryParse(g.Value, out var phut) && phut is >= 0 and < 60 ? phut : 0;

    private static int? DocThoiLuong(string s)
    {
        var m = Regex.Match(s, @"(\d{1,3})\s*phut\b");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var phut) && phut is >= 15 and <= 600)
            return phut;

        m = Regex.Match(s, @"(\d{1,2})\s*tieng\s*(ruoi)?\b");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var gio) && gio is >= 1 and <= 8)
            return gio * 60 + (m.Groups[2].Success ? 30 : 0);

        return null;
    }

    private static void DocSucChua(string s, YeuCauTroLy y)
    {
        var m = Regex.Match(s, @"(\d{1,4})\s*(?:cho|nguoi|sinh vien)\b");
        if (!m.Success)
            m = Regex.Match(s, @"(?:suc chua|cho ngoi|cho duoc)\s*(?:la|khoang|tren|tu)?\s*(\d{1,4})");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var so) && so is >= 1 and <= 1000)
            y.SucChua = so;
    }

    private static void DocToa(string s, IReadOnlyList<ToaNha> toaNhas, YeuCauTroLy y)
    {
        ToaNha? chon = null;
        var daiNhat = 0;
        foreach (var t in toaNhas)
        {
            var code = ChuanHoa(t.CodeToaNha);
            var ten = ChuanHoa(t.TenToaNha);
            var khop = (ten.Length > 0 && s.Contains(ten))
                       || Regex.IsMatch(s, $@"\b(toa|nha)\s+{Regex.Escape(code)}\b")
                       || (code.Any(char.IsDigit) && Regex.IsMatch(s, $@"\b{Regex.Escape(code)}\b"));
            if (khop && code.Length > daiNhat)
            {
                chon = t;
                daiNhat = code.Length;
            }
        }
        if (chon is not null)
        {
            y.MaToaNha = chon.MaToaNha;
            y.TenToa = chon.TenToaNha;
        }
    }

    private static void DocLoaiPhong(string s, YeuCauTroLy y)
    {
        if (Regex.IsMatch(s, @"\b(may tinh|tin hoc|cntt|lap trinh|phong may)\b")) y.LoaiPhong = Catalogs.PhongMayTinh;
        else if (Regex.IsMatch(s, @"\b(hoa sinh|sinh hoc|thi nghiem hoa)\b")) y.LoaiPhong = Catalogs.LabHoaSinh;
        else if (Regex.IsMatch(s, @"\b(thuc pham|cntp|che bien)\b")) y.LoaiPhong = Catalogs.LabCntp;
        else if (Regex.IsMatch(s, @"\b(dien tu|tu dong hoa|vi dieu khien|plc|dien)\b")) y.LoaiPhong = Catalogs.LabDienDienTu;
        else if (Regex.IsMatch(s, @"\b(co khi|o to|che tao|xuong|may tien|may phay|cnc)\b")) y.LoaiPhong = Catalogs.XuongCoKhi;
        else if (Regex.IsMatch(s, @"\b(luyen thanh|thanh nhac|hat)\b")) y.LoaiPhong = Catalogs.PhongLuyenThanh;
        else if (Regex.IsMatch(s, @"\b(hoa tau|hoa nhac|nhac cu|ban nhac)\b")) y.LoaiPhong = Catalogs.PhongHoaTau;
        else if (Regex.IsMatch(s, @"\b(my thuat|hoa vien|ve)\b")) y.LoaiPhong = Catalogs.PhongHoaVienMyThuat;
        else if (Regex.IsMatch(s, @"\b(kien truc|do an)\b")) y.LoaiPhong = Catalogs.PhongDoAnKienTruc;
        else if (Regex.IsMatch(s, @"\b(the duc|the thao|the hinh|gym|san bong|bong ro)\b")) y.LoaiPhong = Catalogs.SanTheDucNgoaiTroi;
        else if (Regex.IsMatch(s, @"\b(thi nghiem|phong lab|lab)\b")) y.LoaiPhong = Catalogs.LabHoaSinh;
        else if (Regex.IsMatch(s, @"\b(ly thuyet)\b")) y.LoaiPhong = Catalogs.RoomTheory;
    }

    private static void DocThietBi(string s, YeuCauTroLy y)
    {
        var daCo = new HashSet<string>();
        foreach (var (khoa, loc, hienThi) in ThietBiMap)
        {
            if (!Regex.IsMatch(s, $@"\b{Regex.Escape(khoa)}\b")) continue;
            if (!daCo.Add(loc)) continue;
            y.ThietBi.Add(loc);
            y.ThietBiTen.Add(hienThi);
        }
    }

    private static void TaoMoTa(YeuCauTroLy y)
    {
        if (y.PhongCode is not null) y.MoTa.Add($"Phòng {y.PhongCode}");
        y.MoTa.Add($"{ThuTrongTuan(y.Ngay)}, {y.Ngay:dd/MM/yyyy}");
        if (y.Tu is TimeOnly tu && y.Den is TimeOnly den)
            y.MoTa.Add($"{tu:HH\\:mm} – {den:HH\\:mm}");
        else if (y.Buoi is not null)
            y.MoTa.Add($"Buổi {TenBuoi(y.Buoi).ToLower()}");
        else
            y.MoTa.Add("Cả ngày");
        if (y.TenToa is not null) y.MoTa.Add(y.TenToa);
        if (y.LoaiPhong is not null) y.MoTa.Add(Catalogs.LabelLoaiPhong(y.LoaiPhong));
        if (y.SucChua is int sc) y.MoTa.Add($"Ít nhất {sc} chỗ");
        if (y.ThietBiTen.Count > 0) y.MoTa.Add("Có " + string.Join(", ", y.ThietBiTen));
    }

    public static bool KhopThietBi(IReadOnlyList<string> tenThietBi, IReadOnlyList<string> yeuCau)
    {
        if (yeuCau.Count == 0) return true;
        var ten = tenThietBi.Select(ChuanHoa).ToList();
        return yeuCau.All(k => ten.Any(t => t.Contains(k)));
    }

    public static bool KhopBuoi(IReadOnlyList<KhungTrongVm> khung, string? buoi)
    {
        if (buoi is null) return true;
        var p = BuoiPeriod(buoi);
        return khung.Any(k => k.Den > p.Tu && k.Tu < p.Den);
    }

    public static (TimeOnly Tu, TimeOnly Den) BuoiPeriod(string buoi) => buoi switch
    {
        "SANG" => (new TimeOnly(7, 0), new TimeOnly(11, 25)),
        "CHIEU" => (new TimeOnly(13, 0), new TimeOnly(17, 25)),
        "TOI" => (new TimeOnly(18, 30), new TimeOnly(21, 0)),
        _ => (new TimeOnly(0, 0), new TimeOnly(23, 59))
    };

    public static string TenBuoi(string buoi) => buoi switch
    {
        "SANG" => "Sáng",
        "CHIEU" => "Chiều",
        "TOI" => "Tối",
        _ => "Trong ngày"
    };

    public static (TimeOnly? Tu, TimeOnly? Den) GioDeXuat(IReadOnlyList<KhungTrongVm> khung, YeuCauTroLy y)
    {
        if (y.Tu is TimeOnly tu && y.Den is TimeOnly den) return (tu, den);

        (TimeOnly Tu, TimeOnly Den)? p = y.Buoi is null ? null : BuoiPeriod(y.Buoi);
        var ungVien = khung
            .Where(k => p is null || (k.Den > p.Value.Tu && k.Tu < p.Value.Den))
            .OrderBy(k => k.Tu)
            .ToList();
        if (ungVien.Count == 0) return (null, null);
        return (ungVien[0].Tu, ungVien[0].Den);
    }

    public static (KhungTrongVm Khung, int KhoangCach)? KhungGanNhat(
        IReadOnlyList<KhungTrongVm> ds, YeuCauTroLy y)
    {
        IEnumerable<KhungTrongVm> ungVien = ds;
        if (y.Tu is null && y.Buoi is not null)
        {
            var p = BuoiPeriod(y.Buoi);
            ungVien = ungVien.Where(k => k.Den > p.Tu && k.Tu < p.Den);
        }

        if (y.Tu is TimeOnly tu && y.Den is TimeOnly den)
        {
            var phut = Math.Max(15, (int)(den - tu).TotalMinutes);
            var dsLoc = ungVien
                .Where(k => (k.Den - k.Tu).TotalMinutes >= phut)
                .Select(k => (k, cach: KhoangCach(k, tu, den)))
                .OrderBy(x => x.cach)
                .ThenBy(x => x.k.Tu)
                .ToList();
            return dsLoc.Count == 0 ? null : (dsLoc[0].k, dsLoc[0].cach);
        }

        var dauTien = ungVien.OrderBy(k => k.Tu).FirstOrDefault();
        return dauTien is null ? null : (dauTien, 0);
    }

    private static int KhoangCach(KhungTrongVm k, TimeOnly tu, TimeOnly den)
    {
        if (k.Den <= tu) return (int)(tu - k.Den).TotalMinutes;
        if (k.Tu >= den) return (int)(k.Tu - den).TotalMinutes;
        return 0;
    }

    public static string ThuTrongTuan(DateOnly ngay) => ngay.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ Hai",
        DayOfWeek.Tuesday => "Thứ Ba",
        DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm",
        DayOfWeek.Friday => "Thứ Sáu",
        DayOfWeek.Saturday => "Thứ Bảy",
        _ => "Chủ Nhật"
    };

    /// <summary>Bỏ dấu tiếng Việt để so khớp từ khoá, không làm thay đổi dữ liệu lưu trong CSDL.</summary>
    public static string ChuanHoa(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => c
            });
        }
        return sb.ToString().ToLowerInvariant();
    }
}
