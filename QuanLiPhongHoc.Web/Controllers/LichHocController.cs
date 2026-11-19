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
public class LichHocController : Controller
{
    private const int PageSize = 250;

    private readonly QuanLiPhongHocContext _db;
    private readonly ILichHocService _service;
    private readonly IAuditService _audit;
    private readonly ICauHinhCaHocService _cauHinh;

    public LichHocController(QuanLiPhongHocContext db, ILichHocService service, IAuditService audit, ICauHinhCaHocService cauHinh)
    {
        _db = db;
        _service = service;
        _audit = audit;
        _cauHinh = cauHinh;
    }

    public async Task<IActionResult> Index(DateOnly? week, DateOnly? ngay, int? building, string? loai, int page = 1)
    {
        var anchor = ngay ?? week ?? DateOnly.FromDateTime(DateTime.Today);
        var start = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var end = start.AddDays(6);

        var query = _db.LichHocs.Include(x => x.HocPhan).ThenInclude(x => x!.GiangVien)
            .Include(x => x.GiangVienDayThay)
            .Include(x => x.PhongHoc)
            .Where(x => x.NgayHoc >= start && x.NgayHoc <= end);
        if (building is not null) query = query.Where(x => x.PhongHoc!.MaToaNha == building);
        if (User.IsInRole(AppRoles.Lecturer) && !User.IsInRole(AppRoles.Training))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            query = query.Where(x => x.HocPhan!.MaGiangVien == uid || x.MaGiangVienDayThay == uid);
        }

        ViewBag.CountAll = await query.CountAsync();
        ViewBag.CountTheory = await query.CountAsync(x => x.HocPhan!.LaThucHanh == false);
        ViewBag.CountPractice = await query.CountAsync(x => x.HocPhan!.LaThucHanh);
        ViewBag.CountCancelled = await query.CountAsync(x => !x.TrangThaiHoatDong);

        if (loai == "theory") query = query.Where(x => x.HocPhan!.LaThucHanh == false);
        else if (loai == "practice") query = query.Where(x => x.HocPhan!.LaThucHanh);

