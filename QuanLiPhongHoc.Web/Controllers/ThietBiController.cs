using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Models.ViewModels;
using QuanLiPhongHoc.Web.Services;
using System.Security.Claims;
using ClosedXML.Excel;

namespace QuanLiPhongHoc.Web.Controllers;

[Authorize(Roles = AppRoles.Facility)]
public class ThietBiController : Controller
{
    private readonly QuanLiPhongHocContext _db;
    private readonly IAuditService _audit;

    public ThietBiController(QuanLiPhongHocContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, string? loai, int page = 1)
    {
        const int pageSize = 20;
        var query = _db.ThietBis.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.TenThietBi.Contains(q) || (x.MaCodeThietBi != null && x.MaCodeThietBi.Contains(q)));
        if (!string.IsNullOrWhiteSpace(loai))
            query = query.Where(x => x.LoaiPhong == loai);

        var tongSo = await query.CountAsync();
        var soTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)pageSize));
        page = Math.Clamp(page, 1, soTrang);

        ViewBag.Q = q;
        ViewBag.Loai = loai;
        ViewBag.PhanTrang = new PhanTrangVm
        {
            Trang = page,
            SoTrang = soTrang,
            TongSo = tongSo,
            KichThuoc = pageSize,
            DonVi = "thiết bị",
            TaoUrl = p => Url.Action(nameof(Index), new { q, loai, page = p }) ?? "/ThietBi"
        };
        var thongKeRaw = await _db.PhongThietBis
            .GroupBy(x => x.MaThietBi)
            .Select(g => new
            {
                MaThietBi = g.Key,
                Tong = g.Count(x => x.MaPhong != null),
                Hong = g.Count(x => x.MaPhong != null && x.TrangThai != "GOOD" && x.TrangThai != "RETIRED"),
                Kho = g.Count(x => x.MaPhong == null)
            })
            .ToListAsync();
        ViewBag.ThongKe = thongKeRaw
            .Where(x => x.MaThietBi != null)
            .ToDictionary(x => x.MaThietBi!.Value, x => new ThietBiStatDto { Tong = x.Tong, Hong = x.Hong, Kho = x.Kho });

        return View(await query.OrderBy(x => x.TenThietBi)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var tb = await _db.ThietBis.AsNoTracking().FirstOrDefaultAsync(x => x.MaThietBi == id);
        if (tb is null) return NotFound();
        ViewBag.ThietBi = tb;
        var items = await _db.PhongThietBis
            .Include(x => x.PhongHoc).ThenInclude(x => x!.ToaNha)
            .Where(x => x.MaThietBi == id)
            .OrderBy(x => x.MaPhong == null ? 0 : 1)
            .ThenBy(x => x.PhongHoc!.CodePhong)
            .ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        await FillPhanBoLookupsAsync();
        return View(new ThietBiFormVm { PhamViPhanBo = "PHONG" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThietBiFormVm vm)
    {
        ChuanHoa(vm);
        await KiemTraMaAsync(vm);

        var phongNhan = await XacDinhPhongNhanAsync(vm, batBuoc: true);

        if (!ModelState.IsValid)
        {
            await FillPhanBoLookupsAsync();
            return View(vm);
        }

        var tb = new ThietBi
        {
            TenThietBi = vm.TenThietBi,
            LoaiPhong = vm.LoaiPhong,
            MaCodeThietBi = vm.MaCodeThietBi
        };
        _db.ThietBis.Add(tb);
        await _db.SaveChangesAsync();

        var taoMoi = await TaoBanGhiPhanBoAsync(tb, phongNhan, vm);
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_CREATE", "ThietBi", null,
            $"{tb.MaCodeThietBi}|{tb.TenThietBi}|{tb.LoaiPhong}|{vm.PhamViPhanBo}|{taoMoi}", Ip());
        TempData["Ok"] = $"Đã thêm {tb.TenThietBi} và phân bổ {taoMoi} bản ghi" +
            (vm.PhamViPhanBo == "TOA" ? " cho toàn bộ phòng của tòa đã chọn." : $" vào phòng {phongNhan[0].CodePhong}.");
        return RedirectToAction(nameof(Details), new { id = tb.MaThietBi });
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var tb = await _db.ThietBis.FindAsync(id);
        if (tb is null) return NotFound();
        await FillPhanBoLookupsAsync();
        ViewBag.ReturnUrl = ChonQuayLai(returnUrl, id);
        return View(new ThietBiFormVm
        {
            MaThietBi = tb.MaThietBi,
            MaCodeThietBi = tb.MaCodeThietBi,
            TenThietBi = tb.TenThietBi,
            LoaiPhong = tb.LoaiPhong,
            PhamViPhanBo = ""
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ThietBiFormVm vm, string? returnUrl = null)
    {
        var tb = await _db.ThietBis.FindAsync(vm.MaThietBi);
        if (tb is null) return NotFound();
        ChuanHoa(vm);
        await KiemTraMaAsync(vm);

        var phongNhan = await XacDinhPhongNhanAsync(vm, batBuoc: false);

        if (!ModelState.IsValid)
        {
            await FillPhanBoLookupsAsync();
            ViewBag.ReturnUrl = ChonQuayLai(returnUrl, vm.MaThietBi);
            return View(vm);
        }

        var old = $"{tb.MaCodeThietBi}|{tb.TenThietBi}|{tb.LoaiPhong}";
        tb.MaCodeThietBi = vm.MaCodeThietBi;
        tb.TenThietBi = vm.TenThietBi;
        tb.LoaiPhong = vm.LoaiPhong;
        await _db.SaveChangesAsync();

        var taoMoi = await TaoBanGhiPhanBoAsync(tb, phongNhan, vm);
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_UPDATE", "ThietBi", old,
            $"{tb.MaCodeThietBi}|{tb.TenThietBi}|{tb.LoaiPhong}|{vm.PhamViPhanBo}|{taoMoi}", Ip());
        TempData["Ok"] = taoMoi == 0
            ? "Đã cập nhật thiết bị."
            : $"Đã cập nhật {tb.TenThietBi} và bổ sung {taoMoi} bản ghi thiết bị.";
        return RedirectToAction(nameof(Details), new { id = tb.MaThietBi });
    }

    /// <summary>Xác định danh sách phòng sẽ nhận thiết bị; để trống phạm vi khi sửa nghĩa là chỉ cập nhật thông tin.</summary>
    private async Task<List<PhongHoc>> XacDinhPhongNhanAsync(ThietBiFormVm vm, bool batBuoc)
    {
        vm.PhamViPhanBo = (vm.PhamViPhanBo ?? string.Empty).Trim().ToUpperInvariant();
        var soLuong = Math.Clamp(vm.SoLuong, 1, 100);
        List<PhongHoc> phongNhan = [];

        if (vm.PhamViPhanBo == "PHONG")
        {
            var phong = vm.MaPhong is int mp
                ? await _db.PhongHocs.FirstOrDefaultAsync(x => x.MaPhong == mp)
                : null;
            if (phong is null)
                ModelState.AddModelError(nameof(vm.MaPhong), "Chọn phòng cụ thể để phân bổ.");
            else phongNhan.Add(phong);
        }
        else if (vm.PhamViPhanBo == "TOA")
        {
            phongNhan = vm.MaToaNha is int mt
                ? await _db.PhongHocs.Where(x => x.MaToaNha == mt).OrderBy(x => x.CodePhong).ToListAsync()
                : [];
            if (vm.MaToaNha is null)
                ModelState.AddModelError(nameof(vm.MaToaNha), "Chọn tòa nhà để phân bổ.");
            else if (phongNhan.Count == 0)
                ModelState.AddModelError(nameof(vm.MaToaNha), "Tòa nhà chưa có phòng học nào để phân bổ.");
            else if (phongNhan.Count * soLuong > 400)
                ModelState.AddModelError(nameof(vm.MaToaNha), $"Tòa nhà có {phongNhan.Count} phòng × {soLuong} thiết bị = {phongNhan.Count * soLuong} bản ghi, vượt mức 400. Giảm số lượng mỗi phòng hoặc chọn phòng cụ thể.");
        }
        else
        {
            vm.PhamViPhanBo = string.Empty;
            if (batBuoc) ModelState.AddModelError(nameof(vm.PhamViPhanBo), "Chọn nơi phân bổ thiết bị.");
        }

        return phongNhan;
    }

    private async Task<int> TaoBanGhiPhanBoAsync(ThietBi tb, List<PhongHoc> phongNhan, ThietBiFormVm vm)
    {
        var soLuong = Math.Clamp(vm.SoLuong, 1, 100);
        var trangThai = Catalogs.TrangThaiThietBi.ContainsKey(vm.TrangThai) ? vm.TrangThai : "GOOD";
        var nhanGoc = string.IsNullOrWhiteSpace(vm.NhanDienThietBi) ? tb.TenThietBi : vm.NhanDienThietBi.Trim();
        var taoMoi = 0;
        foreach (var phong in phongNhan)
        {
            for (var i = 0; i < soLuong; i++)
            {
                var nhan = phongNhan.Count > 1 || soLuong > 1
                    ? $"{nhanGoc} · {phong.CodePhong} #{i + 1}"
                    : $"{nhanGoc} · {phong.CodePhong}";
                _db.PhongThietBis.Add(new PhongThietBi
                {
                    MaPhong = phong.MaPhong,
                    MaThietBi = tb.MaThietBi,
                    NhanDienThietBi = nhan,
                    TrangThai = trangThai
                });
                taoMoi++;
            }
        }
        if (taoMoi > 0) await _db.SaveChangesAsync();
        return taoMoi;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tb = await _db.ThietBis.FindAsync(id);
        if (tb is not null)
        {
            if (await _db.PhongThietBis.AnyAsync(x => x.MaThietBi == id && x.MaPhong != null))
            {
                TempData["Error"] = $"Thiết bị {tb.TenThietBi} vẫn đang được phân bổ trong phòng nên không thể xóa.";
                return RedirectToAction(nameof(Details), new { id });
            }
            _db.ThietBis.Remove(tb);
            await _db.SaveChangesAsync();
            await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_DELETE", "ThietBi", tb.TenThietBi, null, Ip());
            TempData["Ok"] = "Đã xóa loại thiết bị.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Gán một loại thiết bị vào phòng học (có thể tạo nhiều bản ghi với nhãn khác nhau).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gan(GanThietBiVm vm)
    {
        var phong = await _db.PhongHocs.FindAsync(vm.MaPhong);
        var tb = await _db.ThietBis.FindAsync(vm.MaThietBi);
        if (phong is null || tb is null)
        {
            TempData["Error"] = "Phòng hoặc thiết bị không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        var soLuong = Math.Clamp(vm.SoLuong, 1, 200);
        var trangThai = Catalogs.TrangThaiThietBi.ContainsKey(vm.trangThai) ? vm.trangThai : "GOOD";
        for (var i = 0; i < soLuong; i++)
        {
            _db.PhongThietBis.Add(new PhongThietBi
            {
                MaPhong = phong.MaPhong,
                MaThietBi = tb.MaThietBi,
                NhanDienThietBi = string.IsNullOrWhiteSpace(vm.NhanDienThietBi)
                    ? (soLuong > 1 ? $"{tb.TenThietBi} #{i + 1}" : tb.TenThietBi)
                    : (soLuong > 1 ? $"{vm.NhanDienThietBi.Trim()} #{i + 1}" : vm.NhanDienThietBi.Trim()),
                TrangThai = trangThai
            });
        }
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "ACTION_CREATE", "Phong_ThietBi",
            null, $"{phong.CodePhong}|{tb.TenThietBi}|x{soLuong}", Ip());
        TempData["Ok"] = $"Đã gán {soLuong} thiết bị vào phòng {phong.CodePhong}.";
        return RedirectToAction("Details", "PhongHoc", new { id = phong.MaPhong });
    }

    [HttpGet]
    public async Task<IActionResult> Combobox(int maPhong)
    {
        var phong = await _db.PhongHocs.FindAsync(maPhong);
        var ds = await _db.ThietBis
            .Where(x => x.LoaiPhong == null || phong == null || x.LoaiPhong == phong.LoaiPhong)
            .Select(x => new { x.MaThietBi, x.TenThietBi, x.LoaiPhong, x.MaCodeThietBi })
            .ToListAsync();
        return Json(ds);
    }

    /// <summary>Danh sách phòng của một tòa để form thêm thiết bị lọc theo tòa đã chọn.</summary>
    [HttpGet]
    public async Task<IActionResult> PhongTheoToa(int maToa)
    {
        var ds = await _db.PhongHocs.AsNoTracking()
            .Where(x => x.MaToaNha == maToa)
            .OrderBy(x => x.CodePhong)
            .Select(x => new { x.MaPhong, x.CodePhong, x.LoaiPhong })
            .ToListAsync();
        return Json(ds);
    }

    /// <summary>Xuất báo cáo tình trạng các bản ghi thiết bị để gửi kèm biên bản.</summary>
    [HttpGet]
    public async Task<IActionResult> XuatBaoCao(int id)
    {
        var tb = await _db.ThietBis.AsNoTracking().FirstOrDefaultAsync(x => x.MaThietBi == id);
        if (tb is null) return NotFound();

        var items = await _db.PhongThietBis.AsNoTracking()
            .Include(x => x.PhongHoc).ThenInclude(x => x!.ToaNha)
            .Where(x => x.MaThietBi == id)
            .OrderBy(x => x.MaPhong == null ? 0 : 1)
            .ThenBy(x => x.PhongHoc!.CodePhong)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ThietBi");
        ws.Cell(1, 1).Value = $"BÁO CÁO THIẾT BỊ: {tb.TenThietBi}";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        ws.Cell(2, 1).Value = $"Mã thiết bị: {tb.MaCodeThietBi ?? "—"} · Loại phòng: {Catalogs.LabelLoaiPhong(tb.LoaiPhong)}";
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(3, 1).Value = $"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm} · Tổng {items.Count} bản ghi";
        ws.Range(3, 1, 3, 7).Merge();

        var headers = new[] { "STT", "Vị trí", "Tòa nhà", "Nhãn nhận diện", "Trạng thái", "Mã loại thiết bị", "Ghi chú" };
        for (var i = 0; i < headers.Length; i++) ws.Cell(5, i + 1).Value = headers[i];
        ws.Range(5, 1, 5, headers.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EDF2"));

        var row = 6;
        foreach (var item in items)
        {
            ws.Cell(row, 1).Value = row - 5;
            ws.Cell(row, 2).Value = item.PhongHoc?.CodePhong ?? "Trong kho";
            ws.Cell(row, 3).Value = item.PhongHoc?.ToaNha?.CodeToaNha ?? "—";
            ws.Cell(row, 4).Value = item.NhanDienThietBi ?? "";
            ws.Cell(row, 5).Value = Catalogs.Label(Catalogs.TrangThaiThietBi, item.TrangThai);
            ws.Cell(row, 6).Value = tb.MaCodeThietBi ?? "";
            ws.Cell(row, 7).Value = item.TrangThai == "GOOD" ? "" : "Cần theo dõi / xử lý";
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoThietBi_{tb.MaCodeThietBi ?? tb.MaThietBi.ToString()}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    private async Task FillPhanBoLookupsAsync()
    {
        ViewBag.ToaNhas = await _db.ToaNhas.AsNoTracking().OrderBy(x => x.CodeToaNha).ToListAsync();
        ViewBag.Phongs = await _db.PhongHocs.AsNoTracking().OrderBy(x => x.CodePhong).ToListAsync();
    }

    private static void ChuanHoa(ThietBiFormVm vm)
    {
        vm.TenThietBi = vm.TenThietBi?.Trim() ?? string.Empty;
        vm.MaCodeThietBi = string.IsNullOrWhiteSpace(vm.MaCodeThietBi)
            ? null
            : vm.MaCodeThietBi.Trim().ToUpperInvariant();
    }

    private async Task KiemTraMaAsync(ThietBiFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.MaCodeThietBi)) return;
        if (await _db.ThietBis.AnyAsync(x => x.MaCodeThietBi == vm.MaCodeThietBi && x.MaThietBi != vm.MaThietBi))
            ModelState.AddModelError(nameof(vm.MaCodeThietBi), $"Mã thiết bị {vm.MaCodeThietBi} đã được sử dụng.");
    }

    private string ChonQuayLai(string? returnUrl, int id)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(Details), new { id }) ?? $"/ThietBi/Details/{id}";

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
