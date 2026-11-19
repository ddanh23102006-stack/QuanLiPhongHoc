using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;
using System.Globalization;
using System.Security.Claims;

namespace QuanLiPhongHoc.Web.Controllers;

/// <summary>Lớp học hoặc phiếu mượn đã duyệt đang giữ một phòng trong ngày.</summary>
internal sealed record TinhNangPhong(int? MaPhong, string KhungGio, string Lop, int? SiSo);

[Authorize]
public class PhongHocController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IAuditService _audit;
    private readonly IDashboardService _dashboard;

    public PhongHocController(QuanLiPhongHocContext db, IAuditService audit, IDashboardService dashboard)
    {
        _db = db;
        _audit = audit;
        _dashboard = dashboard;
    }

    /// <summary>
    /// Màn hình quản lý phòng học. Trạng thái "Đang có lớp" suy ra từ LichHoc/DatPhong trong ngày
    /// chứ không lưu ở cột nào — nhờ vậy CSVC vừa đổi trang_thai_phong là trang mượn phòng thấy ngay.
    /// </summary>
    private const int SoDongMoiTrang = 20;

    public async Task<IActionResult> Index(string? q, string? toa, string? loai, string? tt, int page = 1, string? them = null)
    {
        var ngay = DateOnly.FromDateTime(DateTime.Today);
        var maToa = int.TryParse(toa, out var mt) ? mt : (int?)null;
        var moThem = them is "1" || string.Equals(them, "true", StringComparison.OrdinalIgnoreCase);

        var query = _db.PhongHocs
            .Include(x => x.ToaNha)
            .Include(x => x.KhungTrongs)
            .Include(x => x.PhongThietBis).ThenInclude(x => x.ThietBi)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.CodePhong.Contains(q));
        if (maToa is int mt2)
            query = query.Where(x => x.MaToaNha == mt2);
        if (!string.IsNullOrWhiteSpace(loai))
            query = query.Where(x => x.LoaiPhong == loai);

        var phongs = await query.OrderBy(x => x.CodePhong).ToListAsync();
        var maPhongs = phongs.Select(x => x.MaPhong).ToList();

        var lopHomNay = await _db.LichHocs.AsNoTracking()
            .Include(x => x.HocPhan)
            .Where(x => x.NgayHoc == ngay && x.TrangThaiHoatDong && x.MaPhong != null && maPhongs.Contains(x.MaPhong.Value))
            .Select(x => new { x.MaPhong, x.CaHoc, TenHocPhan = x.HocPhan!.TenHocPhan, x.HocPhan.SoLuongDangKy })
            .ToListAsync();

        var phieuHomNay = await _db.DatPhongs.AsNoTracking()
            .Where(x => x.NgayDat == ngay && x.TrangThai == "APPROVED" && x.MaPhong != null && maPhongs.Contains(x.MaPhong.Value))
            .Select(x => new { x.MaPhong, x.GioBatDau, x.GioKetThuc })
            .ToListAsync();

        var suCoMo = await _db.SuCos.AsNoTracking()
            .Where(x => x.MaPhong != null && maPhongs.Contains(x.MaPhong.Value) && x.TrangThai != "RESOLVED")
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new { x.MaPhong, x.MoTa, x.MucDoNghiemTrong })
            .ToListAsync();

        var tinhNangHomNay = lopHomNay
            .Select(x => new TinhNangPhong(x.MaPhong, $"{Catalogs.LabelCa(x.CaHoc)} • {Catalogs.CaTimeRange(x.CaHoc)}", x.TenHocPhan, x.SoLuongDangKy))
            .Concat(phieuHomNay.Select(x => new TinhNangPhong(x.MaPhong,
                Catalogs.MoTaKhoang(x.GioBatDau, x.GioKetThuc), "Phiếu mượn đã duyệt", null)))
            .ToList();

        var rows = new List<PhongHocRowVm>();
        foreach (var p in phongs)
        {
            var giuCho = tinhNangHomNay.Where(x => x.MaPhong == p.MaPhong).ToList();
            var moiNhat = giuCho.OrderByDescending(x => x.KhungGio).FirstOrDefault();
            var suCo = suCoMo.FirstOrDefault(x => x.MaPhong == p.MaPhong);
            var offline = p.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE";

            rows.Add(new PhongHocRowVm
            {
                MaPhong = p.MaPhong,
                CodePhong = p.CodePhong,
                LoaiPhong = p.LoaiPhong,
                SucChua = p.SucChua,
                TrangThaiPhong = p.TrangThaiPhong,
                MaToaNha = p.MaToaNha,
                HienThiDangKyMuon = p.HienThiDangKyMuon,
                KhungTrong = p.KhungTrongs.OrderBy(x => x.GioBatDau)
                    .Select(x => $"{x.GioBatDau:HH\\:mm}-{x.GioKetThuc:HH\\:mm}")
                    .ToList(),
                Toa = p.ToaNha is null ? "Chưa gán tòa nhà" : $"{p.ToaNha.CodeToaNha} — {p.ToaNha.TenToaNha}",
                ThietBi = p.PhongThietBis.Where(x => x.ThietBi != null).Select(x => x.ThietBi!.TenThietBi).Distinct().ToList(),
                CoLopHomNay = giuCho.Count > 0,
                GhiChuGanNhat = offline ? suCo?.MoTa ?? Catalogs.LabelTrangThaiPhong(p.TrangThaiPhong) : moiNhat?.Lop,
                GhiChiTietGanNhat = offline
                    ? (suCo is null ? null : $"Sự cố mức {Catalogs.Label(Catalogs.MucDoSuCo, suCo.MucDoNghiemTrong)}")
                    : moiNhat is null ? null : $"{moiNhat.KhungGio}{(moiNhat.SiSo is int s ? $" • {s} SV" : "")}",
                CoSuCoMo = suCo != null
            });
        }

        if (!string.IsNullOrWhiteSpace(tt))
            rows = tt switch
            {
                "AVAILABLE" => rows.Where(x => x.TrangThaiPhong == "AVAILABLE" && !x.CoLopHomNay).ToList(),
                "IN_USE" => rows.Where(x => x.CoLopHomNay && x.TrangThaiPhong == "AVAILABLE").ToList(),
                "MAINTENANCE" => rows.Where(x => x.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE").ToList(),
                _ => rows
            };

        // KPI tính trên toàn bộ phòng khớp bộ lọc; bảng chỉ lấy một trang.
        var tongSo = rows.Count;
        var baoTri = rows.Count(x => x.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE");
        var dangCoLop = rows.Count(x => x.CoLopHomNay && x.TrangThaiPhong == "AVAILABLE");
        var soTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        page = Math.Clamp(page, 1, soTrang);

        ViewBag.MoThemPhong = moThem;
        ViewBag.ToaChonSan = maToa;
        return View(new PhongHocQuanLyVm
        {
            Rows = rows.Skip((page - 1) * SoDongMoiTrang).Take(SoDongMoiTrang).ToList(),
            DanhSachToa = await _db.ToaNhas.AsNoTracking().OrderBy(x => x.CodeToaNha).ToListAsync(),
            Q = q,
            MaToaNha = maToa,
            LoaiPhong = loai,
            TrangThai = tt,
            Ngay = ngay,
            TongSo = tongSo,
            BaoTri = baoTri,
            DangCoLop = dangCoLop,
            SanSang = tongSo - baoTri - dangCoLop,
            PhanTrang = new PhanTrangVm
            {
                Trang = page,
                SoTrang = soTrang,
                TongSo = tongSo,
                KichThuoc = SoDongMoiTrang,
                DonVi = "phòng",
                TaoUrl = p => Url.Action(nameof(Index), new { q, toa, loai, tt, page = p }) ?? "/PhongHoc"
            }
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var phong = await _db.PhongHocs.Include(x => x.ToaNha)
            .Include(x => x.PhongThietBis).ThenInclude(x => x.ThietBi)
            .FirstOrDefaultAsync(x => x.MaPhong == id);
        if (phong == null) return NotFound();
        return View(phong);
    }

    /// <summary>Xuất Excel danh sách thiết bị của phòng để CSVC gửi kèm báo cáo.</summary>
    [HttpGet]
    public async Task<IActionResult> XuatBaoCaoThietBi(int id)
    {
        var phong = await _db.PhongHocs.AsNoTracking()
            .Include(x => x.ToaNha)
            .Include(x => x.PhongThietBis).ThenInclude(x => x.ThietBi)
            .FirstOrDefaultAsync(x => x.MaPhong == id);
        if (phong is null) return NotFound();

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("ThietBiPhong");
        ws.Cell(1, 1).Value = $"BÁO CÁO THIẾT BỊ PHÒNG {phong.CodePhong}";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        ws.Cell(2, 1).Value = $"Tòa nhà: {phong.ToaNha?.CodeToaNha ?? "—"} · Loại phòng: {Catalogs.LabelLoaiPhong(phong.LoaiPhong)} · Trạng thái: {Catalogs.LabelTrangThaiPhong(phong.TrangThaiPhong)}";
        ws.Range(2, 1, 2, 6).Merge();
        ws.Cell(3, 1).Value = $"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm} · Tổng {phong.PhongThietBis.Count} thiết bị";
        ws.Range(3, 1, 3, 6).Merge();

        var headers = new[] { "STT", "Nhãn nhận diện", "Tên thiết bị", "Mã loại", "Trạng thái", "Ghi chú" };
        for (var i = 0; i < headers.Length; i++) ws.Cell(5, i + 1).Value = headers[i];
        ws.Range(5, 1, 5, headers.Length).Style.Font.SetBold().Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#E8EDF2"));

        var row = 6;
        foreach (var tb in phong.PhongThietBis.OrderBy(x => x.NhanDienThietBi))
        {
            ws.Cell(row, 1).Value = row - 5;
            ws.Cell(row, 2).Value = tb.NhanDienThietBi ?? "";
            ws.Cell(row, 3).Value = tb.ThietBi?.TenThietBi ?? "";
            ws.Cell(row, 4).Value = tb.ThietBi?.MaCodeThietBi ?? "";
            ws.Cell(row, 5).Value = Catalogs.Label(Catalogs.TrangThaiThietBi, tb.TrangThai);
            ws.Cell(row, 6).Value = tb.TrangThai == "GOOD" ? "" : "Cần theo dõi / xử lý";
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoThietBi_{phong.CodePhong}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Create(int? toa)
    {
        await FillLookups();
        return View(new PhongHocFormVm { KhungTrong = KhaiKhung(null), MaToaNha = toa });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Create(PhongHocFormVm vm)
    {
        var khungTrong = vm.HienThiDangKyMuon ? NhapKhung(vm) : [];
        if (await _db.PhongHocs.AnyAsync(x => x.CodePhong == vm.CodePhong))
            ModelState.AddModelError(nameof(vm.CodePhong), "Mã phòng đã tồn tại.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }
        var entity = new PhongHoc
        {
            CodePhong = vm.CodePhong,
            MaToaNha = vm.MaToaNha,
            LoaiPhong = vm.LoaiPhong,
            SucChua = vm.SucChua,
            TrangThaiPhong = vm.TrangThaiPhong,
            HienThiDangKyMuon = vm.HienThiDangKyMuon,
            KhungTrongs = khungTrong
        };
        _db.PhongHocs.Add(entity);
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        await _audit.WriteAsync(UserId(), "ACTION_CREATE", "PhongHoc", null, vm.CodePhong, Ip());
        TempData["Ok"] = vm.HienThiDangKyMuon
            ? $"Đã thêm phòng {vm.CodePhong}. Phòng xuất hiện ngay trên trang đăng ký mượn phòng."
            : $"Đã thêm phòng {vm.CodePhong}. Phòng đang ẩn khỏi trang đăng ký mượn phòng.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.PhongHocs.Include(x => x.KhungTrongs).FirstOrDefaultAsync(x => x.MaPhong == id);
        if (p == null) return NotFound();
        await FillLookups();
        return View(new PhongHocFormVm
        {
            MaPhong = p.MaPhong,
            CodePhong = p.CodePhong,
            MaToaNha = p.MaToaNha,
            LoaiPhong = p.LoaiPhong,
            SucChua = p.SucChua,
            TrangThaiPhong = p.TrangThaiPhong,
            HienThiDangKyMuon = p.HienThiDangKyMuon,
            KhungTrong = KhaiKhung(p.KhungTrongs)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Edit(PhongHocFormVm vm, int? page)
    {
        var p = await _db.PhongHocs.FindAsync(vm.MaPhong);
        if (p == null) return NotFound();
        if (await _db.PhongHocs.AnyAsync(x => x.CodePhong == vm.CodePhong && x.MaPhong != vm.MaPhong))
        {
            TempData["Error"] = $"Mã phòng {vm.CodePhong} đã được phòng khác sử dụng.";
            return RedirectToAction(nameof(Index), new { page });
        }
        // Ẩn phòng khỏi trang mượn không có nghĩa là phòng ngừng mở cửa:
        // giữ nguyên khung giờ đã khai để bộ xếp lịch vẫn dùng được.
        var khungTrong = vm.HienThiDangKyMuon ? NhapKhung(vm) : null;
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index), new { page });
        }
        var old = $"{p.CodePhong}|{p.TrangThaiPhong}|{p.SucChua}";
        var oldTrangThai = p.TrangThaiPhong;
        p.CodePhong = vm.CodePhong;
        p.MaToaNha = vm.MaToaNha;
        p.LoaiPhong = vm.LoaiPhong;
        p.SucChua = vm.SucChua;
        p.TrangThaiPhong = vm.TrangThaiPhong;
        p.HienThiDangKyMuon = vm.HienThiDangKyMuon;

        if (khungTrong is not null)
        {
            var cu = await _db.PhongKhungTrongs.Where(x => x.MaPhong == p.MaPhong).ToListAsync();
            _db.PhongKhungTrongs.RemoveRange(cu);
            foreach (var x in khungTrong)
            {
                x.MaPhong = p.MaPhong;
                _db.PhongKhungTrongs.Add(x);
            }
        }

        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        await _audit.WriteAsync(UserId(), "ACTION_UPDATE", "PhongHoc", old,
            $"{p.CodePhong}|{p.TrangThaiPhong}|{p.HienThiDangKyMuon}", Ip());
        TempData["Ok"] = $"Đã cập nhật {p.CodePhong}." +
            (oldTrangThai != vm.TrangThaiPhong ? $" Trạng thái: {Catalogs.LabelTrangThaiPhong(vm.TrangThaiPhong)} — trang mượn phòng đã thay đổi." : "") +
            (vm.HienThiDangKyMuon ? " Phòng đang hiển thị trên trang đăng ký mượn." : " Phòng đang ẩn khỏi trang đăng ký mượn.");
        return RedirectToAction(nameof(Index), new { page });
    }

    /// <summary>Chuyển nhanh AVAILABLE → MAINTENANCE → UNAVAILABLE → AVAILABLE; trang mượn phòng đọc lại ngay.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> DoiTrangThai(int id, int? page)
    {
        var p = await _db.PhongHocs.FindAsync(id);
        if (p == null) return NotFound();
        var old = p.TrangThaiPhong;
        p.TrangThaiPhong = old switch
        {
            "AVAILABLE" => "MAINTENANCE",
            "MAINTENANCE" => "UNAVAILABLE",
            _ => "AVAILABLE"
        };
        await _db.SaveChangesAsync();
        _dashboard.Invalidate();
        await _audit.WriteAsync(UserId(), "ACTION_UPDATE", "PhongHoc", $"{p.CodePhong}|{old}", $"{p.CodePhong}|{p.TrangThaiPhong}", Ip());
        TempData["Ok"] = $"{p.CodePhong}: {Catalogs.LabelTrangThaiPhong(old)} → {Catalogs.LabelTrangThaiPhong(p.TrangThaiPhong)}.";
        return RedirectToAction(nameof(Index), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> Delete(int id, int? page)
    {
        var p = await _db.PhongHocs.FindAsync(id);
        if (p != null)
        {
            var conDuLieu = await _db.LichHocs.AnyAsync(x => x.MaPhong == id)
                           || await _db.DatPhongs.AnyAsync(x => x.MaPhong == id);
            if (conDuLieu)
            {
                TempData["Error"] = $"{p.CodePhong} còn lịch học hoặc phiếu mượn gắn với phòng — hãy chuyển sang trạng thái Bảo trì thay vì xóa.";
                return RedirectToAction(nameof(Index), new { page });
            }
            _db.PhongThietBis.RemoveRange(await _db.PhongThietBis.Where(x => x.MaPhong == id).ToListAsync());
            _db.PhongHocs.Remove(p);
            await _db.SaveChangesAsync();
            _dashboard.Invalidate();
            await _audit.WriteAsync(UserId(), "ACTION_DELETE", "PhongHoc", p.CodePhong, null, Ip());
            TempData["Ok"] = $"Đã xóa phòng {p.CodePhong} khỏi danh mục.";
        }
        return RedirectToAction(nameof(Index), new { page = Math.Max(1, page ?? 1) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Facility)]
    public async Task<IActionResult> CapNhatThietBi(int maPhongThietBi, string trangThai, string? returnUrl)
    {
        var tb = await _db.PhongThietBis.FindAsync(maPhongThietBi);
        if (tb != null && Catalogs.TrangThaiThietBi.ContainsKey(trangThai))
        {
            var old = tb.TrangThai;
            tb.TrangThai = trangThai;
            await _db.SaveChangesAsync();
            await _audit.WriteAsync(UserId(), "ACTION_UPDATE", "Phong_ThietBi",
                $"{tb.NhanDienThietBi}|{old}", $"{tb.NhanDienThietBi}|{trangThai}", Ip());
            TempData["Ok"] = $"{tb.NhanDienThietBi}: {Catalogs.Label(Catalogs.TrangThaiThietBi, old)} → {Catalogs.Label(Catalogs.TrangThaiThietBi, trangThai)}.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        if (tb?.MaPhong is int maPhong)
            return RedirectToAction(nameof(Details), new { id = maPhong });
        if (tb?.MaThietBi is int maThietBi)
            return RedirectToAction("Details", "ThietBi", new { id = maThietBi });
        return RedirectToAction(nameof(Index));
    }

    private async Task FillLookups()
    {
        ViewBag.ToaNhas = new SelectList(await _db.ToaNhas.ToListAsync(), "MaToaNha", "TenToaNha");
    }

    /// <summary>
    /// Dòng nhập khung giờ trống: phòng đã khai thì hiện đúng khai báo, phòng chưa khai
    /// thì prefill ba khung mặc định (sáng/chiều/tối) để CSVC chỉ cần sửa chỗ lệch.
    /// </summary>
    private static List<KhaiKhungVm> KhaiKhung(IEnumerable<PhongKhungTrong>? daKhai)
    {
        var daLuu = daKhai?.OrderBy(x => x.GioBatDau).ToList() ?? [];
        if (daLuu.Count == 0)
            return Catalogs.KhungTrongMacDinh
                .Select(k => new KhaiKhungVm { Tu = k.Start.ToString("HH:mm"), Den = k.End.ToString("HH:mm") })
                .ToList();

        return daLuu
            .Select(x => new KhaiKhungVm { Tu = x.GioBatDau.ToString("HH:mm"), Den = x.GioKetThuc.ToString("HH:mm") })
            .ToList();
    }

    /// <summary>Đọc danh sách khung giờ CSVC nhập. Dòng bỏ trống bị bỏ qua; dòng sai hoặc trùng giờ sẽ báo lỗi.</summary>
    private List<PhongKhungTrong> NhapKhung(PhongHocFormVm vm)
    {
        var ketQua = new List<PhongKhungTrong>();
        foreach (var row in vm.KhungTrong)
        {
            if (string.IsNullOrWhiteSpace(row.Tu) && string.IsNullOrWhiteSpace(row.Den)) continue;

            var tu = Catalogs.ParseGio(row.Tu);
            var den = Catalogs.ParseGio(row.Den);
            if (tu is null || den is null || den <= tu)
            {
                ModelState.AddModelError(string.Empty,
                    $"Khung giờ {(string.IsNullOrWhiteSpace(row.Tu) ? "?" : row.Tu)} – {(string.IsNullOrWhiteSpace(row.Den) ? "?" : row.Den)} chưa hợp lệ — cần định dạng HH:mm và giờ kết thúc phải sau giờ bắt đầu.");
                continue;
            }

            var trung = ketQua.FirstOrDefault(x => Catalogs.IsOverlap(x.GioBatDau, x.GioKetThuc, tu.Value, den.Value));
            if (trung is not null)
            {
                ModelState.AddModelError(string.Empty,
                    $"Khung {Catalogs.MoTaKhoang(tu.Value, den.Value)} trùng khung {Catalogs.MoTaKhoang(trung.GioBatDau, trung.GioKetThuc)} — mỗi khoảng giờ chỉ khai một lần.");
                continue;
            }

            ketQua.Add(new PhongKhungTrong { GioBatDau = tu.Value, GioKetThuc = den.Value });
        }

        if (ketQua.Count == 0)
            ModelState.AddModelError(string.Empty, "Cần khai ít nhất một khung giờ trống cho phòng.");

        return ketQua;
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[Authorize(Roles = AppRoles.Facility)]
public class ToaNhaController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IAuditService _audit;

    public ToaNhaController(QuanLiPhongHocContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, string? tt)
    {
        var tatCaToa = await _db.ToaNhas.AsNoTracking().OrderBy(x => x.CodeToaNha).ToListAsync();
        var toaNhas = tatCaToa.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            toaNhas = toaNhas.Where(x =>
                x.CodeToaNha.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (x.TenToaNha?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.DiaChi?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (!string.IsNullOrWhiteSpace(tt))
            toaNhas = toaNhas.Where(x => x.TrangThaiHoatDong == tt);
        var dsHienThi = toaNhas.ToList();

        var phongs = await _db.PhongHocs.AsNoTracking()
            .Select(x => new { x.MaPhong, x.MaToaNha, x.SucChua, x.TrangThaiPhong })
            .ToListAsync();
        var thietBis = await _db.PhongThietBis.AsNoTracking()
            .Where(x => x.MaPhong != null)
            .Select(x => new { x.MaPhong, x.TrangThai })
            .ToListAsync();
        var suCos = await _db.SuCos.AsNoTracking()
            .Where(x => x.MaPhong != null && x.TrangThai != "RESOLVED")
            .Select(x => new { x.MaPhong })
            .ToListAsync();

        var phongTheoToa = phongs.ToLookup(x => x.MaToaNha ?? 0);
        var thietBiTheoPhong = thietBis.ToLookup(x => x.MaPhong ?? 0);
        var suCoTheoPhong = suCos.ToLookup(x => x.MaPhong ?? 0);

        ToaNhaRowVm Dong(ToaNha t)
        {
            var dsPhong = phongTheoToa[t.MaToaNha].ToList();
            var dsThietBi = dsPhong.SelectMany(p => thietBiTheoPhong[p.MaPhong]).ToList();
            return new ToaNhaRowVm
            {
                ToaNha = t,
                SoPhong = dsPhong.Count,
                SucChua = dsPhong.Sum(p => p.SucChua),
                PhongBaoTri = dsPhong.Count(p => p.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE"),
                ThietBi = dsThietBi.Count,
                ThietBiCanXuLy = dsThietBi.Count(x => Catalogs.ThietBiCanXuLy(x.TrangThai)),
                SuCoMo = dsPhong.Sum(p => suCoTheoPhong[p.MaPhong].Count())
            };
        }

        // KPI tính trên toàn bộ tòa nhà; bảng bên dưới chỉ hiện phần khớp bộ lọc.
        return View(new ToaNhaQuanLyVm
        {
            Rows = dsHienThi.Select(Dong).ToList(),
            Q = q,
            TrangThai = tt,
            TongToa = tatCaToa.Count,
            DangHoatDong = tatCaToa.Count(x => x.TrangThaiHoatDong == "ACTIVE"),
            BaoTri = tatCaToa.Count(x => x.TrangThaiHoatDong == "MAINTENANCE"),
            NgungHoatDong = tatCaToa.Count(x => x.TrangThaiHoatDong == "INACTIVE"),
            TongPhong = phongs.Count
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var toa = await _db.ToaNhas.AsNoTracking().FirstOrDefaultAsync(x => x.MaToaNha == id);
        if (toa is null) return NotFound();

        var phongs = await _db.PhongHocs.AsNoTracking()
            .Where(x => x.MaToaNha == id)
            .Include(x => x.PhongThietBis).ThenInclude(x => x.ThietBi)
            .OrderBy(x => x.CodePhong)
            .ToListAsync();

        var maPhongs = phongs.Select(x => x.MaPhong).ToList();
        var suCos = await _db.SuCos.AsNoTracking()
            .Where(x => x.MaPhong != null && maPhongs.Contains(x.MaPhong.Value))
            .Include(x => x.PhongHoc)
            .Include(x => x.NguoiBaoCaoNav)
            .Include(x => x.ChiTietSuCos).ThenInclude(x => x.PhongThietBi!).ThenInclude(x => x.ThietBi)
            .OrderByDescending(x => x.NgayTao)
            .Take(100)
            .ToListAsync();

        var suCoMoTheoPhong = suCos
            .Where(x => x.TrangThai != "RESOLVED" && x.MaPhong != null)
            .ToLookup(x => x.MaPhong!.Value);

        var rows = phongs.Select(p => new ToaNhaPhongRowVm
        {
            Phong = p,
            ThietBi = p.PhongThietBis.Count,
            ThietBiCanXuLy = p.PhongThietBis.Count(x => Catalogs.ThietBiCanXuLy(x.TrangThai)),
            SuCoMo = suCoMoTheoPhong[p.MaPhong].Count()
        }).ToList();

        return View(new ToaNhaChiTietVm
        {
            ToaNha = toa,
            Phong = rows,
            SuCo = suCos,
            TongSucChua = phongs.Sum(x => x.SucChua),
            PhongBaoTri = phongs.Count(x => x.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE"),
            SoThietBi = rows.Sum(x => x.ThietBi),
            ThietBiCanXuLy = rows.Sum(x => x.ThietBiCanXuLy)
        });
    }

    public IActionResult Create() => View(new ToaNhaFormVm());

    /// <summary>Xuất Excel danh sách phòng và thiết bị của một tòa nhà.</summary>
    [HttpGet]
    public async Task<IActionResult> XuatBaoCao(int id)
    {
        var toa = await _db.ToaNhas.AsNoTracking().FirstOrDefaultAsync(x => x.MaToaNha == id);
        if (toa is null) return NotFound();

        var phongs = await _db.PhongHocs.AsNoTracking()
            .Include(x => x.PhongThietBis)
            .Where(x => x.MaToaNha == id)
            .OrderBy(x => x.CodePhong)
            .ToListAsync();

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("PhongHoc");
        ws.Cell(1, 1).Value = $"BÁO CÁO TÒA NHÀ {toa.CodeToaNha} — {toa.TenToaNha}";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        ws.Cell(2, 1).Value = $"Trạng thái: {Catalogs.LabelTrangThaiToaNha(toa.TrangThaiHoatDong)} · Địa chỉ: {toa.DiaChi ?? "—"}";
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(3, 1).Value = $"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm} · {phongs.Count} phòng";
        ws.Range(3, 1, 3, 7).Merge();

        var headers = new[] { "STT", "Mã phòng", "Loại phòng", "Sức chứa", "Trạng thái", "Số thiết bị", "Thiết bị cần xử lý" };
        for (var i = 0; i < headers.Length; i++) ws.Cell(5, i + 1).Value = headers[i];
        ws.Range(5, 1, 5, headers.Length).Style.Font.SetBold().Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#E8EDF2"));

        var row = 6;
        foreach (var p in phongs)
        {
            ws.Cell(row, 1).Value = row - 5;
            ws.Cell(row, 2).Value = p.CodePhong;
            ws.Cell(row, 3).Value = Catalogs.LabelLoaiPhong(p.LoaiPhong);
            ws.Cell(row, 4).Value = p.SucChua;
            ws.Cell(row, 5).Value = Catalogs.LabelTrangThaiPhong(p.TrangThaiPhong);
            ws.Cell(row, 6).Value = p.PhongThietBis.Count;
            ws.Cell(row, 7).Value = p.PhongThietBis.Count(x => Catalogs.ThietBiCanXuLy(x.TrangThai));
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoToaNha_{toa.CodeToaNha}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ToaNhaFormVm vm)
    {
        ChuanHoa(vm);
        await KiemTraMaAsync(vm);
        if (!ModelState.IsValid) return View(vm);

        var toa = new ToaNha
        {
            CodeToaNha = vm.CodeToaNha,
            TenToaNha = vm.TenToaNha,
            DiaChi = vm.DiaChi,
            SoTang = vm.SoTang,
            TrangThaiHoatDong = vm.TrangThaiHoatDong
        };
        _db.ToaNhas.Add(toa);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(UserId(), "ACTION_CREATE", "ToaNha", null,
            $"{toa.CodeToaNha}|{toa.TenToaNha}|{toa.TrangThaiHoatDong}", Ip());
        TempData["Ok"] = $"Đã thêm tòa nhà {toa.CodeToaNha}. Mở trang chi tiết để thêm phòng và theo dõi thiết bị.";
        return RedirectToAction(nameof(Details), new { id = toa.MaToaNha });
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var toa = await _db.ToaNhas.AsNoTracking().FirstOrDefaultAsync(x => x.MaToaNha == id);
        if (toa is null) return NotFound();
        ViewBag.ReturnUrl = ChonQuayLai(returnUrl, id);
        return View(new ToaNhaFormVm
        {
            MaToaNha = toa.MaToaNha,
            CodeToaNha = toa.CodeToaNha,
            TenToaNha = toa.TenToaNha ?? string.Empty,
            DiaChi = toa.DiaChi,
            SoTang = toa.SoTang,
            TrangThaiHoatDong = toa.TrangThaiHoatDong
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ToaNhaFormVm vm, string? returnUrl = null)
    {
        var toa = await _db.ToaNhas.FirstOrDefaultAsync(x => x.MaToaNha == vm.MaToaNha);
        if (toa is null) return NotFound();

        ChuanHoa(vm);
        await KiemTraMaAsync(vm);
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = ChonQuayLai(returnUrl, vm.MaToaNha);
            return View(vm);
        }

        var old = $"{toa.CodeToaNha}|{toa.TenToaNha}|{toa.DiaChi}|{toa.SoTang}|{toa.TrangThaiHoatDong}";
        toa.CodeToaNha = vm.CodeToaNha;
        toa.TenToaNha = vm.TenToaNha;
        toa.DiaChi = vm.DiaChi;
        toa.SoTang = vm.SoTang;
        toa.TrangThaiHoatDong = vm.TrangThaiHoatDong;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(UserId(), "ACTION_UPDATE", "ToaNha", old,
            $"{toa.CodeToaNha}|{toa.TenToaNha}|{toa.DiaChi}|{toa.SoTang}|{toa.TrangThaiHoatDong}", Ip());
        TempData["Ok"] = $"Đã cập nhật tòa nhà {toa.CodeToaNha}.";
        return RedirectToAction(nameof(Details), new { id = toa.MaToaNha });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, string trangThai, string? returnUrl)
    {
        var toa = await _db.ToaNhas.FindAsync(id);
        if (toa is null) return NotFound();
        if (!Catalogs.TrangThaiToaNha.ContainsKey(trangThai))
        {
            TempData["Error"] = "Trạng thái hoạt động không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var old = toa.TrangThaiHoatDong;
        toa.TrangThaiHoatDong = trangThai;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(UserId(), "ACTION_UPDATE", "ToaNha", $"{toa.CodeToaNha}|{old}",
            $"{toa.CodeToaNha}|{trangThai}", Ip());
        TempData["Ok"] = $"{toa.CodeToaNha}: {Catalogs.LabelTrangThaiToaNha(old)} → {Catalogs.LabelTrangThaiToaNha(trangThai)}." +
            (trangThai == "ACTIVE" ? "" : " Toàn bộ phòng của tòa đang bị coi là bảo trì trên trang mượn phòng.");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var toa = await _db.ToaNhas.FindAsync(id);
        if (toa is null) return NotFound();

        var soPhong = await _db.PhongHocs.CountAsync(x => x.MaToaNha == id);
        if (soPhong > 0)
        {
            TempData["Error"] = $"Tòa {toa.CodeToaNha} còn {soPhong} phòng học — hãy xóa hoặc chuyển các phòng sang tòa khác trước khi xóa.";
            return RedirectToAction(nameof(Index));
        }

        var old = $"{toa.CodeToaNha}|{toa.TenToaNha}|{toa.TrangThaiHoatDong}";
        _db.ToaNhas.Remove(toa);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(UserId(), "ACTION_DELETE", "ToaNha", old, null, Ip());
        TempData["Ok"] = $"Đã xóa tòa nhà {toa.CodeToaNha} khỏi danh mục.";
        return RedirectToAction(nameof(Index));
    }

    private void ChuanHoa(ToaNhaFormVm vm)
    {
        vm.CodeToaNha = vm.CodeToaNha?.Trim().ToUpperInvariant() ?? string.Empty;
        vm.TenToaNha = vm.TenToaNha?.Trim() ?? string.Empty;
        vm.DiaChi = string.IsNullOrWhiteSpace(vm.DiaChi) ? null : vm.DiaChi.Trim();
        if (!Catalogs.TrangThaiToaNha.ContainsKey(vm.TrangThaiHoatDong))
            ModelState.AddModelError(nameof(vm.TrangThaiHoatDong), "Chọn trạng thái hoạt động hợp lệ.");
    }

    private async Task KiemTraMaAsync(ToaNhaFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.CodeToaNha)) return;
        if (await _db.ToaNhas.AnyAsync(x => x.CodeToaNha == vm.CodeToaNha && x.MaToaNha != vm.MaToaNha))
            ModelState.AddModelError(nameof(vm.CodeToaNha), $"Mã tòa nhà {vm.CodeToaNha} đã được sử dụng.");
    }

    private string ChonQuayLai(string? returnUrl, int id)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(Details), new { id }) ?? $"/ToaNha/Details/{id}";

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
