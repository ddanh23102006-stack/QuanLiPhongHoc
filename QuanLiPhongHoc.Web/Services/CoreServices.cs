using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public class DashboardKpiDto
{
    public int TongPhong { get; set; }
    public int PhongSanSang { get; set; }
    public int PhongBaoTri { get; set; }
    public int LichHomNay { get; set; }
    public int SuCoMoi { get; set; }
    public int DonDatCho { get; set; }
    public int DonNghiCho { get; set; }
    public int TaiKhoan { get; set; }
    public double TyLeLapDay { get; set; }
    public DateTime CapNhat { get; set; }
}

public class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public interface IDashboardService
{
    Task<DashboardKpiDto> GetKpisAsync(bool forceRefresh = false);
    Task<List<ChartPointDto>> OccupancyByBuildingAsync();
    Task<List<ChartPointDto>> IncidentsByStatusAsync();
    void Invalidate();
}

public class DashboardService : IDashboardService
{
    public const string CacheKey = "dashboard:kpis";
    private readonly QuanLiPhongHocContext _db;
    private readonly IMemoryCache _cache;

    public DashboardService(QuanLiPhongHocContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<DashboardKpiDto> GetKpisAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(CacheKey, out DashboardKpiDto? cached) && cached != null)
            return cached;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var tong = await _db.PhongHocs.CountAsync();
        var sanSang = await _db.PhongHocs.CountAsync(x => x.TrangThaiPhong == "AVAILABLE");
        var baoTri = await _db.PhongHocs.CountAsync(x => x.TrangThaiPhong == "MAINTENANCE" || x.TrangThaiPhong == "UNAVAILABLE");
        var lich = await _db.LichHocs.CountAsync(x => x.NgayHoc == today && x.TrangThaiHoatDong);
        var dto = new DashboardKpiDto
        {
            TongPhong = tong,
            PhongSanSang = sanSang,
            PhongBaoTri = baoTri,
            LichHomNay = lich,
            SuCoMoi = await _db.SuCos.CountAsync(x => x.TrangThai == "NEW"),
            DonDatCho = await _db.DatPhongs.CountAsync(x => x.TrangThai == "PENDING"),
            DonNghiCho = await _db.DonXinNghis.CountAsync(x => x.TrangThai == "PENDING"),
            TaiKhoan = await _db.NguoiDungs.CountAsync(),
            TyLeLapDay = tong == 0 ? 0 : Math.Round(lich * 100.0 / tong, 1),
            CapNhat = DateTime.Now
        };
        _cache.Set(CacheKey, dto, TimeSpan.FromSeconds(30));
        return dto;
    }

    public async Task<List<ChartPointDto>> OccupancyByBuildingAsync()
    {
        return await _db.ToaNhas
            .Select(t => new ChartPointDto
            {
                Label = t.CodeToaNha,
                Value = t.PhongHocs.Count
            }).ToListAsync();
    }

    public async Task<List<ChartPointDto>> IncidentsByStatusAsync()
    {
        return await _db.SuCos
            .GroupBy(x => x.TrangThai ?? "NEW")
            .Select(g => new ChartPointDto { Label = g.Key, Value = g.Count() })
            .ToListAsync();
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}

public interface ILichHocService
{
    Task<(bool Ok, List<string> Errors)> CreateAsync(LichHoc lich);
    Task<(bool Ok, List<string> Errors)> UpdateAsync(LichHoc lich);
    Task CancelAsync(int id);
}

