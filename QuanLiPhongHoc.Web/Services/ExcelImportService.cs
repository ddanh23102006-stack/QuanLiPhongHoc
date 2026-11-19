using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public class ImportRowError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ImportResult
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }

    /// <summary>Số tài khoản giảng viên được tạo tự động trong lần nhập này.</summary>
    public int LecturersCreated { get; set; }

    public List<ImportRowError> Errors { get; set; } = [];
    public List<HocPhan> Imported { get; set; } = [];
}

public interface IExcelImportService
{
    Task<ImportResult> ImportHocPhanAsync(Stream stream);
}

/// <summary>
/// Import danh sách lớp học phần từ Excel (.xlsx).
/// Hỗ trợ hai định dạng:
/// <list type="bullet">
/// <item>Chuẩn cũ: <c>ma_hoc_phan | ten_hoc_phan | so_luong_dang_ky | ma_giang_vien | la_thuc_hanh</c>.</item>
/// <item>Bảng kế hoạch: <c>STT | Mã Lớp HP | Tên Môn Học | Số Tín Chỉ | Giảng Viên Giảng Dạy | Sĩ Số | Loại Phòng Yêu Cầu</c>
/// (tiêu đề có thể nằm ở bất kỳ dòng nào; cột được dò theo tên tiêu đề).</item>
/// </list>
/// Giảng viên khớp theo mã hoặc theo tên; tên chưa có trong hệ thống sẽ được tạo tài khoản giảng viên mới.
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly QuanLiPhongHocContext _db;

    public ExcelImportService(QuanLiPhongHocContext db) => _db = db;

    private static readonly string[] MaHocPhanAliases =
        ["ma hoc phan", "ma lop hp", "ma lop hoc phan", "mahocphan"];

    private static readonly string[] TenHocPhanAliases =
        ["ten hoc phan", "ten mon hoc", "mon hoc", "tenhocphan"];

    private static readonly string[] SiSoAliases =
        ["so luong dang ky", "si so", "so sinh vien", "soluongdangky"];

    private static readonly string[] GiangVienAliases =
        ["ma giang vien", "giang vien giang day", "giang vien", "ten giang vien", "magiangvien"];

    private static readonly string[] ThucHanhAliases =
        ["la thuc hanh", "thuc hanh", "lathuchanh"];

    private static readonly string[] LoaiPhongAliases =
        ["loai phong yeu cau", "loai phong"];

    private sealed record ParsedRow(
        string MaHocPhan, string TenHocPhan, int SiSo,
        string? MaGiangVien, bool LaThucHanh, string? NhomNganh);

    public async Task<ImportResult> ImportHocPhanAsync(Stream stream)
    {
        var result = new ImportResult();

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet is null)
        {
            result.Errors.Add(new ImportRowError { Row = 0, Message = "Tệp Excel không có trang tính nào." });
            return result;
        }

        var rows = sheet.RowsUsed().ToList();
        if (rows.Count == 0)
        {
            result.Errors.Add(new ImportRowError { Row = 0, Message = "Tệp Excel trống." });
            return result;
        }

        // Dò dòng tiêu đề: hàng nào vừa có cột "mã học phần" vừa có cột "tên ..." thì là hàng tiêu đề.
        var header = TimDongTieuDe(rows, out var cotTheoTen);

        int ColTim(string[] aliases) =>
            aliases.Select(a => cotTheoTen.TryGetValue(a, out var c) ? c : 0).FirstOrDefault(c => c > 0);

        int colMa, colTen, colSiSo, colGiangVien, colLaThucHanh, colLoaiPhong;
        if (header is not null)
        {
            colMa = ColTim(MaHocPhanAliases);
            colTen = ColTim(TenHocPhanAliases);
            colSiSo = ColTim(SiSoAliases);
            colGiangVien = ColTim(GiangVienAliases);
            colLaThucHanh = ColTim(ThucHanhAliases);
            colLoaiPhong = ColTim(LoaiPhongAliases);
        }
        else
        {
            // Không nhận diện được tiêu đề: đọc theo thứ tự cột của định dạng chuẩn cũ.
            colMa = 1;
            colTen = 2;
            colSiSo = 3;
            colGiangVien = 4;
            colLaThucHanh = 5;
            colLoaiPhong = 0;
        }

        var dataRows = header is null ? rows : rows.Skip(header.Value + 1).ToList();

        // Tra cứu giảng viên theo mã hoặc theo tên (chỉ xét tài khoản có vai trò giảng viên).
        var giangViens = await _db.NguoiDungs
            .Where(x => x.NguoiDungVaiTros.Any(v => v.VaiTro.CodeVaiTro == AppRoles.Lecturer))
            .ToListAsync();
        var gvTheoMa = giangViens.ToDictionary(x => x.MaNguoiDung, StringComparer.OrdinalIgnoreCase);
        var gvTheoTen = giangViens
            .GroupBy(x => x.HoTen.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().MaNguoiDung, StringComparer.OrdinalIgnoreCase);

        var vaiTroGiangVien = await _db.VaiTros.FirstOrDefaultAsync(x => x.CodeVaiTro == AppRoles.Lecturer);
        var maDangDung = await _db.NguoiDungs.Select(x => x.MaNguoiDung).ToListAsync();
        var emailDangDung = await _db.NguoiDungs.Select(x => x.Email).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        var soGvLonNhat = maDangDung
            .Where(c => c.StartsWith("GV", StringComparison.OrdinalIgnoreCase)
                        && c.Length > 2 && c[2..].All(char.IsDigit))
            .Select(c => int.Parse(c[2..]))
            .DefaultIfEmpty(1000)
            .Max();

        // Tạo tài khoản giảng viên mới cho tên chưa tồn tại trong hệ thống.
        NguoiDung? TaoGiangVien(string hoTen)
        {
            if (vaiTroGiangVien is null) return null;

            var ma = $"GV{++soGvLonNhat}";
            var email = $"{ma.ToLowerInvariant()}@school.edu.vn";
            var stt = 1;
            while (!emailDangDung.Add(email))
                email = $"{ma.ToLowerInvariant()}-{++stt}@school.edu.vn";

            var nd = new NguoiDung
            {
                MaNguoiDung = ma,
                HoTen = hoTen,
                Email = email,
                MatKhau = MatKhauHasher.Bam("123456"),
                NgayTao = DateTime.Now
            };
            _db.NguoiDungs.Add(nd);
            _db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = ma, MaVaiTro = vaiTroGiangVien.MaVaiTro });

            gvTheoMa[ma] = nd;
            gvTheoTen[hoTen] = ma;
            result.LecturersCreated++;
            return nd;
        }

        string? GiaiGiangVien(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var value = raw.Trim();
            if (gvTheoMa.TryGetValue(value, out var nd)) return nd.MaNguoiDung;
            if (gvTheoTen.TryGetValue(value, out var ma)) return ma;
            return TaoGiangVien(value)?.MaNguoiDung;
        }

        var parsed = new List<ParsedRow>();
        foreach (var row in dataRows)
        {
            var rowNo = row.RowNumber();
            string Doc(int col) => col > 0 ? row.Cell(col).GetString().Trim() : string.Empty;

            try
            {
                var maHocPhan = Doc(colMa);
                var tenHocPhan = Doc(colTen);
                var siSoRaw = Doc(colSiSo);
                var giangVienRaw = Doc(colGiangVien);
                var laThucHanhRaw = Doc(colLaThucHanh);
                var loaiPhongRaw = Doc(colLoaiPhong);

                if (string.IsNullOrWhiteSpace(maHocPhan) && string.IsNullOrWhiteSpace(tenHocPhan)
                    && string.IsNullOrWhiteSpace(siSoRaw) && string.IsNullOrWhiteSpace(giangVienRaw))
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(maHocPhan))
                {
                    result.Errors.Add(new ImportRowError { Row = rowNo, Message = "Thiếu mã học phần." });
                    continue;
                }
                if (string.IsNullOrWhiteSpace(tenHocPhan))
                {
                    result.Errors.Add(new ImportRowError { Row = rowNo, Message = $"Học phần {maHocPhan}: thiếu tên học phần." });
                    continue;
                }
                if (!int.TryParse(siSoRaw, out var siSo) || siSo <= 0)
                {
                    result.Errors.Add(new ImportRowError { Row = rowNo, Message = $"Học phần {maHocPhan}: sĩ số '{siSoRaw}' không hợp lệ." });
                    continue;
                }

                // Cột "Loại phòng yêu cầu" quyết định lớp lý thuyết hay thực hành; thực hành gắn nhóm CNTT.
                bool laThucHanh;
                string? nhomNganh = null;
                if (colLoaiPhong > 0)
                {
                    laThucHanh = !ChuanHoa(loaiPhongRaw).Contains("ly thuyet");
                    if (laThucHanh) nhomNganh = Catalogs.NganhCntt;
                }
                else
                {
                    laThucHanh = laThucHanhRaw is "1" or "true" or "TRUE" or "x" or "X" or "yes" or "có" or "Có";
                }

                parsed.Add(new ParsedRow(maHocPhan, tenHocPhan, siSo, GiaiGiangVien(giangVienRaw), laThucHanh, nhomNganh));
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError { Row = rowNo, Message = ex.Message });
            }
        }

        // Lưu tài khoản giảng viên mới trước để bảo đảm khóa ngoại khi thêm học phần.
        if (result.LecturersCreated > 0)
            await _db.SaveChangesAsync();

        var hocPhanHienCo = await _db.HocPhans.ToListAsync();
        var banDo = new Dictionary<string, HocPhan>(StringComparer.OrdinalIgnoreCase);
        foreach (var hp in hocPhanHienCo) banDo[hp.MaHocPhan] = hp;

        foreach (var p in parsed)
        {
            if (banDo.TryGetValue(p.MaHocPhan, out var existing))
            {
                existing.TenHocPhan = p.TenHocPhan;
                existing.SoLuongDangKy = p.SiSo;
                existing.MaGiangVien = p.MaGiangVien;
                existing.LaThucHanh = p.LaThucHanh;
                existing.NhomNganh = p.NhomNganh;
                result.Imported.Add(existing);
                result.Updated++;
            }
            else
            {
                var hp = new HocPhan
                {
                    MaHocPhan = p.MaHocPhan,
                    TenHocPhan = p.TenHocPhan,
                    SoLuongDangKy = p.SiSo,
                    MaGiangVien = p.MaGiangVien,
                    LaThucHanh = p.LaThucHanh,
                    NhomNganh = p.NhomNganh
                };
                _db.HocPhans.Add(hp);
                banDo[p.MaHocPhan] = hp;
                result.Imported.Add(hp);
                result.Inserted++;
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    /// <summary>Dò dòng tiêu đề trong 20 dòng đầu và trả về bản đồ tên cột (đã chuẩn hóa) → số thứ tự cột.</summary>
    private static int? TimDongTieuDe(List<IXLRow> rows, out Dictionary<string, int> cotTheoTen)
    {
        cotTheoTen = new Dictionary<string, int>(StringComparer.Ordinal);
        var soDongKiemTra = Math.Min(rows.Count, 20);
        for (var i = 0; i < soDongKiemTra; i++)
        {
            var banDo = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var cell in rows[i].CellsUsed())
            {
                var ten = ChuanHoa(cell.GetString());
                if (ten.Length == 0) continue;
                banDo.TryAdd(ten, cell.Address.ColumnNumber);
            }

            var coMa = MaHocPhanAliases.Any(banDo.ContainsKey);
            var coTen = TenHocPhanAliases.Any(banDo.ContainsKey);
            if (coMa && coTen)
            {
                cotTheoTen = banDo;
                return i;
            }
        }
        return null;
    }

    /// <summary>Chuẩn hóa chuỗi: bỏ dấu tiếng Việt, dấu câu và khoảng trắng dư để so khớp tiêu đề.</summary>
    private static string ChuanHoa(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var s = value.Trim().ToLowerInvariant()
            .Replace('_', ' ').Replace('-', ' ').Replace('.', ' ').Replace('/', ' ');
        var decomposed = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
