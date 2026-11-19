using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;

namespace QuanLiPhongHoc.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly QuanLiPhongHocContext _db;

    public HomeController(IDashboardService dashboard, QuanLiPhongHocContext db)
    {
        _dashboard = dashboard;
        _db = db;
    }

    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Admin)) return RedirectToAction(nameof(Admin));
        if (User.IsInRole(AppRoles.Facility)) return RedirectToAction(nameof(Facility));
        if (User.IsInRole(AppRoles.Training)) return RedirectToAction(nameof(Training));
        if (User.IsInRole(AppRoles.Lecturer)) return RedirectToAction(nameof(Lecturer));
        return RedirectToAction(nameof(Student));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Admin()
    {
        await LoadTenNguoiDungAsync();
        ViewBag.Logs = await _db.NhatKyHeThongs.Include(x => x.NguoiThucHien).OrderByDescending(x => x.ThoiGian).Take(10).ToListAsync();
        return View();
    }

    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Facility()
    {
        await LoadTenNguoiDungAsync();
        ViewBag.Kpi = await _dashboard.GetKpisAsync();
        await LoadThongBaoAsync();
        return View();
    }

    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Training()
    {
        await LoadTenNguoiDungAsync();
        ViewBag.Kpi = await _dashboard.GetKpisAsync();
        await LoadThongBaoAsync();
        return View();
    }

    [Authorize(Roles = AppRoles.Lecturer)]
    public async Task<IActionResult> Lecturer()
    {
        await LoadTenNguoiDungAsync();
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var today = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.Lich = await _db.LichHocs.Include(x => x.HocPhan).Include(x => x.PhongHoc)
            .Include(x => x.GiangVienDayThay)
            .Where(x => x.TrangThaiHoatDong
                        && (x.HocPhan!.MaGiangVien == uid || x.MaGiangVienDayThay == uid)
                        && x.NgayHoc >= today)
            .OrderBy(x => x.NgayHoc).Take(10).ToListAsync();
        ViewBag.Kpi = await _dashboard.GetKpisAsync();
        await LoadThongBaoAsync();
        await LoadSuCoPhanHoiAsync();
        return View();
    }

    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Student()
    {
        await LoadTenNguoiDungAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.Lich = await _db.LichHocs.Include(x => x.HocPhan).Include(x => x.PhongHoc)
            .Include(x => x.GiangVienDayThay)
            .Where(x => x.TrangThaiHoatDong && x.NgayHoc >= today)
            .OrderBy(x => x.NgayHoc).Take(10).ToListAsync();
        ViewBag.Kpi = await _dashboard.GetKpisAsync();
        await LoadThongBaoAsync();
        await LoadSuCoPhanHoiAsync();
        return View();
    }

    /// <summary>Tên hiển thị lấy từ hồ sơ hiện tại, tránh claim cũ còn lưu trong cookie sau khi sửa họ tên.</summary>
    private async Task LoadTenNguoiDungAsync()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.HoTen = uid is null
            ? null
            : await _db.NguoiDungs.Where(x => x.MaNguoiDung == uid).Select(x => x.HoTen).FirstOrDefaultAsync();
    }

    /// <summary>Tin nổi bật hiển thị ở cột ảnh lớn; các tin còn lại (kể cả tin nổi bật vượt quá 2 slot) vào danh sách dấu ».</summary>
    private async Task LoadThongBaoAsync()
    {
        var tinMoi = await _db.ThongBaos.VisibleTo(User)
            .OrderByDescending(x => x.NgayDang)
            .Take(20)
            .ToListAsync();

        var noiBat = tinMoi.Where(x => x.NoiBat).Take(2).ToList();
        var daHienThi = noiBat.Select(x => x.MaThongBao).ToHashSet();

        ViewBag.ThongBao = new ThongBaoHomeVm
        {
            TinNoiBat = noiBat,
            TinKhac = tinMoi.Where(x => !daHienThi.Contains(x.MaThongBao)).Take(8).ToList()
        };
    }

    /// <summary>Phản hồi CSVC chưa đọc, hiển thị banner nhắc người báo cáo mở trang sự cố.</summary>
    private async Task LoadSuCoPhanHoiAsync()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.SuCoPhanHoi = string.IsNullOrEmpty(uid)
            ? new List<SuCo>()
            : await _db.SuCos.Include(x => x.PhongHoc)
                .Where(x => x.NguoiBaoCao == uid && !x.DaXemPhanHoi && x.ThongBaoPhanHoi != null)
                .OrderByDescending(x => x.NgayXuLy)
                .Take(3)
                .ToListAsync();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
