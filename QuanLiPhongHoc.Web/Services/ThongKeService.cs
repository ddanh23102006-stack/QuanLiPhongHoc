using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public class DonViStatDto
{
    public string Label { get; set; } = string.Empty;
    public int SoPhong { get; set; }
    public int PhongSanSang { get; set; }
    public int SoCa { get; set; }
    public int SoPhongCoLich { get; set; }
    public double TyLeLapDay { get; set; }
}

public class ThongKeDto
{
    public int TongPhong { get; set; }
    public int PhongSanSang { get; set; }
    public int PhongBaoTri { get; set; }
    public int TongLich { get; set; }
    public int LichHomNay { get; set; }
    public int TongHocPhan { get; set; }
    public int TongGiangVien { get; set; }
    public int TongSinhVien { get; set; }
    public int SuCoMoi { get; set; }
    public int SuCoDangXuLy { get; set; }
    public int SuCoDaXuLy { get; set; }
    public int DatPhongChoDuyet { get; set; }
    public int DatPhongDaDuyet { get; set; }
    public int DatPhongTuChoi { get; set; }
    public int DonNghiChoDuyet { get; set; }
    public int DonNghiDaDuyet { get; set; }
    public double TyLeSuDungPhong { get; set; }
    public int SoPhongCoLich { get; set; }
    public int TongTiet { get; set; }
    public int TietLyThuyet { get; set; }
    public int TietThucHanh { get; set; }
    public int SoCaLyThuyet { get; set; }
    public int SoCaThucHanh { get; set; }
    public double TyLeThucHanh { get; set; }
    public string HocPhanLonNhat { get; set; } = string.Empty;
    public int SiSoLonNhat { get; set; }
    public List<ChartPointDto> PhongTheoToaNha { get; set; } = [];
    public List<ChartPointDto> SuCoTheoTrangThai { get; set; } = [];
    public List<ChartPointDto> LichTheoCa { get; set; } = [];
    public List<ChartPointDto> TopGiangVien { get; set; } = [];
    public List<ChartPointDto> ThietBiTheoTrangThai { get; set; } = [];
    public List<ChartPointDto> PhongTheoLoai { get; set; } = [];
    public List<DonViStatDto> ToaNhaStats { get; set; } = [];
}

public interface IThongKeService
{
    Task<ThongKeDto> GetAsync(DateOnly? from = null, DateOnly? to = null);
}

public class ThongKeService : IThongKeService
{
    private readonly QuanLiPhongHocContext _db;

    public ThongKeService(QuanLiPhongHocContext db) => _db = db;

    public async Task<ThongKeDto> GetAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var start = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = to ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var today = DateOnly.FromDateTime(DateTime.Today);

        var dto = new ThongKeDto
        {
            TongPhong = await _db.PhongHocs.CountAsync(),
            PhongSanSang = await _db.PhongHocs.CountAsync(x => x.TrangThaiPhong == "AVAILABLE"),
            PhongBaoTri = await _db.PhongHocs.CountAsync(x => x.TrangThaiPhong == "MAINTENANCE" || x.TrangThaiPhong == "UNAVAILABLE"),
            TongLich = await _db.LichHocs.CountAsync(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end),
            LichHomNay = await _db.LichHocs.CountAsync(x => x.TrangThaiHoatDong && x.NgayHoc == today),
            TongHocPhan = await _db.HocPhans.CountAsync(),
            TongGiangVien = await _db.NguoiDungs.CountAsync(x => x.NguoiDungVaiTros.Any(v => v.VaiTro.CodeVaiTro == AppRoles.Lecturer)),
            TongSinhVien = await _db.HocPhans.SumAsync(x => (int?)x.SoLuongDangKy) ?? 0,
            SuCoMoi = await _db.SuCos.CountAsync(x => x.TrangThai == "NEW"),
            SuCoDangXuLy = await _db.SuCos.CountAsync(x => x.TrangThai == "IN_PROGRESS"),
            SuCoDaXuLy = await _db.SuCos.CountAsync(x => x.TrangThai == "RESOLVED"),
            DatPhongChoDuyet = await _db.DatPhongs.CountAsync(x => x.TrangThai == "PENDING"),
            DatPhongDaDuyet = await _db.DatPhongs.CountAsync(x => x.TrangThai == "APPROVED"),
            DatPhongTuChoi = await _db.DatPhongs.CountAsync(x => x.TrangThai == "REJECTED"),
            DonNghiChoDuyet = await _db.DonXinNghis.CountAsync(x => x.TrangThai == "PENDING"),
            DonNghiDaDuyet = await _db.DonXinNghis.CountAsync(x => x.TrangThai == "APPROVED")
        };

