using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;
using System.Security.Claims;

namespace QuanLiPhongHoc.Web.Controllers;

[Authorize(Roles = AppRoles.BusinessUsers)]
public class DonXinNghiController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IDonXinNghiService _service;
    private readonly IAuditService _audit;

    public DonXinNghiController(QuanLiPhongHocContext db, IDonXinNghiService service, IAuditService audit)
    {
        _db = db;
        _service = service;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, string? trangThai, string? phuongAn, DateOnly? ngay, string? sort)
    {
        var isTraining = User.IsInRole(AppRoles.Training);
        var q0 = _db.DonXinNghis.Include(x => x.HocPhan).Include(x => x.GiangVien).Include(x => x.GiangVienDayThay).AsQueryable();
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole(AppRoles.Lecturer) && !isTraining)
            q0 = q0.Where(x => x.MaGiangVien == uid);

        ViewBag.PendingCount = await q0.CountAsync(x => x.TrangThai == "PENDING");
        ViewBag.ApprovedCount = await q0.CountAsync(x => x.TrangThai == "APPROVED");

        var status = isTraining && string.IsNullOrEmpty(trangThai) ? "PENDING" : trangThai;
        var query = q0;
        if (!string.IsNullOrEmpty(status) && status != "ALL") query = query.Where(x => x.TrangThai == status);
        if (!string.IsNullOrEmpty(phuongAn) && phuongAn != "ALL") query = query.Where(x => x.PhuongAnGiaiQuyet == phuongAn);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.GiangVien!.HoTen.Contains(q)
                || x.MaGiangVien!.Contains(q)
                || x.HocPhan!.TenHocPhan.Contains(q)
                || x.MaHocPhan!.Contains(q));
        if (ngay is not null)
        {
            var from = ngay.Value.ToDateTime(TimeOnly.MinValue);
            var to = from.AddDays(1);
            query = query.Where(x => x.NgayTao >= from && x.NgayTao < to);
        }

        var list = await (sort == "oldest"
            ? query.OrderBy(x => x.NgayTao)
            : query.OrderByDescending(x => x.NgayTao)).Take(100).ToListAsync();

        var courseIds = list.Where(x => x.MaHocPhan != null).Select(x => x.MaHocPhan!).Distinct().ToList();
        var schedules = await _db.LichHocs.Include(x => x.PhongHoc)
            .Where(x => x.MaHocPhan != null && courseIds.Contains(x.MaHocPhan))
            .OrderBy(x => x.NgayHoc).ThenBy(x => x.CaHoc)
            .ToListAsync();
        ViewBag.Schedules = schedules.GroupBy(x => x.MaHocPhan!).ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.Q = q;
        ViewBag.TrangThai = status;
        ViewBag.PhuongAn = phuongAn;
        ViewBag.Ngay = ngay;
        ViewBag.Sort = sort ?? "newest";
        return View(list);
    }

    [Authorize(Roles = AppRoles.Lecturer)]
    public async Task<IActionResult> Create()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var hps = await _db.HocPhans.Where(x => x.MaGiangVien == uid).ToListAsync();
        ViewBag.HocPhans = new SelectList(hps, "MaHocPhan", "TenHocPhan");
        ViewBag.Gvs = new SelectList(await _db.NguoiDungs.Where(x => x.NguoiDungVaiTros.Any(v => v.VaiTro.CodeVaiTro == AppRoles.Lecturer)).ToListAsync(), "MaNguoiDung", "HoTen");
        return View(new DonXinNghiFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Lecturer)]
    public async Task<IActionResult> Create(DonXinNghiFormVm vm)
    {
        var don = new DonXinNghi
        {
            MaGiangVien = User.FindFirstValue(ClaimTypes.NameIdentifier),
            MaHocPhan = vm.MaHocPhan,
            LyDo = vm.LyDo,
            PhuongAnGiaiQuyet = vm.PhuongAnGiaiQuyet,
            MaGiangVienDayThay = vm.PhuongAnGiaiQuyet.StartsWith("SUBSTITUTE") ? vm.MaGiangVienDayThay : null
        };
        await _service.CreateAsync(don);
        await _audit.WriteAsync(don.MaGiangVien, "ACTION_CREATE", "DonXinNghi", null, vm.PhuongAnGiaiQuyet, HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Đã gửi đơn xin nghỉ / dạy bù.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Approve(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, messages) = await _service.ApproveAsync(id, uid);
        await _audit.WriteAsync(uid, "ACTION_APPROVE", "DonXinNghi", id.ToString(),
            ok ? "APPROVED" : "FAIL", HttpContext.Connection.RemoteIpAddress?.ToString());

        var thongDiep = messages.Count > 0 ? string.Join(" ", messages) : null;
        if (ok)
            TempData["Ok"] = thongDiep ?? "Đã duyệt đơn.";
        else
            TempData["Error"] = thongDiep ?? "Không duyệt được đơn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Reject(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _service.RejectAsync(id, uid);
        return RedirectToAction(nameof(Index));
    }
}
