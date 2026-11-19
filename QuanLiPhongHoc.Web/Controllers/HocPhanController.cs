using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;
using System.Security.Claims;

namespace QuanLiPhongHoc.Web.Controllers;

[Authorize(Roles = AppRoles.Training)]
public class HocPhanController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IExcelImportService _excel;
    private readonly IAutoSchedulingService _scheduler;
    private readonly IAuditService _audit;

    public HocPhanController(QuanLiPhongHocContext db, IExcelImportService excel, IAutoSchedulingService scheduler, IAuditService audit)
    {
        _db = db;
        _excel = excel;
        _scheduler = scheduler;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, string? loaiLop, string? trangThaiLich)
    {
        var query = _db.HocPhans
            .AsNoTracking()
            .Include(x => x.GiangVien)
            .Include(x => x.LichHocs)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.MaHocPhan.Contains(q) || x.TenHocPhan.Contains(q));

        if (loaiLop == "theory")
            query = query.Where(x => !x.LaThucHanh);
        else if (loaiLop == "practice")
            query = query.Where(x => x.LaThucHanh);

        if (trangThaiLich == "scheduled")
            query = query.Where(x => x.LichHocs.Any(l => l.TrangThaiHoatDong));
        else if (trangThaiLich == "unscheduled")
            query = query.Where(x => !x.LichHocs.Any(l => l.TrangThaiHoatDong));

        ViewBag.Q = q;
        ViewBag.LoaiLop = loaiLop;
        ViewBag.TrangThaiLich = trangThaiLich;
        return View(await query.OrderBy(x => x.MaHocPhan).ToListAsync());
    }

    [HttpGet]
    public IActionResult Import() => View(new ImportResult());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn tệp Excel (.xlsx).");
            return View(new ImportResult());
        }

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Chỉ hỗ trợ định dạng .xlsx.");
            return View(new ImportResult());
        }

        await using var stream = file.OpenReadStream();
        var result = await _excel.ImportHocPhanAsync(stream);

        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_IMPORT", "HocPhan",
            null, $"Thêm {result.Inserted}, cập nhật {result.Updated}, tạo {result.LecturersCreated} giảng viên, lỗi {result.Errors.Count}",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        // Không nhập được dòng nào: ở lại trang để người dùng xem chi tiết lỗi.
        if (result.Inserted + result.Updated == 0)
        {
            ModelState.AddModelError(string.Empty, "Không nhập được học phần nào. Xem chi tiết lỗi bên dưới.");
            return View(result);
        }

        // Sau khi nhập, mở danh mục để nhân viên rà soát dữ liệu vừa cập nhật.
        TempData["Ok"] = $"Nhập xong: thêm mới {result.Inserted}, cập nhật {result.Updated}"
            + (result.LecturersCreated > 0 ? $", tạo mới {result.LecturersCreated} giảng viên" : "")
            + (result.Errors.Count > 0 ? $", {result.Errors.Count} dòng lỗi" : "")
            + ". Đang chuyển sang danh mục học phần…";
        if (result.Errors.Count > 0)
            TempData["ImportErrors"] = result.Errors;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AutoSchedule()
    {
        await FillScheduleLookups();
        return View(new AutoScheduleOptions());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoSchedule(AutoScheduleOptions form, List<string>? maHocPhans, List<string>? caHocs)
    {
        var options = new AutoScheduleOptions
        {
            StartDate = form.StartDate,
            EndDate = form.EndDate,
            MaxNodes = form.MaxNodes <= 0 ? 200_000 : form.MaxNodes,
            CaHocs = caHocs is { Count: > 0 } ? caHocs : Catalogs.CaHoc.Select(x => x.Code).ToList(),
            Persist = true
        };

        if (options.EndDate < options.StartDate)
            (options.StartDate, options.EndDate) = (options.EndDate, options.StartDate);

        var selected = maHocPhans is { Count: > 0 }
            ? maHocPhans
            : await _db.HocPhans.Select(x => x.MaHocPhan).ToListAsync();

        var result = await _scheduler.RunAsync(selected, options);

        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_AUTO_SCHEDULE", "LichHoc",
            null, $"Xếp {result.Assigned.Count} lớp, duyệt {result.NodesExplored} nút",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        await FillScheduleLookups();
        ViewBag.Result = result;
        ViewBag.Options = options;
        TempData[result.Success ? "Ok" : "Error"] = result.Message;
        return View(options);
    }

    /// <summary>
    /// Xuất thời khóa biểu vừa xếp tự động ra file Excel (môn học, số tiết, giảng viên...).
    /// Nhận danh sách lớp đã xếp từ trang kết quả dưới dạng JSON để không phải chạy lại thuật toán.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportAutoSchedule(string? data)
    {
        List<SlotAssignment> rows = [];
        if (!string.IsNullOrWhiteSpace(data))
        {
            rows = System.Text.Json.JsonSerializer.Deserialize<List<SlotAssignment>>(data,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("TKB");
        string[] headers = ["STT", "Mã học phần", "Môn học", "Số tiết", "Giảng viên", "Phòng", "Ngày", "Ca"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var x in rows.OrderBy(x => x.NgayHoc).ThenBy(x => x.CaHoc))
        {
            ws.Cell(r, 1).Value = r - 1;
            ws.Cell(r, 2).Value = x.MaHocPhan;
            ws.Cell(r, 3).Value = x.TenHocPhan;
            ws.Cell(r, 4).Value = x.SoTiet;
            ws.Cell(r, 5).Value = string.IsNullOrWhiteSpace(x.TenGiangVien)
                ? x.MaGiangVien ?? ""
                : x.TenGiangVien!;
            ws.Cell(r, 6).Value = x.CodePhong;
            ws.Cell(r, 7).Value = x.NgayHoc.ToString("dd/MM/yyyy");
            ws.Cell(r, 8).Value = $"{Catalogs.LabelCa(x.CaHoc)} ({Catalogs.CaTimeRange(x.CaHoc)})";
            r++;
        }
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_AUTO_SCHEDULE", "LichHoc",
            null, $"Xuất Excel {rows.Count} lớp đã xếp", HttpContext.Connection.RemoteIpAddress?.ToString());
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ThoiKhoaBieu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    /// <summary>
    /// Làm mới dữ liệu xếp lịch: xóa toàn bộ lịch đã xếp và toàn bộ danh mục học phần
    /// để nhân viên đào tạo import dữ liệu mới và chạy lại xếp lịch từ đầu.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAll()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Đơn xin nghỉ tham chiếu học phần với ràng buộc Restrict: gỡ liên kết trước khi xóa học phần.
        await _db.DonXinNghis
            .Where(x => x.MaHocPhan != null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaHocPhan, (string?)null));

        var lichCount = await _db.LichHocs.ExecuteDeleteAsync();
        var hocPhanCount = await _db.HocPhans.ExecuteDeleteAsync();
        await transaction.CommitAsync();

        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_DELETE", "HocPhan", null,
            $"Làm mới dữ liệu: xóa {lichCount} lịch đã xếp và {hocPhanCount} học phần",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Ok"] = $"Đã làm mới dữ liệu: xóa {lichCount} lịch đã xếp và {hocPhanCount} học phần. " +
                         "Bạn có thể import dữ liệu mới.";
        return RedirectToAction(nameof(Import));
    }

    public async Task<IActionResult> Create()
    {
        await FillLecturers();
        return View(new HocPhanFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HocPhanFormVm vm)
    {
        if (await _db.HocPhans.AnyAsync(x => x.MaHocPhan == vm.MaHocPhan))
            ModelState.AddModelError(nameof(vm.MaHocPhan), "Mã học phần đã tồn tại.");
        if (vm.LaThucHanh && vm.NhomNganh == Catalogs.NganhDaiCuong)
            ModelState.AddModelError(nameof(vm.NhomNganh), "Nhóm Đại cương không có phòng thực hành — chọn nhóm ngành chuyên môn hoặc bỏ cờ thực hành.");
        if (!ModelState.IsValid)
        {
            await FillLecturers();
            return View(vm);
        }

        _db.HocPhans.Add(new HocPhan
        {
            MaHocPhan = vm.MaHocPhan,
            TenHocPhan = vm.TenHocPhan,
            SoLuongDangKy = vm.SoLuongDangKy,
            MaGiangVien = vm.MaGiangVien,
            LaThucHanh = vm.LaThucHanh,
            NhomNganh = vm.NhomNganh
        });
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_CREATE", "HocPhan", null, vm.MaHocPhan, HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Đã thêm học phần.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var hp = await _db.HocPhans.FindAsync(id);
        if (hp is null) return NotFound();
        await FillLecturers();
        return View(new HocPhanFormVm
        {
            MaHocPhan = hp.MaHocPhan,
            TenHocPhan = hp.TenHocPhan,
            SoLuongDangKy = hp.SoLuongDangKy,
            MaGiangVien = hp.MaGiangVien,
            LaThucHanh = hp.LaThucHanh,
            NhomNganh = hp.NhomNganh
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HocPhanFormVm vm)
    {
        var hp = await _db.HocPhans.FindAsync(vm.MaHocPhan);
        if (hp is null) return NotFound();
        if (!ModelState.IsValid)
        {
            await FillLecturers();
            return View(vm);
        }

        var old = $"{hp.TenHocPhan}|{hp.SoLuongDangKy}|{hp.MaGiangVien}|{hp.LaThucHanh}|{hp.NhomNganh}";
        hp.TenHocPhan = vm.TenHocPhan;
        hp.SoLuongDangKy = vm.SoLuongDangKy;
        hp.MaGiangVien = vm.MaGiangVien;
        hp.LaThucHanh = vm.LaThucHanh;
        hp.NhomNganh = vm.NhomNganh;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "HocPhan", old,
            $"{hp.TenHocPhan}|{hp.SoLuongDangKy}|{hp.MaGiangVien}|{hp.LaThucHanh}|{hp.NhomNganh}", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Đã cập nhật học phần.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var hp = await _db.HocPhans.FindAsync(id);
        if (hp is not null)
        {
            _db.HocPhans.Remove(hp);
            await _db.SaveChangesAsync();
            await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_DELETE", "HocPhan", id, null, HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Ok"] = "Đã xóa học phần.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task FillLecturers()
    {
        ViewBag.GiangViens = await _db.NguoiDungs
            .Where(x => x.NguoiDungVaiTros.Any(v => v.VaiTro.CodeVaiTro == AppRoles.Lecturer))
            .OrderBy(x => x.HoTen)
            .ToListAsync();
    }

    private async Task FillScheduleLookups()
    {
        ViewBag.HocPhans = await _db.HocPhans.OrderBy(x => x.MaHocPhan).ToListAsync();
        ViewBag.CaHocList = Catalogs.CaHoc;
        ViewBag.Phongs = await _db.PhongHocs.Include(x => x.ToaNha).Where(x => x.TrangThaiPhong == "AVAILABLE").ToListAsync();
    }
}