        var soPhongSuDung = await _db.LichHocs
            .Where(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end && x.MaPhong != null)
            .Select(x => x.MaPhong)
            .Distinct()
            .CountAsync();
        dto.TyLeSuDungPhong = dto.TongPhong == 0 ? 0 : Math.Round(soPhongSuDung * 100.0 / dto.TongPhong, 1);
        dto.SoPhongCoLich = soPhongSuDung;

        dto.SoCaLyThuyet = await _db.LichHocs.CountAsync(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end && !x.HocPhan!.LaThucHanh);
        dto.SoCaThucHanh = await _db.LichHocs.CountAsync(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end && x.HocPhan!.LaThucHanh);
        dto.TietLyThuyet = dto.SoCaLyThuyet * 3;
        dto.TietThucHanh = dto.SoCaThucHanh * 5;
        dto.TongTiet = dto.TietLyThuyet + dto.TietThucHanh;
        var tongCa = dto.SoCaLyThuyet + dto.SoCaThucHanh;
        dto.TyLeThucHanh = tongCa == 0 ? 0 : Math.Round(dto.SoCaThucHanh * 100.0 / tongCa, 1);

        var topHocPhan = await _db.HocPhans.OrderByDescending(x => x.SoLuongDangKy).FirstOrDefaultAsync();
        dto.HocPhanLonNhat = topHocPhan?.TenHocPhan ?? "—";
        dto.SiSoLonNhat = topHocPhan?.SoLuongDangKy ?? 0;

        dto.PhongTheoToaNha = await _db.ToaNhas
            .Select(t => new ChartPointDto { Label = t.CodeToaNha, Value = t.PhongHocs.Count })
            .ToListAsync();

        dto.PhongTheoLoai = await _db.PhongHocs
            .GroupBy(x => x.LoaiPhong)
            .Select(g => new ChartPointDto { Label = g.Key, Value = g.Count() })
            .ToListAsync();

        dto.SuCoTheoTrangThai = await _db.SuCos
            .GroupBy(x => x.TrangThai)
            .Select(g => new ChartPointDto { Label = g.Key ?? "NEW", Value = g.Count() })
            .ToListAsync();

        dto.LichTheoCa = await _db.LichHocs
            .Where(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end)
            .GroupBy(x => x.CaHoc)
            .Select(g => new ChartPointDto { Label = g.Key, Value = g.Count() })
            .ToListAsync();

        var topGv = await _db.LichHocs
            .Where(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end && x.HocPhan!.MaGiangVien != null)
            .GroupBy(x => x.HocPhan!.MaGiangVien!)
            .Select(g => new { Ma = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync();
        var gvIds = topGv.Select(x => x.Ma).ToList();
        var gvNames = await _db.NguoiDungs.Where(x => gvIds.Contains(x.MaNguoiDung))
            .ToDictionaryAsync(x => x.MaNguoiDung, x => x.HoTen);
        dto.TopGiangVien = topGv
            .Select(x => new ChartPointDto { Label = gvNames.GetValueOrDefault(x.Ma, x.Ma), Value = x.Count })
            .ToList();

        var toaNha = await _db.ToaNhas
            .Select(t => new
            {
                t.MaToaNha,
                t.CodeToaNha,
                SoPhong = t.PhongHocs.Count,
                SanSang = t.PhongHocs.Count(p => p.TrangThaiPhong == "AVAILABLE")
            })
            .ToListAsync();
        var toaNhaUsage = await _db.LichHocs
            .Where(x => x.TrangThaiHoatDong && x.NgayHoc >= start && x.NgayHoc <= end && x.PhongHoc != null)
            .GroupBy(x => x.PhongHoc!.MaToaNha)
            .Select(g => new
            {
                MaToa = g.Key,
                SoCa = g.Count(),
                SoPhong = g.Select(x => x.MaPhong).Distinct().Count()
            })
            .ToListAsync();
        dto.ToaNhaStats = toaNha
            .Select(t =>
            {
                var usage = toaNhaUsage.FirstOrDefault(x => x.MaToa == t.MaToaNha);
                var soPhongCoLich = usage?.SoPhong ?? 0;
                return new DonViStatDto
                {
                    Label = t.CodeToaNha,
                    SoPhong = t.SoPhong,
                    PhongSanSang = t.SanSang,
                    SoCa = usage?.SoCa ?? 0,
                    SoPhongCoLich = soPhongCoLich,
                    TyLeLapDay = t.SoPhong == 0 ? 0 : Math.Round(soPhongCoLich * 100.0 / t.SoPhong, 1)
                };
            })
            .OrderByDescending(x => x.SoCa)
            .ThenBy(x => x.Label)
            .ToList();

        dto.ThietBiTheoTrangThai = await _db.PhongThietBis
            .GroupBy(x => x.TrangThai)
            .Select(g => new ChartPointDto { Label = g.Key, Value = g.Count() })
            .ToListAsync();

        return dto;
    }
}
