using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;
using System.Security.Claims;
using ClosedXML.Excel;

namespace QuanLiPhongHoc.Web.Controllers;

[Authorize(Roles = AppRoles.BusinessUsers)]
public class SuCoController : Controller
{
    private const int PageSize = 20;
    private const long MaxAnhSuCoByte = 10 * 1024 * 1024;
    private const string ThuMucAnhSuCo = "uploads/suco";

    private readonly QuanLiPhongHocContext _db;
    private readonly ISuCoService _service;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public SuCoController(QuanLiPhongHocContext db, ISuCoService service, IAuditService audit, IWebHostEnvironment env)
    {
        _db = db;
        _service = service;
        _audit = audit;
        _env = env;
    }

    public async Task<IActionResult> Index(int page = 1, int? toa = null, string? tt = null, string? q = null, string? mucdo = null)
    {
        var laCsvc = User.IsInRole(AppRoles.Facility);
        var maNguoiDung = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tuKhoa = q?.Trim();

        var query = _db.SuCos.AsNoTracking()
            .Include(x => x.PhongHoc).ThenInclude(x => x!.ToaNha)
            .Include(x => x.NguoiBaoCaoNav)
            .Include(x => x.NguoiXuLyNav)
            .Include(x => x.PhongDieuChuyenNav)
            .Include(x => x.ChiTietSuCos).ThenInclude(x => x.PhongThietBi).ThenInclude(x => x!.ThietBi)
            .AsQueryable();

        // Người dùng chỉ thấy đơn mình gửi; CSVC thấy toàn bộ hộp thư tiếp nhận.
        if (!laCsvc)
            query = query.Where(x => x.NguoiBaoCao == maNguoiDung);
        if (toa is int maToa)
            query = query.Where(x => x.PhongHoc != null && x.PhongHoc.MaToaNha == maToa);
        if (!string.IsNullOrWhiteSpace(tt))
            query = query.Where(x => x.TrangThai == tt);
        if (!string.IsNullOrWhiteSpace(mucdo))
            query = query.Where(x => x.MucDoNghiemTrong == mucdo);
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var soSuCo = int.TryParse(new string(tuKhoa.Where(char.IsDigit).ToArray()), out var so) ? so : 0;
            query = query.Where(x => x.MaSuCo == soSuCo
                || (x.MoTa != null && x.MoTa.Contains(tuKhoa))
                || (x.PhongHoc != null && x.PhongHoc.CodePhong.Contains(tuKhoa))
                || (x.NguoiBaoCaoNav != null && x.NguoiBaoCaoNav.HoTen.Contains(tuKhoa))
                || (x.HoTenNguoiBaoCao != null && x.HoTenNguoiBaoCao.Contains(tuKhoa))
                || (x.NguoiBaoCao != null && x.NguoiBaoCao.Contains(tuKhoa)));
        }

        // Mới tiếp nhận trước, đang xử lý sau, đã xong cuối; trong mỗi nhóm ưu tiên mức độ nặng.
        query = query
            .OrderBy(x => x.TrangThai == "NEW" ? 0 : x.TrangThai == "IN_PROGRESS" ? 1 : 2)
            .ThenBy(x => x.MucDoNghiemTrong == "CRITICAL" ? 0
                : x.MucDoNghiemTrong == "HIGH" ? 1
                : x.MucDoNghiemTrong == "MEDIUM" ? 2 : 3)
            .ThenByDescending(x => x.NgayTao);