        var total = await query.CountAsync();
        var soTrang = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, soTrang);

        ViewBag.WeekStart = start;
        ViewBag.WeekEnd = end;
        ViewBag.PrevWeek = start.AddDays(-7);
        ViewBag.NextWeek = start.AddDays(7);
        ViewBag.Building = building;
        ViewBag.Loai = loai;
        ViewBag.CurrentPage = page;
        ViewBag.TotalRooms = await _db.PhongHocs.CountAsync();
        ViewBag.Buildings = await _db.ToaNhas.OrderBy(x => x.CodeToaNha).ToListAsync();
        ViewBag.Page = page;
        ViewBag.SoTrang = soTrang;
        ViewBag.TongSo = total;
        ViewBag.TuCa = (page - 1) * PageSize + 1;
        ViewBag.DenCa = Math.Min(page * PageSize, total);
        if (User.IsInRole(AppRoles.Training))
            await Fill();

        return View(await query.OrderBy(x => x.NgayHoc).ThenBy(x => x.CaHoc)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
    }

    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Export(DateOnly? week, int? building, string? loai)
    {
        var anchor = week ?? DateOnly.FromDateTime(DateTime.Today);
        var start = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var end = start.AddDays(6);
        var query = _db.LichHocs.Include(x => x.HocPhan).ThenInclude(x => x!.GiangVien)
            .Include(x => x.GiangVienDayThay)
            .Include(x => x.PhongHoc)
            .Where(x => x.NgayHoc >= start && x.NgayHoc <= end);
        if (building is not null) query = query.Where(x => x.PhongHoc!.MaToaNha == building);
        if (loai == "theory") query = query.Where(x => x.HocPhan!.LaThucHanh == false);
        else if (loai == "practice") query = query.Where(x => x.HocPhan!.LaThucHanh);

        var rows = await query.OrderBy(x => x.NgayHoc).ThenBy(x => x.CaHoc).ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã lịch,Ngày học,Ca,Học phần,Giảng viên,Phòng,Trạng thái");
        foreach (var x in rows)
        {
            sb.AppendLine(string.Join(",",
                x.MaLichHoc,
                x.NgayHoc.ToString("dd/MM/yyyy"),
                Catalogs.LabelCa(x.CaHoc),
                Csv(x.HocPhan?.TenHocPhan),
                Csv(x.GiangVienDayThay?.HoTen ?? x.HocPhan?.GiangVien?.HoTen),
                Csv(x.PhongHoc?.CodePhong),
                x.TrangThaiHoatDong ? "Hoạt động" : "Đã hủy"));
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"TKB_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
    }

    private static string Csv(string? value) =>
        string.IsNullOrEmpty(value) ? "" : $"\"{value.Replace("\"", "\"\"")}\"";

    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Create(DateOnly? ngay, string? ca)
    {
        await Fill();
        return View(new LichHocFormVm
        {
            NgayHoc = ngay ?? DateOnly.FromDateTime(DateTime.Today),
            CaHoc = string.IsNullOrEmpty(ca) ? "CA_S1" : ca
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Create(LichHocFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await Fill();
            return View(vm);
        }
        var lich = new LichHoc
        {
            MaHocPhan = vm.MaHocPhan,
            MaPhong = vm.MaPhong,
            NgayHoc = vm.NgayHoc,
            CaHoc = vm.CaHoc,
            TrangThaiHoatDong = true,
            MaGiangVienDayThay = vm.MaGiangVienDayThay,
            GhiChuDieuPhoi = vm.GhiChuDieuPhoi
        };
        var (ok, errors) = await _service.CreateAsync(lich);
        if (!ok)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            await Fill();
            return View(vm);
        }
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_CREATE", "LichHoc", null, $"{vm.MaHocPhan}|{vm.MaPhong}|{vm.NgayHoc}|{vm.CaHoc}", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Đã xếp lịch. Hệ thống đã kiểm tra trùng phòng và trùng giảng viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Edit(LichHocFormVm vm, DateOnly? week, int? building, string? loai, int page = 1)
    {
        var lich = await _db.LichHocs
            .Include(x => x.HocPhan)
            .FirstOrDefaultAsync(x => x.MaLichHoc == vm.MaLichHoc);
        if (lich is null) return NotFound();

        if (string.IsNullOrWhiteSpace(lich.MaHocPhan) || lich.HocPhan is null)
        {
            TempData["Error"] = "Lịch này không còn học phần hợp lệ để điều phối.";
            return RedirectToAction(nameof(Index), new { week, building, loai, page });
        }

        if (!string.IsNullOrWhiteSpace(vm.MaGiangVienDayThay))
        {
            var laGiangVien = await _db.NguoiDungs.AnyAsync(x =>
                x.MaNguoiDung == vm.MaGiangVienDayThay
                && x.NguoiDungVaiTros.Any(v => v.VaiTro.CodeVaiTro == AppRoles.Lecturer));
            if (!laGiangVien)
            {
                TempData["Error"] = "Giảng viên thay thế không tồn tại hoặc không có vai trò giảng viên.";
                return RedirectToAction(nameof(Index), new { week, building, loai, page });
            }
        }

        lich.MaPhong = vm.MaPhong;
        lich.NgayHoc = vm.NgayHoc;
        lich.CaHoc = vm.CaHoc;
        lich.TrangThaiHoatDong = vm.TrangThaiHoatDong;
        lich.MaGiangVienDayThay = string.Equals(
            vm.MaGiangVienDayThay, lich.HocPhan.MaGiangVien, StringComparison.OrdinalIgnoreCase)
            ? null
            : vm.MaGiangVienDayThay;
        lich.GhiChuDieuPhoi = string.IsNullOrWhiteSpace(vm.GhiChuDieuPhoi)
            ? null
            : vm.GhiChuDieuPhoi.Trim();

        var (ok, errors) = await _service.UpdateAsync(lich);
        if (!ok)
        {
            TempData["Error"] = string.Join(" ", errors);
            return RedirectToAction(nameof(Index), new { week, building, loai, page });
        }

        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "LichHoc",
            lich.MaLichHoc.ToString(),
            $"{lich.MaHocPhan}|{lich.NgayHoc}|{lich.CaHoc}|{lich.MaPhong}|{lich.MaGiangVienDayThay}|{lich.TrangThaiHoatDong}",
            HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Đã cập nhật ca học thủ công.";
        return RedirectToAction(nameof(Index), new { week, building, loai, page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> CauHinhCaHoc(List<CaHocSetting> caHocs, DateOnly? week, int? building, string? loai)
    {
        var ds = caHocs ?? [];
        var (ok, errors) = await _cauHinh.LuuAsync(ds);
        if (!ok)
        {
            TempData["Error"] = string.Join(" ", errors);
        }
        else
        {
            await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "LichHoc", null,
                string.Join("|", ds.Select(c => $"{c.Code} {c.BatDau}-{c.KetThuc}")),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Ok"] = "Đã lưu cấu hình khung giờ ca học.";
        }
        return RedirectToAction(nameof(Index), new { week, building, loai });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Cancel(int id, DateOnly? week, int? building, string? loai, int page = 1)
    {
        await _service.CancelAsync(id);
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "LichHoc", id.ToString(), "Hủy hoạt động", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Ca học đã được chuyển sang trạng thái báo nghỉ.";
        return RedirectToAction(nameof(Index), new { week, building, loai, page });
    }

    [HttpGet]
    public async Task<IActionResult> CheckConflict(
        int maPhong, DateOnly ngay, string ca, string? maHocPhan,
        string? maGiangVienDayThay, int? excludeLichHocId)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IConflictDetectionService>();
        // Có học phần -> kiểm tra đầy đủ 5 ràng buộc (phòng, giảng viên, lớp, sức chứa, loại phòng).
        var r = string.IsNullOrEmpty(maHocPhan)
            ? await svc.CheckScheduleAsync(maPhong, ngay, ca, null)
            : await svc.CheckLichHocAsync(
                maPhong, ngay, ca, maHocPhan, excludeLichHocId, maGiangVienDayThay);
        return Json(new { conflict = r.HasConflict, messages = r.Messages });
    }

    private async Task Fill()
    {
        ViewBag.HocPhans = new SelectList(await _db.HocPhans.ToListAsync(), "MaHocPhan", "TenHocPhan");
        var phongs = await _db.PhongHocs.AsNoTracking()
            .Include(x => x.ToaNha)
            .Where(x => x.TrangThaiPhong == "AVAILABLE")
            .OrderBy(x => x.CodePhong)
            .ToListAsync();
        ViewBag.Phongs = phongs.Where(x => Catalogs.ToaNhaDangHoatDong(x.ToaNha)).ToList();
        ViewBag.GiangViens = await _db.NguoiDungs.AsNoTracking()
            .Where(x => x.NguoiDungVaiTros.Any(v => v.VaiTro.CodeVaiTro == AppRoles.Lecturer))
            .OrderBy(x => x.HoTen)
            .ToListAsync();
        ViewBag.CaHoc = new SelectList(
            Catalogs.CaHoc.Select(x => new { x.Code, Label = $"{x.Label} ({x.Time})" }), "Code", "Label");
    }
}