public class LichHocService : ILichHocService
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IConflictDetectionService _conflict;
    private readonly IDashboardService _dashboard;

    public LichHocService(QuanLiPhongHocContext db, IConflictDetectionService conflict, IDashboardService dashboard)
    {
        _db = db;
        _conflict = conflict;
        _dashboard = dashboard;
    }

    public async Task<(bool Ok, List<string> Errors)> CreateAsync(LichHoc lich)
    {
        if (string.IsNullOrWhiteSpace(lich.MaHocPhan))
            return (false, ["Chưa chọn học phần."]);
        if (lich.MaPhong is null)
            return (false, ["Chưa chọn phòng."]);

        var check = await _conflict.CheckLichHocAsync(
            lich.MaPhong.Value, lich.NgayHoc, lich.CaHoc, lich.MaHocPhan,
            maGiangVienDayThay: lich.MaGiangVienDayThay);
        if (check.HasConflict) return (false, check.Messages);

        lich.TrangThaiHoatDong = true;
        _db.LichHocs.Add(lich);
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, []);
    }

    public async Task<(bool Ok, List<string> Errors)> UpdateAsync(LichHoc lich)
    {
        if (string.IsNullOrWhiteSpace(lich.MaHocPhan))
            return (false, ["Chưa chọn học phần."]);
        if (lich.TrangThaiHoatDong)
        {
            if (lich.MaPhong is not int maPhong)
                return (false, ["Chưa chọn phòng."]);

            var check = await _conflict.CheckLichHocAsync(
                maPhong, lich.NgayHoc, lich.CaHoc, lich.MaHocPhan,
                lich.MaLichHoc, lich.MaGiangVienDayThay);
            if (check.HasConflict) return (false, check.Messages);
        }

        _db.LichHocs.Update(lich);
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, []);
    }

    public async Task CancelAsync(int id)
    {
        var lich = await _db.LichHocs.FindAsync(id);
        if (lich == null) return;
        lich.TrangThaiHoatDong = false;
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
    }
}

public interface IDatPhongService
{
    Task<(bool Ok, List<string> Errors)> CreateAsync(DatPhong don);
    Task<(bool Ok, List<string> Errors)> ApproveAsync(int id, string approverId);
    Task RejectAsync(int id, string approverId);
    Task<(bool Ok, List<string> Errors)> CancelAsync(int id, string nguoiYeuCau);
}

public class DatPhongService : IDatPhongService
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IConflictDetectionService _conflict;
    private readonly IDashboardService _dashboard;

    public DatPhongService(QuanLiPhongHocContext db, IConflictDetectionService conflict, IDashboardService dashboard)
    {
        _db = db;
        _conflict = conflict;
        _dashboard = dashboard;
    }

    public async Task<(bool Ok, List<string> Errors)> CreateAsync(DatPhong don)
    {
        if (don.MaPhong is null) return (false, ["Chưa chọn phòng."]);

        var errors = new List<string>();
        var check = await _conflict.CheckMuonPhongAsync(don.MaPhong.Value, don.NgayDat, don.GioBatDau, don.GioKetThuc);
        errors.AddRange(check.Messages);

        var phong = await _db.PhongHocs.Include(x => x.ToaNha)
            .FirstOrDefaultAsync(x => x.MaPhong == don.MaPhong.Value);
        if (phong is { HienThiDangKyMuon: false })
            errors.Add($"Phòng {phong.CodePhong} chưa mở đăng ký mượn.");
        if (phong != null && !Catalogs.ToaNhaDangHoatDong(phong.ToaNha))
            errors.Add($"Tòa nhà {phong.ToaNha!.TenToaNha} đang bảo trì/ngừng hoạt động nên phòng {phong.CodePhong} tạm không nhận phiếu mượn.");
        if (phong != null && don.SoLuongNguoiThamGia is int so && so > phong.SucChua)
            errors.Add($"{phong.CodePhong} chỉ chứa tối đa {phong.SucChua} người, phiếu ghi {so} người.");
        if (don.NgayDat <= DateOnly.FromDateTime(DateTime.Today))
            errors.Add("Phiếu phải gửi trước ngày sử dụng ít nhất 24 giờ.");

        if (errors.Count > 0) return (false, errors);

        don.TrangThai = "PENDING";
        _db.DatPhongs.Add(don);
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, []);
    }

    public async Task<(bool Ok, List<string> Errors)> ApproveAsync(int id, string approverId)
    {
        var don = await _db.DatPhongs.FindAsync(id);
        if (don == null) return (false, ["Không tìm thấy đơn."]);
        if (don.MaPhong is null) return (false, ["Đơn thiếu mã phòng."]);
        // Tòa nhà có thể chuyển sang bảo trì sau khi phiếu được gửi — chặn duyệt để
        // không đẩy lịch sử dụng vào tòa đang ngừng hoạt động.
        var phongDuyet = await _db.PhongHocs.AsNoTracking().Include(x => x.ToaNha)
            .FirstOrDefaultAsync(x => x.MaPhong == don.MaPhong.Value);
        if (phongDuyet is not null && !Catalogs.ToaNhaDangHoatDong(phongDuyet.ToaNha))
            return (false, [$"Tòa nhà {phongDuyet.ToaNha!.TenToaNha} đang bảo trì/ngừng hoạt động — không thể duyệt phiếu cho phòng {phongDuyet.CodePhong}."]);
        // Khi duyệt chỉ cần chống với lịch học và phiếu đã duyệt; phiếu đang chờ khác
        // chưa giữ phòng nên không được phép chặn việc duyệt.
        var check = await _conflict.CheckMuonPhongAsync(don.MaPhong.Value, don.NgayDat, don.GioBatDau, don.GioKetThuc,
            excludeDatPhongId: don.MaDatPhong, includePending: false);
        if (check.HasConflict) return (false, check.Messages);
        don.TrangThai = "APPROVED";
        don.NguoiDuyet = approverId;
        don.ThoiDiemXuLy = DateTime.Now;
        don.DaXemKetQua = false;
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, []);
    }

    public async Task RejectAsync(int id, string approverId)
    {
        var don = await _db.DatPhongs.FindAsync(id);
        if (don == null) return;
        don.TrangThai = "REJECTED";
        don.NguoiDuyet = approverId;
        don.ThoiDiemXuLy = DateTime.Now;
        don.DaXemKetQua = false;
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
    }

    public async Task<(bool Ok, List<string> Errors)> CancelAsync(int id, string nguoiYeuCau)
    {
        var don = await _db.DatPhongs.FirstOrDefaultAsync(x =>
            x.MaDatPhong == id && x.NguoiYeuCau == nguoiYeuCau && x.TrangThai == "PENDING");
        if (don == null) return (false, ["Chỉ hủy được phiếu chờ duyệt của chính mình."]);
        don.TrangThai = "CANCELLED";
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, []);
    }
}

