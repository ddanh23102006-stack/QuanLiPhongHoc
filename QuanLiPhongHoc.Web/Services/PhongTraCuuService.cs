using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

/// <summary>Một khoảng giờ phòng trống, đã trừ lịch học và các phiếu đang giữ chỗ.</summary>
public record KhungTrongVm(TimeOnly Tu, TimeOnly Den)
{
    public string MoTa => Catalogs.MoTaKhoang(Tu, Den);
}

/// <summary>Card phòng trên màn hình đăng ký mượn phòng.</summary>
public class PhongTraCuuVm
{
    public int MaPhong { get; init; }
    public string CodePhong { get; init; } = string.Empty;
    public string LoaiPhong { get; init; } = string.Empty;
    public int SucChua { get; init; }
    public string TrangThaiPhong { get; init; } = string.Empty;
    public string? Toa { get; init; }
    public IReadOnlyList<string> ThietBi { get; init; } = [];

    /// <summary>Khung giờ CSVC đã khai cho phòng — chưa trừ gì, dùng để báo "phòng chỉ trống trong...".</summary>
    public IReadOnlyList<KhungTrongVm> KhungKhaiBao { get; init; } = [];

    /// <summary>Phần giờ vẫn còn trống trong ngày, sau khi cắt các khoảng đã có lớp hoặc đã có phiếu.</summary>
    public IReadOnlyList<KhungTrongVm> KhungConTrong { get; init; } = [];

    /// <summary>FREE / BUSY / OFFLINE — trạng thái suy ra tại ngày và khung giờ đang lọc.</summary>
    public string TrangThai { get; init; } = Catalogs.PhongTrong;

    /// <summary>Vì sao khung giờ đang xét không mượn được — chỉ có khi phòng BUSY.</summary>
    public string? LyDoBan { get; init; }
}

public class PhongTraCuuFilter
{
    public DateOnly Ngay { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Khung giờ người xem muốn mượn. Chỉ có tác dụng khi nhập đủ hai giờ và sau giờ bắt đầu.</summary>
    public TimeOnly? Tu { get; init; }
    public TimeOnly? Den { get; init; }

    public string? Q { get; init; }
    public int? MaToaNha { get; init; }
    public string? LoaiPhong { get; init; }
    public string? TrangThai { get; init; }

    public (TimeOnly Tu, TimeOnly Den)? XinMuon =>
        Tu is TimeOnly tu && Den is TimeOnly den && den > tu ? (tu, den) : null;
}

public interface IPhongTraCuuService
{
    Task<List<PhongTraCuuVm>> TraCuuAsync(PhongTraCuuFilter filter);
}

/// <summary>
/// Tính trạng thái phòng trực tiếp từ CSDL mỗi lần gọi — không cache — để CSVC vừa khai
/// khung giờ hay thêm phòng là màn hình mượn phòng thấy ngay ở lần tải kế tiếp.
/// </summary>
public class PhongTraCuuService : IPhongTraCuuService
{
    private readonly QuanLiPhongHocContext _db;

    public PhongTraCuuService(QuanLiPhongHocContext db) => _db = db;

    public async Task<List<PhongTraCuuVm>> TraCuuAsync(PhongTraCuuFilter filter)
    {
        var phongQuery = _db.PhongHocs.AsNoTracking()
            .Where(x => x.HienThiDangKyMuon)
            .Include(x => x.ToaNha)
            .Include(x => x.KhungTrongs)
            .Include(x => x.PhongThietBis).ThenInclude(x => x.ThietBi)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Q))
            phongQuery = phongQuery.Where(x => x.CodePhong.Contains(filter.Q));
        if (filter.MaToaNha is int maToa)
            phongQuery = phongQuery.Where(x => x.MaToaNha == maToa);
        if (!string.IsNullOrWhiteSpace(filter.LoaiPhong))
            phongQuery = phongQuery.Where(x => x.LoaiPhong == filter.LoaiPhong);

        var phongs = await phongQuery.OrderBy(x => x.CodePhong).ToListAsync();
        if (phongs.Count == 0) return [];

        var maPhongs = phongs.Select(x => x.MaPhong).ToList();

        var lich = await _db.LichHocs.AsNoTracking()
            .Include(x => x.HocPhan)
            .Where(x => x.NgayHoc == filter.Ngay && x.TrangThaiHoatDong && x.MaPhong != null && maPhongs.Contains(x.MaPhong.Value))
            .ToListAsync();

        var dat = await _db.DatPhongs.AsNoTracking()
            .Include(x => x.NguoiYeuCauNav)
            .Where(x => x.NgayDat == filter.Ngay && x.MaPhong != null
                        && (x.TrangThai == "PENDING" || x.TrangThai == "APPROVED")
                        && maPhongs.Contains(x.MaPhong.Value))
            .ToListAsync();

