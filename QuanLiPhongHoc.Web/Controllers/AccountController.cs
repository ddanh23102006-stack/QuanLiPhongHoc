using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;

namespace QuanLiPhongHoc.Web.Controllers;

public class AccountController : Controller
{
    /// <summary>Liên kết đặt lại mật khẩu chỉ có hiệu lực trong 30 phút kể từ lúc gửi.</summary>
    private static readonly TimeSpan ThoiHanToken = TimeSpan.FromMinutes(30);

    private readonly QuanLiPhongHocContext _db;
    private readonly IAuditService _audit;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        QuanLiPhongHocContext db,
        IAuditService audit,
        IEmailSender email,
        IOptions<EmailOptions> emailOptions,
        IWebHostEnvironment env,
        ILogger<AccountController> logger)
    {
        _db = db;
        _audit = audit;
        _email = email;
        _emailOptions = emailOptions.Value;
        _env = env;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.NguoiDungs
            .Include(x => x.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
            .FirstOrDefaultAsync(x => x.MaNguoiDung == model.MaNguoiDung || x.Email == model.MaNguoiDung);

        var ketQuaMatKhau = user is null
            ? KetQuaKiemTraMatKhau.Sai
            : MatKhauHasher.KiemTra(model.MatKhau, user.MatKhau);

        if (user == null || ketQuaMatKhau == KetQuaKiemTraMatKhau.Sai)
        {
            ModelState.AddModelError(string.Empty, "Mã TDMU ID hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (user.BiKhoa)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Liên hệ quản trị viên.");
            return View(model);
        }

        // Tài khoản tạo trước khi có PBKDF2 vẫn đăng nhập được đúng một lần,
        // sau đó mật khẩu được băm lại ngay để CSDL không còn lưu chuỗi thô.
        if (ketQuaMatKhau == KetQuaKiemTraMatKhau.DungCanBamLai)
        {
            user.MatKhau = MatKhauHasher.Bam(model.MatKhau);
            await _db.SaveChangesAsync();
        }

        var roles = user.NguoiDungVaiTros.Select(x => x.VaiTro.CodeVaiTro).ToList();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.MaNguoiDung),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Email, user.Email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties
        {
            IsPersistent = model.GhiNho,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.GhiNho ? 24 : 8)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);
        await _audit.WriteAsync(user.MaNguoiDung, "ACTION_LOGIN", "NguoiDung", null, "Đăng nhập thành công", HttpContext.Connection.RemoteIpAddress?.ToString());

        // Không quay lại returnUrl nếu đó là dashboard của vai trò khác: người dùng sẽ thấy
        // giao diện không thuộc quyền mình (ví dụ Nhân viên đào tạo mở /Home/Student trước khi
        // đăng nhập rồi bị đưa thẳng vào Cổng sinh viên).
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !IsDashboardPath(returnUrl))
            return Redirect(returnUrl);

        return RedirectToDashboard(roles);
    }

    private IActionResult RedirectToDashboard(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.Admin)) return RedirectToAction("Admin", "Home");
        if (roles.Contains(AppRoles.Facility)) return RedirectToAction("Facility", "Home");
        if (roles.Contains(AppRoles.Training)) return RedirectToAction("Training", "Home");
        if (roles.Contains(AppRoles.Lecturer)) return RedirectToAction("Lecturer", "Home");
        return RedirectToAction("Student", "Home");
    }

    private static bool IsDashboardPath(string returnUrl)
    {
        var path = returnUrl.Split('?', '#')[0].TrimEnd('/');
        return path.Length == 0
            || path.Equals("/Home", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/Index", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/Admin", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/Facility", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/Training", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/Lecturer", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/Student", StringComparison.OrdinalIgnoreCase);
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var khoa = vm.EmailHoacMa.Trim();
        var nguoiDung = await _db.NguoiDungs
            .FirstOrDefaultAsync(x => x.MaNguoiDung == khoa || x.Email == khoa);

        string? lienKetDaGui = null;
        if (nguoiDung is not null)
        {
            var token = TokenDatLaiMatKhau.Tao();

            // Mỗi tài khoản chỉ giữ một yêu cầu còn hiệu lực: yêu cầu mới vô hiệu yêu cầu cũ.
            _db.MatKhauDatLais.RemoveRange(
                await _db.MatKhauDatLais.Where(x => x.MaNguoiDung == nguoiDung.MaNguoiDung).ToListAsync());
            _db.MatKhauDatLais.Add(new DatLaiMatKhau
            {
                MaNguoiDung = nguoiDung.MaNguoiDung,
                TokenHash = TokenDatLaiMatKhau.Bam(token),
                HetHan = DateTime.UtcNow.Add(ThoiHanToken),
                NgayTao = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            try
            {
                var link = TaoLienKetDatLai(nguoiDung.Email, token);
                await _email.SendAsync(
                    nguoiDung.Email,
                    "Đặt lại mật khẩu - Hệ thống Quản lý Phòng học TDMU",
                    NoiDungEmailDatLai(nguoiDung.HoTen, nguoiDung.MaNguoiDung, link));
                lienKetDaGui = link;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không gửi được email đặt lại mật khẩu cho {Email}.", nguoiDung.Email);
            }
        }

        // Dù thông tin có tồn tại hay không, giao diện luôn trả về cùng một thông báo
        // để người lạ không dò được email / mã tài khoản nào đã đăng ký.
        ViewBag.DaGui = true;

        // Chưa cấu hình SMTP: ở môi trường phát triển, liên kết đặt lại được hiển thị ngay
        // trên trang (thay vì chỉ nằm trong tệp .eml) để luồng khôi phục chạy thử được trọn vẹn.
        ViewBag.LienKetDatLai = string.IsNullOrWhiteSpace(_emailOptions.Host) && _env.IsDevelopment()
            ? lienKetDaGui
            : null;
        return View(new ForgotPasswordViewModel());
    }

    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string? email, string? token)
    {
        ViewBag.HopLe = await TimYeuCauDatLaiAsync(email, token) is not null;
        return View(new ResetPasswordViewModel { Email = email?.Trim() ?? "", Token = token ?? "" });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        var yeuCau = await TimYeuCauDatLaiAsync(vm.Email, vm.Token);
        if (yeuCau is null)
        {
            // Token sai, hết hạn hoặc đã dùng: không cho nhập mật khẩu mới.
            ViewBag.HopLe = false;
            return View(vm);
        }

        ViewBag.HopLe = true;
        if (!ModelState.IsValid) return View(vm);

        var nguoiDung = yeuCau.NguoiDung;
        nguoiDung.MatKhau = MatKhauHasher.Bam(vm.MatKhauMoi);

        // Xóa toàn bộ token của tài khoản để link vừa dùng (và mọi link cũ) không thể phát lại.
        _db.MatKhauDatLais.RemoveRange(
            await _db.MatKhauDatLais.Where(x => x.MaNguoiDung == nguoiDung.MaNguoiDung).ToListAsync());
        await _db.SaveChangesAsync();

        await _audit.WriteAsync(nguoiDung.MaNguoiDung, "ACTION_RESET", "NguoiDung",
            null, "Đặt lại mật khẩu qua email", Ip());
        TempData["Ok"] = "Đặt lại mật khẩu thành công. Hãy đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }

    private sealed record YeuCauDatLaiHopLe(DatLaiMatKhau YeuCau, NguoiDung NguoiDung);

    /// <summary>Token trên URL được băm lại rồi so khớp bản ghi còn hạn và đúng email đi kèm.</summary>
    private async Task<YeuCauDatLaiHopLe?> TimYeuCauDatLaiAsync(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token)) return null;

        var hash = TokenDatLaiMatKhau.Bam(token.Trim());
        var now = DateTime.UtcNow;
        var yeuCau = await _db.MatKhauDatLais
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.HetHan > now);
        if (yeuCau is null) return null;

        var nguoiDung = await _db.NguoiDungs.FindAsync(yeuCau.MaNguoiDung);
        return nguoiDung is not null &&
               string.Equals(nguoiDung.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)
            ? new YeuCauDatLaiHopLe(yeuCau, nguoiDung)
            : null;
    }

    private string TaoLienKetDatLai(string email, string token)
    {
        var duongDan = Url.Action(nameof(ResetPassword), "Account", new { email, token }) ?? "/Account/Login";
        var goc = string.IsNullOrWhiteSpace(_emailOptions.PublicBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : _emailOptions.PublicBaseUrl.TrimEnd('/');
        return goc + duongDan;
    }

    private static string NoiDungEmailDatLai(string hoTen, string maNguoiDung, string link)
    {
        var href = WebUtility.HtmlEncode(link);
        return $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#1f2937;line-height:1.6">
              <h2 style="color:#086a9f;margin-bottom:8px">Đặt lại mật khẩu</h2>
              <p>Xin chào <b>{WebUtility.HtmlEncode(hoTen)}</b>,</p>
              <p>Hệ thống Quản lý Phòng học TDMU nhận được yêu cầu đặt lại mật khẩu cho tài khoản
                 <b>{WebUtility.HtmlEncode(maNguoiDung)}</b>.</p>
              <p style="margin:24px 0">
                <a href="{href}" style="background:#086a9f;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none;font-weight:bold">
                  Đặt lại mật khẩu
                </a>
              </p>
              <p>Liên kết có hiệu lực trong <b>30 phút</b> và chỉ sử dụng được một lần.</p>
              <p style="color:#6b7280;font-size:13px">Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email —
                 mật khẩu hiện tại vẫn giữ nguyên.</p>
            </div>
            """;
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Đổi mật khẩu của chính người đang đăng nhập. Trả JSON để hộp thoại ở _Layout
    /// báo lỗi tại chỗ mà không rời trang đang làm việc.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var loiDauTien = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            return Json(new { ok = false, message = loiDauTien ?? "Thông tin chưa hợp lệ." });
        }

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nguoiDung = uid is null ? null : await _db.NguoiDungs.FindAsync(uid);
        if (nguoiDung is null)
            return Json(new { ok = false, message = "Không tìm thấy tài khoản đang đăng nhập." });

        if (MatKhauHasher.KiemTra(vm.MatKhauHienTai, nguoiDung.MatKhau) == KetQuaKiemTraMatKhau.Sai)
            return Json(new { ok = false, message = "Mật khẩu hiện tại không đúng." });

        if (string.Equals(vm.MatKhauMoi, vm.MatKhauHienTai, StringComparison.Ordinal))
            return Json(new { ok = false, message = "Mật khẩu mới phải khác mật khẩu hiện tại." });

        nguoiDung.MatKhau = MatKhauHasher.Bam(vm.MatKhauMoi);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(uid, "ACTION_UPDATE", "NguoiDung", null, "Đổi mật khẩu", Ip());

        TempData["Ok"] = "Đổi mật khẩu thành công. Hãy dùng mật khẩu mới cho lần đăng nhập sau.";
        return Json(new { ok = true });
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!string.IsNullOrEmpty(uid))
            await _audit.WriteAsync(uid, "ACTION_LOGOUT", "NguoiDung", null, "Đăng xuất", HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied(int? code = null)
    {
        // Middleware UseStatusCodePagesWithReExecute giữ nguyên mã trạng thái gốc (401/403)
        // khi re-execute trang này, nên ở đây chỉ truyền mã để hiển thị nội dung phù hợp.
        ViewBag.Code = code ?? StatusCodes.Status403Forbidden;
        return View();
    }
}
