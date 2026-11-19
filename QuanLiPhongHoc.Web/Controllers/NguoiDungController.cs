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

[Authorize(Roles = AppRoles.Admin)]
public class NguoiDungController : Controller
{
    private const int PageSize = 20;

    private readonly QuanLiPhongHocContext _db;
    private readonly IAuditService _audit;

    public NguoiDungController(QuanLiPhongHocContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.NguoiDungs.Include(x => x.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
            .OrderBy(x => x.MaNguoiDung);

        var total = await query.CountAsync();
        var soTrang = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, soTrang);

        ViewBag.Page = page;
        ViewBag.SoTrang = soTrang;
        ViewBag.TongSo = total;
        return View(await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.VaiTros = new SelectList(await _db.VaiTros.ToListAsync(), "MaVaiTro", "TenVaiTro");
        return View(new NguoiDungFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NguoiDungFormVm vm)
    {
        var vaiTros = await _db.VaiTros.ToListAsync();
        var chon = vaiTros.Where(x => vm.MaVaiTroIds.Contains(x.MaVaiTro)).Select(x => x.MaVaiTro).Distinct().ToList();
        if (chon.Count == 0)
            ModelState.AddModelError(nameof(vm.MaVaiTroIds), "Chọn ít nhất một vai trò.");
        if (await _db.NguoiDungs.AnyAsync(x => x.MaNguoiDung == vm.MaNguoiDung || x.Email == vm.Email))
            ModelState.AddModelError(string.Empty, "Mã hoặc email đã tồn tại.");
        if (!ModelState.IsValid)
        {
            ViewBag.VaiTros = new SelectList(vaiTros, "MaVaiTro", "TenVaiTro");
            return View(vm);
        }
        var u = new NguoiDung
        {
            MaNguoiDung = vm.MaNguoiDung,
            HoTen = vm.HoTen,
            Email = vm.Email,
            MatKhau = MatKhauHasher.Bam(string.IsNullOrWhiteSpace(vm.MatKhau) ? "123456" : vm.MatKhau),
            BiKhoa = vm.BiKhoa,
            NgayTao = DateTime.Now
        };
        _db.NguoiDungs.Add(u);
        foreach (var ma in chon)
            _db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = u.MaNguoiDung, MaVaiTro = ma });
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_CREATE", "NguoiDung", null, u.MaNguoiDung, HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var u = await _db.NguoiDungs.Include(x => x.NguoiDungVaiTros).FirstOrDefaultAsync(x => x.MaNguoiDung == id);
        if (u == null) return NotFound();
        ViewBag.VaiTros = new SelectList(await _db.VaiTros.ToListAsync(), "MaVaiTro", "TenVaiTro");
        return View(new NguoiDungFormVm
        {
            MaNguoiDung = u.MaNguoiDung,
            HoTen = u.HoTen,
            Email = u.Email,
            BiKhoa = u.BiKhoa,
            MaVaiTroIds = u.NguoiDungVaiTros.Select(x => x.MaVaiTro).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NguoiDungFormVm vm)
    {
        var u = await _db.NguoiDungs.Include(x => x.NguoiDungVaiTros).FirstOrDefaultAsync(x => x.MaNguoiDung == vm.MaNguoiDung);
        if (u == null) return NotFound();
        var vaiTros = await _db.VaiTros.ToListAsync();
        var chon = vaiTros.Where(x => vm.MaVaiTroIds.Contains(x.MaVaiTro)).Select(x => x.MaVaiTro).Distinct().ToList();
        if (chon.Count == 0)
            ModelState.AddModelError(nameof(vm.MaVaiTroIds), "Chọn ít nhất một vai trò.");
        if (!ModelState.IsValid)
        {
            ViewBag.VaiTros = new SelectList(vaiTros, "MaVaiTro", "TenVaiTro");
            return View(vm);
        }
        var tenCu = vaiTros.Where(x => u.NguoiDungVaiTros.Any(v => v.MaVaiTro == x.MaVaiTro))
            .Select(x => x.TenVaiTro ?? x.CodeVaiTro);
        var tenMoi = vaiTros.Where(x => chon.Contains(x.MaVaiTro)).Select(x => x.TenVaiTro ?? x.CodeVaiTro);
        var old = $"Khoa:{u.BiKhoa};VaiTro:{string.Join(", ", tenCu)}";
        u.HoTen = vm.HoTen;
        u.Email = vm.Email;
        u.BiKhoa = vm.BiKhoa;
        if (!string.IsNullOrWhiteSpace(vm.MatKhau)) u.MatKhau = MatKhauHasher.Bam(vm.MatKhau);
        _db.NguoiDungVaiTros.RemoveRange(u.NguoiDungVaiTros);
        foreach (var ma in chon)
            _db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = u.MaNguoiDung, MaVaiTro = ma });
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "NguoiDung", old, $"Khoa:{u.BiKhoa};VaiTro:{string.Join(", ", tenMoi)}", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Ok"] = "Đã cập nhật tài khoản. Phiên hiện tại của người dùng sẽ áp dụng quyền mới khi đăng nhập lại.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = AppRoles.Admin)]
public class NhatKyController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    public NhatKyController(QuanLiPhongHocContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.NhatKyHeThongs.Include(x => x.NguoiThucHien).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var hanhDong = NhatKyCatalog.HanhDong
                .Where(x => x.Value.Contains(q, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList();
            var doiTuong = NhatKyCatalog.DoiTuong
                .Where(x => x.Value.Contains(q, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList();
            query = query.Where(x => x.LoaiHanhDong.Contains(q)
                                     || hanhDong.Contains(x.LoaiHanhDong)
                                     || (x.DoiTuongTacDong != null && (x.DoiTuongTacDong.Contains(q) || doiTuong.Contains(x.DoiTuongTacDong))));
        }

        ViewBag.TenVaiTro = await _db.VaiTros.AsNoTracking()
            .ToDictionaryAsync(x => x.MaVaiTro, x => string.IsNullOrWhiteSpace(x.TenVaiTro) ? x.CodeVaiTro : x.TenVaiTro!);
        return View(await query.OrderByDescending(x => x.ThoiGian).Take(300).ToListAsync());
    }
}

[Authorize(Roles = AppRoles.Training)]
public class ThongKeController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly IThongKeService _thongKe;
    private readonly QuanLiPhongHocContext _db;

    public ThongKeController(IDashboardService dashboard, IThongKeService thongKe, QuanLiPhongHocContext db)
    {
        _dashboard = dashboard;
        _thongKe = thongKe;
        _db = db;
    }

    private async Task<(DateOnly From, DateOnly To)> ResolveRangeAsync(DateOnly? from, DateOnly? to)
    {
        if (from is not null && to is not null) return (from.Value, to.Value);
        var sessions = _db.LichHocs.Where(x => x.TrangThaiHoatDong);
        var first = await sessions.OrderBy(x => x.NgayHoc).Select(x => (DateOnly?)x.NgayHoc).FirstOrDefaultAsync();
        var last = await sessions.OrderByDescending(x => x.NgayHoc).Select(x => (DateOnly?)x.NgayHoc).FirstOrDefaultAsync();
        var fallback = DateOnly.FromDateTime(DateTime.Today);
        return (from ?? first ?? fallback.AddDays(-30), to ?? last ?? fallback);
    }

    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var range = await ResolveRangeAsync(from, to);
        ViewBag.Kpi = await _dashboard.GetKpisAsync(true);
        ViewBag.From = range.From;
        ViewBag.To = range.To;
        return View(await _thongKe.GetAsync(range.From, range.To));
    }

    /// <summary>Xuất báo cáo tổng hợp ra file Excel (.xlsx).</summary>
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to)
    {
        var range = await ResolveRangeAsync(from, to);
        var d = await _thongKe.GetAsync(range.From, range.To);
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("TongHop");
        ws.Cell(1, 1).Value = "BÁO CÁO HIỆU SUẤT ĐÀO TẠO & GIẢNG ĐƯỜNG";
        ws.Range(1, 1, 1, 2).Merge().Style.Font.SetBold().Font.FontSize = 14;
        ws.Cell(3, 1).Value = "Chỉ số";
        ws.Cell(3, 2).Value = "Giá trị";
        ws.Range(3, 1, 3, 2).Style.Font.SetBold();
        var rows = new (string Label, object Value)[]
        {
            ("Tổng số phòng", d.TongPhong),
            ("Phòng sẵn sàng", d.PhongSanSang),
            ("Phòng đang sử dụng", d.SoPhongCoLich),
            ("Tỷ lệ sử dụng phòng (%)", d.TyLeSuDungPhong),
            ("Tổng số ca trong kỳ", d.TongLich),
            ("Tổng tiết giảng thực tế", d.TongTiet),
            ("Tiết lý thuyết", d.TietLyThuyet),
            ("Tiết thực hành", d.TietThucHanh),
            ("Tỷ lệ ca thực hành (%)", d.TyLeThucHanh),
            ("Học phần có sĩ số lớn nhất", d.HocPhanLonNhat),
            ("Sĩ số học phần lớn nhất", d.SiSoLonNhat)
        };
        var r = 4;
        foreach (var item in rows)
        {
            ws.Cell(r, 1).Value = item.Label;
            switch (item.Value)
            {
                case int i: ws.Cell(r, 2).Value = i; break;
                case double db: ws.Cell(r, 2).Value = db; break;
                default: ws.Cell(r, 2).Value = item.Value?.ToString() ?? ""; break;
            }
            r++;
        }

        r++;
        ws.Cell(r, 1).Value = "Tòa nhà";
        ws.Cell(r, 2).Value = "Số phòng";
        ws.Cell(r, 3).Value = "Số ca trong kỳ";
        ws.Cell(r, 4).Value = "Phòng có lịch";
        ws.Cell(r, 5).Value = "Tỷ lệ lấp đầy (%)";
        ws.Range(r, 1, r, 5).Style.Font.SetBold();
        foreach (var t in d.ToaNhaStats)
        {
            r++;
            ws.Cell(r, 1).Value = t.Label;
            ws.Cell(r, 2).Value = t.SoPhong;
            ws.Cell(r, 3).Value = t.SoCa;
            ws.Cell(r, 4).Value = t.SoPhongCoLich;
            ws.Cell(r, 5).Value = t.TyLeLapDay;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCao_DaoTao_{DateTime.Now:yyyyMMddHHmm}.xlsx");
    }

    /// <summary>Xuất báo cáo tổng hợp ra file CSV (mở được bằng Excel).</summary>
    public async Task<IActionResult> Export(DateOnly? from, DateOnly? to)
    {
        var range = await ResolveRangeAsync(from, to);
        var d = await _thongKe.GetAsync(range.From, range.To);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Chỉ số,Giá trị");
        sb.AppendLine($"Tổng số phòng,{d.TongPhong}");
        sb.AppendLine($"Phòng sẵn sàng,{d.PhongSanSang}");
        sb.AppendLine($"Phòng bảo trì,{d.PhongBaoTri}");
        sb.AppendLine($"Tỷ lệ sử dụng phòng (%),{d.TyLeSuDungPhong}");
        sb.AppendLine($"Tổng số lịch học,{d.TongLich}");
        sb.AppendLine($"Lịch hôm nay,{d.LichHomNay}");
        sb.AppendLine($"Tổng học phần,{d.TongHocPhan}");
        sb.AppendLine($"Tổng giảng viên,{d.TongGiangVien}");
        sb.AppendLine($"Tổng lượt đăng ký,{d.TongSinhVien}");
        sb.AppendLine($"Sự cố mới,{d.SuCoMoi}");
        sb.AppendLine($"Sự cố đang xử lý,{d.SuCoDangXuLy}");
        sb.AppendLine($"Sự cố đã xử lý,{d.SuCoDaXuLy}");
        sb.AppendLine($"Đặt phòng chờ duyệt,{d.DatPhongChoDuyet}");
        sb.AppendLine($"Đặt phòng đã duyệt,{d.DatPhongDaDuyet}");
        sb.AppendLine($"Đơn nghỉ chờ duyệt,{d.DonNghiChoDuyet}");
        sb.AppendLine();
        sb.AppendLine("Phòng theo tòa nhà,Số phòng");
        foreach (var x in d.PhongTheoToaNha) sb.AppendLine($"{x.Label},{x.Value}");
        sb.AppendLine();
        sb.AppendLine("Sự cố theo trạng thái,Số lượng");
        foreach (var x in d.SuCoTheoTrangThai) sb.AppendLine($"{x.Label},{x.Value}");
        sb.AppendLine();
        sb.AppendLine("Lịch theo ca,Số lượng");
        foreach (var x in d.LichTheoCa) sb.AppendLine($"{Catalogs.LabelCa(x.Label)},{x.Value}");

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
        return File(bytes, "text/csv", $"BaoCao_PhongHoc_{DateTime.Now:yyyyMMddHHmm}.csv");
    }
}
