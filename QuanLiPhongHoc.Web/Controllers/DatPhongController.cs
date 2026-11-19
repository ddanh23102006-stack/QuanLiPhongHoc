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
public class DatPhongController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IDatPhongService _service;
    private readonly IPhongTraCuuService _traCuu;
    private readonly ITroLyTimPhongService _troLy;
    private readonly IAuditService _audit;

    public DatPhongController(QuanLiPhongHocContext db, IDatPhongService service, IPhongTraCuuService traCuu,
        ITroLyTimPhongService troLy, IAuditService audit)
    {
        _db = db;
        _service = service;
        _traCuu = traCuu;
        _troLy = troLy;
        _audit = audit;
    }

    public async Task<IActionResult> Index(int page = 1, string? q = null, string? tt = null, int? toa = null)
    {
        const int pageSize = 15;
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var laFacility = User.IsInRole(AppRoles.Facility);
        var tuKhoa = q?.Trim();

        var nguon = _db.DatPhongs.AsNoTracking().AsQueryable();
        if (!laFacility)
            nguon = nguon.Where(x => x.NguoiYeuCau == uid);
        if (!string.IsNullOrWhiteSpace(tt))
            nguon = nguon.Where(x => x.TrangThai == tt);
        if (toa is int maToa)
            nguon = nguon.Where(x => x.PhongHoc != null && x.PhongHoc.MaToaNha == maToa);
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var soPhieu = int.TryParse(new string(tuKhoa.Where(char.IsDigit).ToArray()), out var so) ? so : 0;
            nguon = nguon.Where(x => x.MaDatPhong == soPhieu
                || (x.MucDich != null && x.MucDich.Contains(tuKhoa))
                || (x.NguoiYeuCau != null && x.NguoiYeuCau.Contains(tuKhoa))
                || (x.PhongHoc != null && x.PhongHoc.CodePhong.Contains(tuKhoa))
                || (x.NguoiYeuCauNav != null && x.NguoiYeuCauNav.HoTen.Contains(tuKhoa)));
        }

        var tongSo = await nguon.CountAsync();
        var soTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)pageSize));
        page = Math.Clamp(page, 1, soTrang);

        var ds = await nguon
            .Include(x => x.PhongHoc).ThenInclude(x => x!.ToaNha)
            .Include(x => x.NguoiYeuCauNav).ThenInclude(x => x!.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
            .Include(x => x.NguoiDuyetNav)
            .OrderBy(x => x.TrangThai == "PENDING" ? 0 : 1)
            .ThenByDescending(x => x.MaDatPhong)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (!laFacility)
        {
            // Thông báo kết quả duyệt phải phủ toàn bộ phiếu của người gửi, không phụ thuộc trang bảng.
            var daXuLy = await _db.DatPhongs
                .Include(x => x.PhongHoc)
                .Include(x => x.NguoiDuyetNav)
                .Where(x => x.NguoiYeuCau == uid && x.ThoiDiemXuLy != null)
                .OrderByDescending(x => x.ThoiDiemXuLy)
                .ToListAsync();
            var chuaXem = daXuLy.Where(x => !x.DaXemKetQua).ToList();
            // Luôn kèm mọi kết quả chưa xem bên cạnh 5 kết quả mới nhất, tránh việc
            // phiếu cũ bị đánh dấu đã xem mà người gửi không bao giờ nhìn thấy.
            ViewBag.KetQuaXuLy = daXuLy.Take(5).Concat(chuaXem)
                .DistinctBy(x => x.MaDatPhong)
                .OrderByDescending(x => x.ThoiDiemXuLy)
                .ToList();
            ViewBag.PhieuChuaXem = chuaXem.Select(x => x.MaDatPhong).ToHashSet();
            if (chuaXem.Count > 0)
            {
                chuaXem.ForEach(x => x.DaXemKetQua = true);
                await _db.SaveChangesAsync();
            }
        }

        ViewBag.Page = page;
        ViewBag.SoTrang = soTrang;
        ViewBag.TongSo = tongSo;
        ViewBag.Q = tuKhoa;
        ViewBag.TrangThai = tt;
        ViewBag.Toa = toa;
        ViewBag.SoChoDuyet = laFacility
            ? await _db.DatPhongs.CountAsync(x => x.TrangThai == "PENDING")
            : 0;
        ViewBag.DanhSachToa = laFacility
            ? await _db.ToaNhas.AsNoTracking().OrderBy(x => x.CodeToaNha).ToListAsync()
            : new List<ToaNha>();
        return View(ds);
    }

    /// <summary>
    /// Màn hình tra cứu phòng còn trống + lập phiếu mượn theo khung giờ tự do.
    /// Chỉ liệt kê phòng vẫn còn khung giờ trống trong ngày đang xem.
    /// </summary>
    public async Task<IActionResult> Create(DateOnly? ngay, string? tu, string? den, string? q, int? toa, string? loai, int page = 1)
    {
        if (User.IsInRole(AppRoles.Facility))
        {
            TempData["Error"] = "Nhân viên cơ sở vật chất xét duyệt phiếu mượn, không tự lập phiếu.";
            return RedirectToAction(nameof(Index));
        }
        return View(await BuildCreateVmAsync(TaoBoLoc(ngay, tu, den, q, toa, loai), null, page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DatPhongFormVm vm, string? q, int? toa, string? loai, int page = 1)
    {
        if (User.IsInRole(AppRoles.Facility))
        {
            TempData["Error"] = "Nhân viên cơ sở vật chất xét duyệt phiếu mượn, không tự lập phiếu.";
            return RedirectToAction(nameof(Index));
        }

        // Bộ lọc sau khi gửi dùng chính ngày/giờ người dùng đã nhập, tránh ô ẩn trùng tên
        // Tu/Den ghi đè giá trị họ vừa nhập.
        var filter = TaoBoLoc(vm.NgayDat, vm.Tu, vm.Den, q, toa, loai);
        if (!ModelState.IsValid)
            return View(await BuildCreateVmAsync(filter, vm, page));

        var gioTu = Catalogs.ParseGio(vm.Tu);
        var gioDen = Catalogs.ParseGio(vm.Den);
        if (gioTu is null || gioDen is null || gioDen <= gioTu)
        {
            ModelState.AddModelError(string.Empty,
                "Khung giờ chưa hợp lệ — cần định dạng HH:mm và giờ kết thúc phải sau giờ bắt đầu.");
            return View(await BuildCreateVmAsync(filter, vm, page));
        }

        var don = new DatPhong
        {
            NguoiYeuCau = User.FindFirstValue(ClaimTypes.NameIdentifier),
            MaPhong = vm.MaPhong,
            NgayDat = vm.NgayDat,
            GioBatDau = gioTu.Value,
            GioKetThuc = gioDen.Value,
            MucDich = vm.MucDich,
            SoLuongNguoiThamGia = vm.SoLuongNguoiThamGia,
            TrangThai = "PENDING"
        };

        var (ok, errors) = await _service.CreateAsync(don);
        if (!ok)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            return View(await BuildCreateVmAsync(filter, vm, page));
        }

        await _audit.WriteAsync(don.NguoiYeuCau, "ACTION_CREATE", "DatPhong", null,
            $"{vm.MaPhong}|{vm.NgayDat:yyyy-MM-dd}|{gioTu:HH\\:mm}-{gioDen:HH\\:mm}", Ip());
        TempData["Ok"] = "Đã gửi phiếu đăng ký mượn phòng. Nhân viên cơ sở vật chất sẽ xét duyệt.";
        return RedirectToAction(nameof(Index));
    }

    private static PhongTraCuuFilter TaoBoLoc(DateOnly? ngay, string? tu, string? den, string? q, int? toa, string? loai) => new()
    {
        Ngay = ngay ?? DateOnly.FromDateTime(DateTime.Today),
        Tu = Catalogs.ParseGio(tu),
        Den = Catalogs.ParseGio(den),
        Q = q,
        MaToaNha = toa,
        LoaiPhong = loai,
        TrangThai = Catalogs.PhongTrong
    };

    private const int SoPhongMoiTrang = 12;

    private async Task<DatPhongCreateVm> BuildCreateVmAsync(PhongTraCuuFilter filter, DatPhongFormVm? daNhap, int page = 1)
    {
        var maTaiKhoan = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nguoiDung = await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNguoiDung == maTaiKhoan);

        // Tra cứu trả toàn bộ phòng khớp bộ lọc để số đếm và việc dò phòng đã chọn luôn đúng;
        // giao diện chỉ vẽ một trang để danh sách 200+ phòng không dồn hết vào một màn hình.
        var phong = await _traCuu.TraCuuAsync(filter);
        var soTrang = Math.Max(1, (int)Math.Ceiling(phong.Count / (double)SoPhongMoiTrang));
        page = Math.Clamp(page, 1, soTrang);

        return new DatPhongCreateVm
        {
            Filter = filter,
            Phong = phong,
            PhongTrang = phong.Skip((page - 1) * SoPhongMoiTrang).Take(SoPhongMoiTrang).ToList(),
            DanhSachToa = await _db.ToaNhas.AsNoTracking().OrderBy(x => x.CodeToaNha).ToListAsync(),
            HoTen = nguoiDung?.HoTen ?? User.Identity?.Name ?? "",
            MaNguoiDung = maTaiKhoan ?? "",
            SoDienThoai = nguoiDung?.SoDienThoai,
            PhanTrang = new PhanTrangVm
            {
                Trang = page,
                SoTrang = soTrang,
                TongSo = phong.Count,
                KichThuoc = SoPhongMoiTrang,
                DonVi = "phòng",
                TaoUrl = p => Url.Action(nameof(Create), new
                {
                    ngay = filter.Ngay.ToString("yyyy-MM-dd"),
                    tu = filter.Tu?.ToString("HH:mm"),
                    den = filter.Den?.ToString("HH:mm"),
                    q = filter.Q,
                    toa = filter.MaToaNha,
                    loai = filter.LoaiPhong,
                    page = p
                }) ?? "/DatPhong/Create"
            },
            Phieu = daNhap ?? new DatPhongFormVm
            {
                NgayDat = filter.Ngay,
                Tu = filter.Tu?.ToString("HH:mm") ?? "",
                Den = filter.Den?.ToString("HH:mm") ?? "",
                SoLuongNguoiThamGia = 20
            }
        };
    }

    /// <summary>
    /// Trợ lý tìm phòng: đọc câu tiếng Việt và trả về bộ lọc + phòng trống/phương án thay thế.
    /// Chỉ đọc dữ liệu, không ghi gì nên dùng GET để nhẹ và không cần chống CSRF.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TroLy(string? cauHoi)
    {
        if (User.IsInRole(AppRoles.Facility))
            return Json(new { traLoi = "Chức năng mượn phòng dành cho sinh viên và giảng viên." });
        if (string.IsNullOrWhiteSpace(cauHoi))
            return Json(new { traLoi = "Bạn nhập câu hỏi giúp mình nhé, ví dụ: “Chiều mai 2h–4h phòng 60 chỗ có máy chiếu”." });

        var cau = cauHoi.Trim();
        if (cau.Length > 300) cau = cau[..300];
        return Json(await _troLy.HoiAsync(cau, DateOnly.FromDateTime(DateTime.Today)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Approve(int id, int? page, string? q, string? tt, int? toa)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, errors) = await _service.ApproveAsync(id, uid);
        TempData[ok ? "Ok" : "Error"] = ok ? "Đã duyệt đơn đặt phòng." : string.Join(" ", errors);
        await _audit.WriteAsync(uid, "ACTION_APPROVE", "DatPhong", id.ToString(), ok ? "APPROVED" : "FAIL", Ip());
        return RedirectToAction(nameof(Index), new { page, q, tt, toa });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Reject(int id, int? page, string? q, string? tt, int? toa)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _service.RejectAsync(id, uid);
        await _audit.WriteAsync(uid, "ACTION_REJECT", "DatPhong", id.ToString(), "REJECTED", Ip());
        return RedirectToAction(nameof(Index), new { page, q, tt, toa });
    }

    /// <summary>Người mượn tự rút đơn khi không còn nhu cầu — chỉ áp dụng cho phiếu chờ duyệt của chính mình.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, int? page)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, errors) = await _service.CancelAsync(id, uid);
        if (ok)
        {
            await _audit.WriteAsync(uid, "ACTION_CANCEL", "DatPhong", id.ToString(), "CANCELLED", Ip());
            TempData["Ok"] = "Đã hủy phiếu, phòng được giải phóng cho người khác.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", errors);
        }
        return RedirectToAction(nameof(Index), new { page });
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