public interface ISuCoService
{
    Task CreateAsync(SuCo suCo, int? maPhongThietBi, string? ghiChuLoi);
    Task UpdateStatusAsync(int id, string status);

    /// <summary>
    /// CSVC tiếp nhận báo cáo: ghi đánh giá, đổi trạng thái và gửi thông báo phục hồi/điều chuyển phòng.
    /// </summary>
    Task<(bool Ok, string? Error)> XuLyAsync(int id, string? danhGia, string? ketQua, int? phongDieuChuyenId,
        string? thongBao, string? trangThai, string? nguoiXuLy);
}

public class SuCoService : ISuCoService
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IDashboardService _dashboard;

    public SuCoService(QuanLiPhongHocContext db, IDashboardService dashboard)
    {
        _db = db;
        _dashboard = dashboard;
    }

    public async Task CreateAsync(SuCo suCo, int? maPhongThietBi, string? ghiChuLoi)
    {
        suCo.TrangThai = "NEW";
        suCo.NgayTao = DateTime.Now;
        _db.SuCos.Add(suCo);
        await _db.SaveChangesAsync();

        if (maPhongThietBi.HasValue)
        {
            _db.ChiTietSuCos.Add(new ChiTietSuCo
            {
                MaSuCo = suCo.MaSuCo,
                MaPhongThietBi = maPhongThietBi,
                GhiChuLoi = ghiChuLoi
            });
        }

        var high = string.Equals(suCo.MucDoNghiemTrong, "HIGH", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(suCo.MucDoNghiemTrong, "CRITICAL", StringComparison.OrdinalIgnoreCase);

        if (high && maPhongThietBi.HasValue)
        {
            var tb = await _db.PhongThietBis.FindAsync(maPhongThietBi.Value);
            if (tb != null) tb.TrangThai = "DAMAGED";
        }

        if (high && suCo.MaPhong.HasValue &&
            suCo.LoaiSuCo != null && Catalogs.SuCoHaTang.Contains(suCo.LoaiSuCo))
        {
            var phong = await _db.PhongHocs.FindAsync(suCo.MaPhong.Value);
            if (phong != null) phong.TrangThaiPhong = "MAINTENANCE";
        }

        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        var suCo = await _db.SuCos.FindAsync(id);
        if (suCo == null) return;
        suCo.TrangThai = status;
        if (status == "RESOLVED" && suCo.MaPhong.HasValue)
        {
            var phong = await _db.PhongHocs.FindAsync(suCo.MaPhong.Value);
            if (phong != null && phong.TrangThaiPhong == "MAINTENANCE")
                phong.TrangThaiPhong = "AVAILABLE";
        }
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
    }

    public async Task<(bool Ok, string? Error)> XuLyAsync(int id, string? danhGia, string? ketQua,
        int? phongDieuChuyenId, string? thongBao, string? trangThai, string? nguoiXuLy)
    {
        var suCo = await _db.SuCos
            .Include(x => x.PhongHoc)
            .Include(x => x.ChiTietSuCos).ThenInclude(x => x.PhongThietBi)
            .FirstOrDefaultAsync(x => x.MaSuCo == id);
        if (suCo == null) return (false, "Không tìm thấy báo cáo sự cố.");

        if (!string.IsNullOrWhiteSpace(ketQua) && !Catalogs.KetQuaSuCo.ContainsKey(ketQua))
            return (false, "Kết quả xử lý không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(trangThai) && !Catalogs.TrangThaiSuCo.ContainsKey(trangThai))
            return (false, "Trạng thái báo cáo không hợp lệ.");

        PhongHoc? dich = null;
        if (ketQua == "TRANSFERRED")
        {
            if (phongDieuChuyenId is not int maDich || maDich == suCo.MaPhong)
                return (false, "Chọn phòng đích khác phòng đang báo cáo để điều chuyển.");
            dich = await _db.PhongHocs.FirstOrDefaultAsync(x => x.MaPhong == maDich);
            if (dich == null) return (false, "Phòng đích không tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(danhGia)) suCo.DanhGia = danhGia.Trim();
        if (!string.IsNullOrWhiteSpace(trangThai)) suCo.TrangThai = trangThai;
        suCo.NguoiXuLy = nguoiXuLy;
        suCo.NgayXuLy = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(ketQua))
        {
            suCo.KetQuaXuLy = ketQua;
            suCo.TrangThai = "RESOLVED";
            suCo.PhongDieuChuyen = ketQua == "TRANSFERRED" ? phongDieuChuyenId : null;

            if (ketQua == "RESTORED")
            {
                foreach (var ct in suCo.ChiTietSuCos)
                {
                    if (ct.PhongThietBi is { } tb && tb.TrangThai is "DAMAGED" or "REPAIRING" or "NEEDS_REPAIR")
                        tb.TrangThai = "GOOD";
                }
                if (suCo.PhongHoc is { TrangThaiPhong: "MAINTENANCE" } phong)
                    phong.TrangThaiPhong = "AVAILABLE";
            }
            else if (dich != null)
            {
                // Điều chuyển phòng: chuyển thiết bị được báo lỗi sang phòng đích để tiếp tục phục vụ.
                foreach (var ct in suCo.ChiTietSuCos)
                {
                    if (ct.PhongThietBi is { } tb) tb.MaPhong = dich.MaPhong;
                }
            }

            var maPhongText = suCo.PhongHoc?.CodePhong ?? "—";
            var macDinh = ketQua == "RESTORED"
                ? $"Sự cố tại phòng {maPhongText} đã được phục hồi."
                : $"Sự cố tại phòng {maPhongText} đã được xử lý bằng phương án điều chuyển sang phòng {dich!.CodePhong}.";
            suCo.ThongBaoPhanHoi = string.IsNullOrWhiteSpace(thongBao) ? macDinh : thongBao.Trim();
            suCo.DaXemPhanHoi = false;
        }
        else if (!string.IsNullOrWhiteSpace(thongBao))
        {
            suCo.ThongBaoPhanHoi = thongBao.Trim();
            suCo.DaXemPhanHoi = false;
        }

        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, null);
    }
}

public interface IDonXinNghiService
{
    Task CreateAsync(DonXinNghi don);

    /// <summary>Trả về thông điệp thành công (ví dụ ngày dạy bù đã xếp) hoặc lỗi khi không duyệt được.</summary>
    Task<(bool Ok, List<string> Messages)> ApproveAsync(int id, string approverId);
    Task RejectAsync(int id, string approverId);
}

public class DonXinNghiService : IDonXinNghiService
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IDashboardService _dashboard;
    private readonly IConflictDetectionService _conflict;

    public DonXinNghiService(QuanLiPhongHocContext db, IDashboardService dashboard, IConflictDetectionService conflict)
    {
        _db = db;
        _dashboard = dashboard;
        _conflict = conflict;
    }

    public async Task CreateAsync(DonXinNghi don)
    {
        don.TrangThai = "PENDING";
        don.NgayTao = DateTime.Now;
        _db.DonXinNghis.Add(don);
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
    }

    public async Task<(bool Ok, List<string> Messages)> ApproveAsync(int id, string approverId)
    {
        var don = await _db.DonXinNghis.Include(x => x.HocPhan).FirstOrDefaultAsync(x => x.MaDon == id);
        if (don == null) return (false, ["Không tìm thấy đơn."]);
        if (don.TrangThai != "PENDING") return (false, ["Đơn đã được xử lý trước đó."]);

        var messages = new List<string>();

        if (don.PhuongAnGiaiQuyet.StartsWith("SUBSTITUTE", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(don.MaGiangVienDayThay) || don.HocPhan == null)
                return (false, ["Đơn dạy thay thiếu giảng viên dạy thay hoặc học phần."]);

            don.HocPhan.MaGiangVien = don.MaGiangVienDayThay;
            messages.Add($"Đã chuyển học phần {don.MaHocPhan} cho giảng viên {don.MaGiangVienDayThay}.");
        }
        else if (don.PhuongAnGiaiQuyet == "MAKEUP_CLASS")
        {
            if (don.HocPhan == null || string.IsNullOrWhiteSpace(don.MaHocPhan))
                return (false, ["Đơn dạy bù thiếu học phần."]);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var buoiNghi = await _db.LichHocs
                .Where(x => x.MaHocPhan == don.MaHocPhan && x.TrangThaiHoatDong
                            && x.MaPhong != null && x.NgayHoc >= today)
                .OrderBy(x => x.NgayHoc).ThenBy(x => x.CaHoc)
                .FirstOrDefaultAsync();
            if (buoiNghi is null)
                return (false, [$"Học phần {don.MaHocPhan} không còn buổi học sắp tới để báo nghỉ."]);

            var buoiBu = await TimBuoiDayBuAsync(don.MaHocPhan, buoiNghi);
            if (buoiBu is null)
                return (false, [$"Không tìm được phòng trống để dạy bù {don.MaHocPhan} trong 4 tuần tới. " +
                                "Hãy xếp lịch bù thủ công rồi duyệt lại."]);

            buoiNghi.TrangThaiHoatDong = false;
            _db.LichHocs.Add(buoiBu);
            messages.Add($"Đã hủy buổi {buoiNghi.NgayHoc:dd/MM/yyyy} ca {Catalogs.LabelCa(buoiNghi.CaHoc)} " +
                         $"và xếp bù ngày {buoiBu.NgayHoc:dd/MM/yyyy} ca {Catalogs.LabelCa(buoiBu.CaHoc)}.");
        }

        don.TrangThai = "APPROVED";
        don.NguoiDuyet = approverId;
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        return (true, messages);
    }

    /// <summary>
    /// Tìm ngày dạy bù sớm nhất trong 4 tuần, giữ nguyên phòng và ca của buổi nghỉ,
    /// chỉ nhận ngày không vướng lịch phòng, lịch giảng viên hay lịch khác của học phần.
    /// </summary>
    private async Task<LichHoc?> TimBuoiDayBuAsync(string maHocPhan, LichHoc buoiNghi)
    {
        var ngayCuoi = buoiNghi.NgayHoc.AddDays(28);
        for (var ngay = buoiNghi.NgayHoc.AddDays(1); ngay <= ngayCuoi; ngay = ngay.AddDays(1))
        {
            if (ngay.DayOfWeek == DayOfWeek.Sunday) continue;

            var check = await _conflict.CheckLichHocAsync(buoiNghi.MaPhong!.Value, ngay, buoiNghi.CaHoc, maHocPhan);
            if (check.HasConflict) continue;

            return new LichHoc
            {
                MaHocPhan = maHocPhan,
                MaPhong = buoiNghi.MaPhong,
                NgayHoc = ngay,
                CaHoc = buoiNghi.CaHoc,
                TrangThaiHoatDong = true
            };
        }
        return null;
    }

    public async Task RejectAsync(int id, string approverId)
    {
        var don = await _db.DonXinNghis.FindAsync(id);
        if (don == null) return;
        don.TrangThai = "REJECTED";
        don.NguoiDuyet = approverId;
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
    }
}