        var lichTheoPhong = lich.GroupBy(x => x.MaPhong!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var datTheoPhong = dat.GroupBy(x => x.MaPhong!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var ketQua = new List<PhongTraCuuVm>();
        foreach (var p in phongs)
        {
            IEnumerable<(TimeOnly Start, TimeOnly End)> khaiBaoRaw = p.KhungTrongs.Count > 0
                ? p.KhungTrongs.Select(x => (x.GioBatDau, x.GioKetThuc))
                : Catalogs.KhungTrongMacDinh;
            var khunKhaiBao = khaiBaoRaw
                .OrderBy(x => x.Start)
                .Select(x => new KhungTrongVm(x.Start, x.End))
                .ToList();

            var giuCho = new List<(TimeOnly Start, TimeOnly End, string LyDo)>();
            foreach (var x in lichTheoPhong.GetValueOrDefault(p.MaPhong, []))
            {
                if (Catalogs.GetCaTime(x.CaHoc) is not (var batDau, var ketThuc)) continue;
                giuCho.Add((batDau, ketThuc,
                    $"Lịch {x.HocPhan?.TenHocPhan ?? x.MaHocPhan} ({Catalogs.MoTaKhoang(batDau, ketThuc)})"));
            }
            foreach (var x in datTheoPhong.GetValueOrDefault(p.MaPhong, []))
            {
                var mo = x.TrangThai == "APPROVED"
                    ? $"Phiếu đã duyệt của {x.NguoiYeuCauNav?.HoTen ?? x.NguoiYeuCau}"
                    : $"{x.NguoiYeuCauNav?.HoTen ?? x.NguoiYeuCau} đã đăng ký, chờ duyệt";
                giuCho.Add((x.GioBatDau, x.GioKetThuc, $"{mo} ({Catalogs.MoTaKhoang(x.GioBatDau, x.GioKetThuc)})"));
            }

            var conTrong = Catalogs
                .ConKhungTrong(khunKhaiBao.Select(x => (x.Tu, x.Den)), giuCho.Select(x => (x.Start, x.End)))
                .Select(x => new KhungTrongVm(x.Start, x.End))
                .ToList();

            // Tòa nhà bảo trì/ngừng hoạt động thì mọi phòng bên trong đều offline,
            // kể cả khi phòng vẫn đang ở trạng thái AVAILABLE.
            var offline = p.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE"
                          || !Catalogs.ToaNhaDangHoatDong(p.ToaNha);
            string trangThai;
            string? lyDoBan = null;

            if (offline)
            {
                trangThai = Catalogs.PhongBaoTri;
            }
            else if (filter.XinMuon is (TimeOnly xinTu, TimeOnly xinDen))
            {
                var trung = giuCho.Where(g => Catalogs.IsOverlap(g.Start, g.End, xinTu, xinDen))
                                  .Select(g => g.LyDo).ToList();
                var phuHop = khunKhaiBao.Any(k => Catalogs.NamTrongKhung((k.Tu, k.Den), xinTu, xinDen));

                if (trung.Count > 0)
                {
                    trangThai = Catalogs.PhongBan;
                    lyDoBan = string.Join(", ", trung);
                }
                else if (!phuHop)
                {
                    trangThai = Catalogs.PhongBan;
                    lyDoBan = $"Ngoài khung giờ trống của phòng. Phòng chỉ trống: {Ghep(khunKhaiBao)}.";
                }
                else
                {
                    trangThai = Catalogs.PhongTrong;
                }
            }
            else
            {
                trangThai = conTrong.Count > 0 ? Catalogs.PhongTrong : Catalogs.PhongBan;
                lyDoBan = conTrong.Count > 0
                    ? null
                    : giuCho.Count > 0 ? "Đã kín mọi khung giờ trống trong ngày." : "Phòng chưa khai khung giờ trống.";
            }

            if (!string.IsNullOrEmpty(filter.TrangThai) && filter.TrangThai != trangThai) continue;

            ketQua.Add(new PhongTraCuuVm
            {
                MaPhong = p.MaPhong,
                CodePhong = p.CodePhong,
                LoaiPhong = p.LoaiPhong,
                SucChua = p.SucChua,
                TrangThaiPhong = p.TrangThaiPhong,
                Toa = p.ToaNha is null ? null : $"{p.ToaNha.CodeToaNha} — {p.ToaNha.TenToaNha}",
                ThietBi = p.PhongThietBis.Where(x => x.ThietBi != null).Select(x => x.ThietBi!.TenThietBi).Distinct().ToList(),
                KhungKhaiBao = khunKhaiBao,
                KhungConTrong = conTrong,
                TrangThai = trangThai,
                LyDoBan = lyDoBan
            });
        }

        return ketQua;
    }

    private static string Ghep(IReadOnlyList<KhungTrongVm> khung) =>
        khung.Count == 0 ? "chưa khai báo" : string.Join(" · ", khung.Select(x => x.MoTa));
}