        var total = await query.CountAsync();
        var soTrang = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, soTrang);

        var soPhanHoiMoi = laCsvc
            ? 0
            : await _db.SuCos.CountAsync(x => x.NguoiBaoCao == maNguoiDung && !x.DaXemPhanHoi && x.ThongBaoPhanHoi != null);

        var ds = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        // Người báo cáo mở hộp thư là coi như đã đọc phản hồi.
        if (!laCsvc && soPhanHoiMoi > 0)
        {
            await _db.SuCos
                .Where(x => x.NguoiBaoCao == maNguoiDung && !x.DaXemPhanHoi && x.ThongBaoPhanHoi != null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.DaXemPhanHoi, true));
        }

        ViewBag.Page = page;
        ViewBag.SoTrang = soTrang;
        ViewBag.TongSo = total;
        ViewBag.Toa = toa;
        ViewBag.TrangThai = tt;
        ViewBag.Q = tuKhoa;
        ViewBag.MucDo = mucdo;
        ViewBag.LaCsvc = laCsvc;
        ViewBag.SoPhanHoiMoi = soPhanHoiMoi;
        ViewBag.SoMoi = laCsvc ? await _db.SuCos.CountAsync(x => x.TrangThai == "NEW") : 0;
        ViewBag.SoCanXuLy = laCsvc ? await _db.SuCos.CountAsync(x => x.TrangThai != "RESOLVED") : 0;
        ViewBag.DanhSachToa = await _db.ToaNhas.AsNoTracking()
            .OrderBy(x => x.CodeToaNha)
            .ToListAsync();
        ViewBag.TenToa = toa is int id
            ? await _db.ToaNhas.AsNoTracking().Where(x => x.MaToaNha == id)
                .Select(x => x.CodeToaNha + " — " + x.TenToaNha).FirstOrDefaultAsync()
            : null;
        ViewBag.PhongDieuChuyen = await _db.PhongHocs.AsNoTracking()
            .OrderBy(x => x.CodePhong)
            .ToListAsync();
        return View(ds);
    }

    public async Task<IActionResult> Create(int? maPhong, int? maPhongThietBi)
    {
        await FillCreateLookupsAsync();
        var vm = new SuCoFormVm
        {
            DoiTuongBaoCao = User.IsInRole(AppRoles.Student) ? "SINH_VIEN" : "GIANG_VIEN"
        };
        if (maPhong is int mp && await _db.PhongHocs.AnyAsync(x => x.MaPhong == mp))
        {
            vm.MaPhong = mp;
            if (maPhongThietBi is int ptbId)
            {
                var tb = await _db.PhongThietBis.Include(x => x.ThietBi)
                    .FirstOrDefaultAsync(x => x.MaPhongThietBi == ptbId && x.MaPhong == mp);
                if (tb != null)
                {
                    vm.MaPhongThietBi = tb.MaPhongThietBi;
                    vm.MoTa = $"Thiết bị {tb.NhanDienThietBi} ({tb.ThietBi?.TenThietBi}) cần kiểm tra/sửa chữa.";
                    vm.GhiChuLoi = "Thiết bị hỏng / cần sửa chữa";
                    vm.MucDoNghiemTrong = tb.TrangThai == "DAMAGED" ? "HIGH" : "MEDIUM";
                }
            }
        }
        await NapThongTinNguoiBaoCaoAsync(vm);
        ViewBag.MaPhongThietBi = vm.MaPhongThietBi;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuCoFormVm vm)
    {
        if (!await _db.PhongHocs.AnyAsync(x => x.MaPhong == vm.MaPhong))
            ModelState.AddModelError(nameof(vm.MaPhong), "Chọn phòng gặp sự cố.");
        if (vm.MucDoNghiemTrong is not ("MEDIUM" or "HIGH" or "CRITICAL"))
            ModelState.AddModelError(nameof(vm.MucDoNghiemTrong), "Chọn mức độ ảnh hưởng.");
        if (vm.DoiTuongBaoCao is not ("SINH_VIEN" or "GIANG_VIEN"))
            ModelState.AddModelError(nameof(vm.DoiTuongBaoCao), "Chọn đối tượng báo cáo.");
        if (string.IsNullOrWhiteSpace(vm.LoaiSuCo) || !Catalogs.LoaiSuCo.ContainsKey(vm.LoaiSuCo))
            ModelState.AddModelError(nameof(vm.LoaiSuCo), "Chọn loại sự cố.");

        var loiAnh = await KiemTraAnhAsync(vm.AnhDinhKem);
        if (loiAnh != null)
            ModelState.AddModelError(nameof(vm.AnhDinhKem), loiAnh);

        if (!ModelState.IsValid)
        {
            await FillCreateLookupsAsync();
            await NapThongTinNguoiBaoCaoAsync(vm);
            ViewBag.MaPhongThietBi = vm.MaPhongThietBi;
            return View(vm);
        }

        var anhDinhKem = vm.AnhDinhKem is { Length: > 0 } ? await LuuAnhSuCoAsync(vm.AnhDinhKem) : null;

        var suCo = new SuCo
        {
            MaPhong = vm.MaPhong,
            NguoiBaoCao = User.FindFirstValue(ClaimTypes.NameIdentifier),
            LoaiSuCo = vm.LoaiSuCo,
            MoTa = vm.MoTa?.Trim(),
            MucDoNghiemTrong = vm.MucDoNghiemTrong,
            DoiTuongBaoCao = vm.DoiTuongBaoCao,
            HoTenNguoiBaoCao = vm.HoTenNguoiBaoCao?.Trim(),
            SoDienThoaiLienHe = vm.SoDienThoaiLienHe?.Trim(),
            NoiDungGopY = string.IsNullOrWhiteSpace(vm.NoiDungGopY) ? null : vm.NoiDungGopY.Trim(),
            AnhDinhKem = anhDinhKem
        };
        await _service.CreateAsync(suCo, vm.MaPhongThietBi, vm.GhiChuLoi);
        await _audit.WriteAsync(suCo.NguoiBaoCao, "ACTION_CREATE", "SuCo", null, vm.MoTa, HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = anhDinhKem is null
            ? "Đã ghi nhận sự cố. Nếu mức độ cao, hệ thống tự đồng bộ trạng thái thiết bị/phòng."
            : "Đã ghi nhận sự cố kèm ảnh hiện trường. CSVC sẽ kiểm tra và phản hồi trong mục Báo cáo sự cố.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> XuLy(SuCoXuLyVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.KetQua) && string.IsNullOrWhiteSpace(vm.TrangThai)
            && string.IsNullOrWhiteSpace(vm.DanhGia) && string.IsNullOrWhiteSpace(vm.ThongBao))
        {
            TempData["Error"] = "Chọn trạng thái, nhập đánh giá hoặc chọn kết quả xử lý trước khi lưu.";
            return QuayLai(vm.ReturnUrl);
        }

        var (ok, error) = await _service.XuLyAsync(vm.MaSuCo, vm.DanhGia, vm.KetQua,
            vm.PhongDieuChuyen, vm.ThongBao, vm.TrangThai, User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (!ok)
        {
            TempData["Error"] = error ?? "Không xử lý được báo cáo.";
            return QuayLai(vm.ReturnUrl);
        }

        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "SuCo",
            vm.MaSuCo.ToString(), $"{vm.TrangThai}|{vm.KetQua}|{vm.PhongDieuChuyen}|{vm.ThongBao}",
            HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = string.IsNullOrWhiteSpace(vm.KetQua)
            ? "Đã lưu đánh giá và trạng thái xử lý."
            : $"Đã lưu kết quả \"{Catalogs.Label(Catalogs.KetQuaSuCo, vm.KetQua)}\" và gửi thông báo cho người báo cáo.";
        return QuayLai(vm.ReturnUrl);
    }

    /// <summary>Xuất hộp thư báo cáo sự cố ra Excel để gửi kèm biên bản họp.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Export(int? toa = null, string? tt = null)
    {
        var query = _db.SuCos.AsNoTracking()
            .Include(x => x.PhongHoc)
            .Include(x => x.NguoiBaoCaoNav)
            .Include(x => x.NguoiXuLyNav)
            .Include(x => x.PhongDieuChuyenNav)
            .AsQueryable();
        if (toa is int maToa)
            query = query.Where(x => x.PhongHoc != null && x.PhongHoc.MaToaNha == maToa);
        if (!string.IsNullOrWhiteSpace(tt))
            query = query.Where(x => x.TrangThai == tt);

        var ds = await query.OrderBy(x => x.TrangThai == "RESOLVED" ? 1 : 0)
            .ThenByDescending(x => x.NgayTao)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BaoCaoSuCo");
        ws.Cell(1, 1).Value = "BÁO CÁO SỰ CỐ CƠ SỞ VẬT CHẤT";
        ws.Range(1, 1, 1, 11).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        ws.Cell(2, 1).Value = $"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm} · {ds.Count} báo cáo · Người lập: {User.Identity?.Name}";
        ws.Range(2, 1, 2, 11).Merge();

        var headers = new[]
        {
            "STT", "Ngày báo cáo", "Phòng", "Người báo cáo", "Loại sự cố", "Mức độ",
            "Trạng thái", "Đánh giá của CSVC", "Kết quả", "Phòng điều chuyển", "Thông báo đã gửi"
        };
        for (var i = 0; i < headers.Length; i++) ws.Cell(4, i + 1).Value = headers[i];
        ws.Range(4, 1, 4, headers.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EDF2"));

        var row = 5;
        foreach (var s in ds)
        {
            ws.Cell(row, 1).Value = row - 4;
            ws.Cell(row, 2).Value = s.NgayTao.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 3).Value = s.PhongHoc?.CodePhong ?? "—";
            ws.Cell(row, 4).Value = s.NguoiBaoCaoNav?.HoTen ?? s.NguoiBaoCao ?? "—";
            ws.Cell(row, 5).Value = Catalogs.LabelLoaiSuCo(s.LoaiSuCo);
            ws.Cell(row, 6).Value = Catalogs.Label(Catalogs.MucDoSuCo, s.MucDoNghiemTrong);
            ws.Cell(row, 7).Value = Catalogs.Label(Catalogs.TrangThaiSuCo, s.TrangThai);
            ws.Cell(row, 8).Value = s.DanhGia ?? "";
            ws.Cell(row, 9).Value = s.KetQuaXuLy is null ? "" : Catalogs.Label(Catalogs.KetQuaSuCo, s.KetQuaXuLy);
            ws.Cell(row, 10).Value = s.PhongDieuChuyenNav?.CodePhong ?? "";
            ws.Cell(row, 11).Value = s.ThongBaoPhanHoi ?? "";
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoSuCo_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    private IActionResult QuayLai(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl)
    {
        if (!Catalogs.TrangThaiSuCo.ContainsKey(status))
        {
            TempData["Error"] = "Trạng thái sự cố không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        await _service.UpdateStatusAsync(id, status);
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "SuCo",
            id.ToString(), status, HttpContext.Connection.RemoteIpAddress?.ToString());
        return QuayLai(returnUrl);
    }

    private async Task FillCreateLookupsAsync()
    {
        var phongs = await _db.PhongHocs.Include(x => x.ToaNha)
            .OrderBy(x => x.CodePhong).ToListAsync();
        ViewBag.Phongs = new SelectList(phongs, "MaPhong", "CodePhong");
        ViewBag.PhongsFull = phongs;
        ViewBag.PhongLoai = phongs.ToDictionary(x => x.MaPhong, x => x.LoaiPhong);
        ViewBag.DanhSachToa = await _db.ToaNhas.AsNoTracking()
            .OrderBy(x => x.CodeToaNha).ToListAsync();
        ViewBag.ThietBis = new SelectList(await _db.PhongThietBis.Include(x => x.ThietBi).ToListAsync(), "MaPhongThietBi", "NhanDienThietBi");
    }

    /// <summary>Họ tên, mã tài khoản và SĐT lấy từ hồ sơ hiện tại; đối tượng mặc định theo vai trò.</summary>
    private async Task NapThongTinNguoiBaoCaoAsync(SuCoFormVm vm)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nd = uid is null
            ? null
            : await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNguoiDung == uid);

        ViewBag.HoTen = nd?.HoTen ?? User.Identity?.Name;
        ViewBag.MaNguoiDung = uid;

        if (vm.DoiTuongBaoCao is not ("SINH_VIEN" or "GIANG_VIEN"))
            vm.DoiTuongBaoCao = User.IsInRole(AppRoles.Student) ? "SINH_VIEN" : "GIANG_VIEN";
        if (string.IsNullOrWhiteSpace(vm.HoTenNguoiBaoCao))
            vm.HoTenNguoiBaoCao = nd?.HoTen ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(vm.SoDienThoaiLienHe))
            vm.SoDienThoaiLienHe = nd?.SoDienThoai;
    }

    /// <summary>Trả về thông báo lỗi nếu ảnh sai định dạng/dung lượng/nội dung; null khi hợp lệ hoặc không đính kèm.</summary>
    private static async Task<string?> KiemTraAnhAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png"))
            return "Chỉ nhận ảnh định dạng JPG hoặc PNG.";
        if (file.Length > MaxAnhSuCoByte)
            return $"Ảnh vượt dung lượng tối đa {MaxAnhSuCoByte / (1024 * 1024)} MB.";

        var dau = new byte[12];
        await using var doc = file.OpenReadStream();
        var n = await doc.ReadAsync(dau.AsMemory(0, 12));
        var laAnh = (n >= 3 && dau[0] == 0xFF && dau[1] == 0xD8 && dau[2] == 0xFF) ||
                    (n >= 4 && dau[0] == 0x89 && dau[1] == 0x50 && dau[2] == 0x4E && dau[3] == 0x47);
        return laAnh ? null : "Nội dung tệp không phải ảnh hợp lệ.";
    }

    private async Task<string> LuuAnhSuCoAsync(IFormFile file)
    {
        var thuMuc = Path.Combine(_env.WebRootPath, "uploads", "suco");
        Directory.CreateDirectory(thuMuc);

        var tenFile = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using var ghi = new FileStream(Path.Combine(thuMuc, tenFile), FileMode.CreateNew, FileAccess.Write);
        await file.CopyToAsync(ghi);
        return $"/{ThuMucAnhSuCo}/{tenFile}";
    }

    /// <summary>
    /// Thiết bị của một phòng; truyền loaiSuCo để chỉ trả các thiết bị khả năng gặp sự cố đó.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ThietBiTheoPhong(int maPhong, string? loaiSuCo)
    {
        var items = await _db.PhongThietBis.AsNoTracking().Include(x => x.ThietBi)
            .Where(x => x.MaPhong == maPhong)
            .Select(x => new { x.MaPhongThietBi, x.NhanDienThietBi, TenThietBi = x.ThietBi!.TenThietBi })
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(loaiSuCo))
            items = items.Where(x => Catalogs.ThietBiKhopLoaiSuCo(x.TenThietBi, loaiSuCo)).ToList();

        return Json(items.Select(x => new
        {
            x.MaPhongThietBi,
            label = x.NhanDienThietBi + " - " + x.TenThietBi
        }));
    }
}
