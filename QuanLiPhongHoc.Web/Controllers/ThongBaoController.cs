using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;
using System.Security.Claims;
using IoFile = System.IO.File;

namespace QuanLiPhongHoc.Web.Controllers;

public class ThongBaoController : Controller
{
    private const int PageSize = 10;

    /// <summary>Ảnh do admin tải lên nằm trong thư mục con này của wwwroot.</summary>
    private const string ThuMucAnh = "uploads/thongbao";

    private string ThuMucAnhTuyetDoi => Path.Combine(_env.WebRootPath, "uploads", "thongbao");

    private readonly QuanLiPhongHocContext _db;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public ThongBaoController(QuanLiPhongHocContext db, IAuditService audit, IWebHostEnvironment env)
    {
        _db = db;
        _audit = audit;
        _env = env;
    }

    [Authorize(Roles = AppRoles.Everyone)]
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = _db.ThongBaos.VisibleTo(User);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.TieuDe.Contains(q) || x.NoiDung.Contains(q));

        var total = await query.CountAsync();
        var soTrang = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, soTrang);

        ViewBag.Q = q;
        ViewBag.Page = page;
        ViewBag.SoTrang = soTrang;
        ViewBag.TongSo = total;
        return View(await query
            .OrderByDescending(x => x.NgayDang)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync());
    }

    [Authorize(Roles = AppRoles.Everyone)]
    public async Task<IActionResult> Details(int id)
    {
        var tin = await _db.ThongBaos.VisibleTo(User).FirstOrDefaultAsync(x => x.MaThongBao == id);
        if (tin is null) return NotFound();

        var hoTen = await HoTenTaiKhoanAsync([tin.NguoiDang]);

        return View(new ThongBaoDetailVm
        {
            ThongBao = tin,
            NguoiQuanTri = TinhTenNguoiQuanTri(tin.NguoiDang, hoTen),
            BaiVietLienQuan = await _db.ThongBaos.VisibleTo(User)
                .Where(x => x.MaThongBao != id)
                .OrderByDescending(x => x.NgayDang)
                .Take(6)
                .ToListAsync()
        });
    }

    [Authorize(Roles = AppRoles.ThongBaoAccess)]
    public async Task<IActionResult> Manage()
    {
        var list = await _db.ThongBaos.OrderByDescending(x => x.NgayDang).ToListAsync();
        ViewBag.NguoiQuanTri = await HoTenTaiKhoanAsync(list.Select(x => x.NguoiDang));
        return View(list);
    }

    [Authorize(Roles = AppRoles.Training)]
    public IActionResult Create() => View("Form", new ThongBaoFormVm());

    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Edit(int id)
    {
        var tin = await _db.ThongBaos.FindAsync(id);
        if (tin is null) return NotFound();

        return View("Form", new ThongBaoFormVm
        {
            MaThongBao = tin.MaThongBao,
            TieuDe = tin.TieuDe,
            NoiDung = tin.NoiDung,
            AnhDaiDien = tin.AnhDaiDien,
            DoiTuong = tin.DoiTuong,
            LaMoi = tin.LaMoi,
            NoiBat = tin.NoiBat,
            TrangThai = tin.TrangThai,
            NgayDang = tin.NgayDang
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Save(ThongBaoFormVm vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        var taiKhoan = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ThongBao? tin;

        if (vm.MaThongBao == 0)
        {
            tin = new ThongBao();
            _db.ThongBaos.Add(tin);
        }
        else
        {
            tin = await _db.ThongBaos.FindAsync(vm.MaThongBao);
            if (tin is null) return NotFound();
        }

        tin.TieuDe = vm.TieuDe;
        tin.NoiDung = vm.NoiDung;
        tin.DoiTuong = vm.DoiTuong;
        tin.LaMoi = vm.LaMoi;
        tin.NoiBat = vm.NoiBat;
        tin.TrangThai = vm.TrangThai;
        tin.NgayDang = vm.NgayDang;
        tin.NguoiDang = taiKhoan;

        var anhCu = tin.AnhDaiDien;
        if (vm.AnhFile is not null && vm.AnhFile.Length > 0)
        {
            var anhMoi = await LuuAnhNenThangAsync(vm.AnhFile);
            if (anhMoi is null) return View("Form", vm);
            tin.AnhDaiDien = anhMoi;
        }
        else if (vm.XoaAnh)
        {
            tin.AnhDaiDien = null;
        }
        else
        {
            tin.AnhDaiDien = string.IsNullOrWhiteSpace(vm.AnhDaiDien) ? null : vm.AnhDaiDien.Trim();
        }

        await _db.SaveChangesAsync();
        await XoaAnhKhongDungNuaAsync(anhCu);
        await _audit.WriteAsync(taiKhoan, vm.MaThongBao == 0 ? "ACTION_CREATE" : "ACTION_UPDATE",
            "ThongBao", tin.MaThongBao.ToString(), tin.TieuDe,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Ok"] = "Đã lưu thông báo.";
        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Training)]
    public async Task<IActionResult> Delete(int id)
    {
        var tin = await _db.ThongBaos.FindAsync(id);
        if (tin is null) return NotFound();

        _db.ThongBaos.Remove(tin);
        await _db.SaveChangesAsync();
        await XoaAnhKhongDungNuaAsync(tin.AnhDaiDien);
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_DELETE",
            "ThongBao", id.ToString(), tin.TieuDe, HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Ok"] = "Đã xóa thông báo.";
        return RedirectToAction(nameof(Manage));
    }

    /// <summary>Mã tài khoản → "Họ tên (mã)" phục vụ cột / dòng Người quản trị.</summary>
    private async Task<Dictionary<string, string>> HoTenTaiKhoanAsync(IEnumerable<string?> maCanTra)
    {
        var codes = maCanTra.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (codes.Count == 0) return [];

        return await _db.NguoiDungs
            .Where(x => codes.Contains(x.MaNguoiDung))
            .ToDictionaryAsync(x => x.MaNguoiDung, x => $"{x.HoTen} ({x.MaNguoiDung})");
    }

    /// <summary>Tài khoản đã bị xóa thì fallback về mã cũ thay vì để trống.</summary>
    private static string? TinhTenNguoiQuanTri(string? maTaiKhoan, IReadOnlyDictionary<string, string> hoTen)
        => string.IsNullOrWhiteSpace(maTaiKhoan)
            ? null
            : hoTen.TryGetValue(maTaiKhoan, out var ten) ? ten : maTaiKhoan;

    /// <summary>
    /// Kiểm tra định dạng, dung lượng và chữ ký nhị phân của ảnh rồi lưu vào wwwroot/uploads/thongbao.
    /// Trả về đường dẫn công khai, hoặc null khi file không hợp lệ (kèm lỗi trong ModelState).
    /// </summary>
    private async Task<string?> LuuAnhNenThangAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (Array.IndexOf(ThongBaoFormVm.DinhDanhChoPhep, ext) < 0)
        {
            ModelState.AddModelError(nameof(ThongBaoFormVm.AnhFile),
                $"Chỉ nhận ảnh định dạng {string.Join(", ", ThongBaoFormVm.DinhDanhChoPhep)}.");
            return null;
        }
        if (file.Length > ThongBaoFormVm.MaxDungLuongByte)
        {
            ModelState.AddModelError(nameof(ThongBaoFormVm.AnhFile),
                $"Ảnh vượt dung lượng tối đa {ThongBaoFormVm.MaxDungLuongByte / (1024 * 1024)} MB.");
            return null;
        }

        var dau = new byte[12];
        await using (var doc = file.OpenReadStream())
        {
            var n = await doc.ReadAsync(dau.AsMemory(0, 12));
            if (n < 12 || !LaAnhBitmap(dau.AsSpan(0, n)))
            {
                ModelState.AddModelError(nameof(ThongBaoFormVm.AnhFile),
                    "Nội dung tệp không phải ảnh hợp lệ.");
                return null;
            }
        }

        var thuMuc = ThuMucAnhTuyetDoi;
        Directory.CreateDirectory(thuMuc);

        // Không dùng tên file do người dùng gửi: tên tự sinh chặn ghi đè và chặn thư mục cha.
        var tenFile = $"{Guid.NewGuid():N}{ext}";
        await using (var ghi = new FileStream(Path.Combine(thuMuc, tenFile), FileMode.CreateNew, FileAccess.Write))
        {
            await file.CopyToAsync(ghi);
        }

        return $"/{ThuMucAnh}/{tenFile}";
    }

    private static bool LaAnhBitmap(ReadOnlySpan<byte> b) =>
        (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) ||
        (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) ||
        (b.Length >= 4 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) ||
        (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
         b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50);

    /// <summary>Xóa file ảnh do hệ thống tải lên khi không còn bản tin nào tham chiếu; ảnh seed trong /img được giữ nguyên.</summary>
    private async Task XoaAnhKhongDungNuaAsync(string? duongDan)
    {
        if (string.IsNullOrWhiteSpace(duongDan) ||
            !duongDan.StartsWith($"/{ThuMucAnh}/", StringComparison.OrdinalIgnoreCase))
            return;

        if (await _db.ThongBaos.AnyAsync(x => x.AnhDaiDien == duongDan)) return;

        var tenFile = Path.GetFileName(duongDan);
        if (string.IsNullOrEmpty(tenFile)) return;

        var dayDu = Path.Combine(ThuMucAnhTuyetDoi, tenFile);
        if (IoFile.Exists(dayDu)) IoFile.Delete(dayDu);
    }
}
