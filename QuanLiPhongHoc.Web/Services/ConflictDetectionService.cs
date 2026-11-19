using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public class ConflictResult
{
    public bool HasConflict => Messages.Count > 0;
    public List<string> Messages { get; } = [];

    public void Add(string message)
    {
        if (!Messages.Contains(message)) Messages.Add(message);
    }
}

public interface IConflictDetectionService
{
    /// <summary>Kiểm tra trùng phòng cho một ca học (lịch xếp thời khóa biểu).</summary>
    Task<ConflictResult> CheckRoomAsync(int maPhong, DateOnly ngay, string caHoc, int? excludeLichHocId = null, int? excludeDatPhongId = null, bool includePending = false);

    /// <summary>
    /// Kiểm tra một khung giờ xin mượn tự do: trùng lịch học, trùng phiếu mượn khác và
    /// phải nằm trọn trong khung giờ trống CSVC đã khai cho phòng.
    /// <paramref name="includePending"/> true khi tạo phiếu mới (chặn trùng với phiếu đang chờ),
    /// false khi duyệt phiếu (chỉ phiếu đã duyệt mới thực sự giữ phòng).
    /// </summary>
    Task<ConflictResult> CheckMuonPhongAsync(int maPhong, DateOnly ngay, TimeOnly tu, TimeOnly den, int? excludeDatPhongId = null, bool includePending = true);

    /// <summary>Kiểm tra trùng giảng viên.</summary>
    Task<ConflictResult> CheckLecturerAsync(string maGiangVien, DateOnly ngay, string caHoc, int? excludeLichHocId = null);

    /// <summary>Kiểm tra tổng hợp phòng + giảng viên.</summary>
    Task<ConflictResult> CheckScheduleAsync(int maPhong, DateOnly ngay, string caHoc, string? maGiangVien, int? excludeLichHocId = null);

    /// <summary>
    /// Kiểm tra đầy đủ 5 ràng buộc khi xếp lịch:
    /// trùng phòng, trùng giảng viên, trùng lớp học phần, vượt sức chứa, sai loại phòng.
    /// </summary>
    Task<ConflictResult> CheckLichHocAsync(
        int maPhong, DateOnly ngay, string caHoc, string maHocPhan,
        int? excludeLichHocId = null, string? maGiangVienDayThay = null);
}

public class ConflictDetectionService : IConflictDetectionService
{
    private readonly QuanLiPhongHocContext _db;

    public ConflictDetectionService(QuanLiPhongHocContext db) => _db = db;

    public Task<ConflictResult> CheckRoomAsync(int maPhong, DateOnly ngay, string caHoc, int? excludeLichHocId = null, int? excludeDatPhongId = null, bool includePending = false)
    {
        var ca = Catalogs.GetCaTime(caHoc);
        if (ca is null)
            return Task.FromResult(new ConflictResult());
        return CheckKhoangAsync(maPhong, ngay, ca.Value.Start, ca.Value.End,
            $"ca {Catalogs.LabelCa(caHoc)} [{Catalogs.CaTimeRange(caHoc)}]",
            excludeLichHocId, excludeDatPhongId, includePending, kiemKhungTrong: false);
    }

    public Task<ConflictResult> CheckMuonPhongAsync(int maPhong, DateOnly ngay, TimeOnly tu, TimeOnly den, int? excludeDatPhongId = null, bool includePending = true)
        => CheckKhoangAsync(maPhong, ngay, tu, den, $"khung giờ xin mượn [{Catalogs.MoTaKhoang(tu, den)}]",
            excludeLichHocId: null, excludeDatPhongId, includePending, kiemKhungTrong: true);

    /// <summary>
    /// Phần lõi chung: mọi giữ chỗ (lịch học quy từ ca sang giờ, phiếu mượn lưu giờ cụ thể)
    /// đều so với khung đang xét theo khoảng thời gian, không so theo mã ca.
    /// </summary>
    private async Task<ConflictResult> CheckKhoangAsync(
        int maPhong, DateOnly ngay, TimeOnly tu, TimeOnly den, string moTaXin,
        int? excludeLichHocId, int? excludeDatPhongId, bool includePending, bool kiemKhungTrong)
    {
        var result = new ConflictResult();

        var phong = await _db.PhongHocs.AsNoTracking()
            .Include(x => x.KhungTrongs)
            .FirstOrDefaultAsync(x => x.MaPhong == maPhong);
        if (phong == null)
        {
            result.Add("Phòng học không tồn tại.");
            return result;
        }
        if (phong.TrangThaiPhong is "MAINTENANCE" or "UNAVAILABLE")
        {
            result.Add($"Phòng {phong.CodePhong} đang {Catalogs.LabelTrangThaiPhong(phong.TrangThaiPhong).ToLower()} nên không nhận đăng ký mới.");
        }

        // Lịch học lưu theo ca nên phải quy ra khoảng giờ rồi mới so trùng.
        var lichTrongNgay = await _db.LichHocs
            .Include(x => x.HocPhan)
            .Include(x => x.PhongHoc)
            .Where(x => x.MaPhong == maPhong && x.NgayHoc == ngay && x.TrangThaiHoatDong)
            .Where(x => excludeLichHocId == null || x.MaLichHoc != excludeLichHocId)
            .ToListAsync();

        // Sân thể dục ngoài trời dùng chung: cho phép tối đa 3 lớp trong cùng một ca,
        // các phòng còn lại mỗi ca chỉ chứa tối đa 1 lớp.
        var laSan = phong.LoaiPhong == Catalogs.SanTheDucNgoaiTroi;
        var demLichCungKhung = 0;
        foreach (var lich in lichTrongNgay)
        {
            if (Catalogs.GetCaTime(lich.CaHoc) is not (var batDau, var ketThuc)) continue;
            if (!Catalogs.IsOverlap(batDau, ketThuc, tu, den)) continue;

            if (laSan)
            {
                demLichCungKhung++;
                continue;
            }

            result.Add($"Trùng phòng {lich.PhongHoc?.CodePhong}: đã có lịch {lich.HocPhan?.TenHocPhan} ({lich.MaHocPhan}) " +
                       $"ngày {ngay:dd/MM/yyyy} ca {Catalogs.LabelCa(lich.CaHoc)} [{Catalogs.CaTimeRange(lich.CaHoc)}] giao với {moTaXin}.");
        }

        if (laSan && demLichCungKhung >= Catalogs.SucChuaSanMoiCa)
        {
            result.Add($"Sân thể dục {phong.CodePhong} đã có {demLichCungKhung} lớp trong cùng khung giờ " +
                       $"— chỉ nhận tối đa {Catalogs.SucChuaSanMoiCa} lớp/ca.");
        }

        var trangThaiCanChong = includePending
            ? new[] { "APPROVED", "PENDING" }
            : ["APPROVED"];

        var datTrongNgay = await _db.DatPhongs
            .Include(x => x.PhongHoc)
            .Where(x => x.MaPhong == maPhong && x.NgayDat == ngay && trangThaiCanChong.Contains(x.TrangThai))
            .Where(x => excludeDatPhongId == null || x.MaDatPhong != excludeDatPhongId)
            .ToListAsync();

        foreach (var dat in datTrongNgay.Where(x => Catalogs.IsOverlap(x.GioBatDau, x.GioKetThuc, tu, den)))
        {
            result.Add($"Khung giờ phòng đã được mượn vào khung giờ đó {Catalogs.MoTaKhoang(dat.GioBatDau, dat.GioKetThuc)}" +
                       $" ({ngay:dd/MM/yyyy}, phiếu {Catalogs.Label(Catalogs.TrangThaiDatPhong, dat.TrangThai)}) — vui lòng chọn giờ khác.");
        }

        if (kiemKhungTrong)
        {
            IEnumerable<(TimeOnly Start, TimeOnly End)> khaiBao = phong.KhungTrongs.Count > 0
                ? phong.KhungTrongs.Select(x => (x.GioBatDau, x.GioKetThuc))
                : Catalogs.KhungTrongMacDinh;
            var cacKhung = khaiBao.OrderBy(x => x.Start).ToList();
            if (!cacKhung.Any(k => Catalogs.NamTrongKhung(k, tu, den)))
            {
                result.Add($"Phòng {phong.CodePhong} chỉ trống trong " +
                           $"{string.Join(" · ", cacKhung.Select(k => Catalogs.MoTaKhoang(k.Start, k.End)))}" +
                           $" — vui lòng chọn khung giờ nằm trọn trong một khoảng trống.");
            }
        }

        return result;
    }

    public async Task<ConflictResult> CheckLecturerAsync(string maGiangVien, DateOnly ngay, string caHoc, int? excludeLichHocId = null)
    {
        var result = new ConflictResult();

        var lichTrongNgay = await _db.LichHocs
            .Include(x => x.HocPhan)
            .Include(x => x.PhongHoc)
            .Where(x => x.TrangThaiHoatDong && x.NgayHoc == ngay && x.HocPhan != null)
            .Where(x => excludeLichHocId == null || x.MaLichHoc != excludeLichHocId)
            .ToListAsync();

        foreach (var lich in lichTrongNgay.Where(x =>
                     string.Equals(x.MaGiangVienDayThay ?? x.HocPhan?.MaGiangVien, maGiangVien, StringComparison.OrdinalIgnoreCase)
                     && Catalogs.IsOverlap(x.CaHoc, caHoc)))
        {
            result.Add($"Giảng viên {maGiangVien} đã dạy {lich.HocPhan?.TenHocPhan} tại {lich.PhongHoc?.CodePhong} " +
                       $"ngày {ngay:dd/MM/yyyy} ca {Catalogs.LabelCa(lich.CaHoc)} — trùng giảng viên.");
        }

        return result;
    }

    public async Task<ConflictResult> CheckScheduleAsync(int maPhong, DateOnly ngay, string caHoc, string? maGiangVien, int? excludeLichHocId = null)
    {
        var result = await CheckRoomAsync(maPhong, ngay, caHoc, excludeLichHocId);
        if (!string.IsNullOrWhiteSpace(maGiangVien))
        {
            var gv = await CheckLecturerAsync(maGiangVien, ngay, caHoc, excludeLichHocId);
            foreach (var m in gv.Messages) result.Add(m);
        }
        return result;
    }

    public async Task<ConflictResult> CheckLichHocAsync(
        int maPhong, DateOnly ngay, string caHoc, string maHocPhan,
        int? excludeLichHocId = null, string? maGiangVienDayThay = null)
    {
        var result = new ConflictResult();

        var phong = await _db.PhongHocs.AsNoTracking()
            .Include(x => x.ToaNha)
            .Include(x => x.KhungTrongs)
            .Include(x => x.PhongThietBis).ThenInclude(x => x.ThietBi)
            .FirstOrDefaultAsync(x => x.MaPhong == maPhong);
        var hocPhan = await _db.HocPhans.FirstOrDefaultAsync(x => x.MaHocPhan == maHocPhan);

        if (phong == null)
        {
            result.Add("Phòng học không tồn tại.");
            return result;
        }
        if (hocPhan == null)
        {
            result.Add("Học phần không tồn tại.");
            return result;
        }

        // (1) Trùng phòng
        var room = await CheckRoomAsync(maPhong, ngay, caHoc, excludeLichHocId);
        foreach (var m in room.Messages) result.Add(m);

        // (2) Trùng giảng viên
        if (!string.IsNullOrWhiteSpace(hocPhan.MaGiangVien))
        {
            var gv = await CheckLecturerAsync(
                maGiangVienDayThay ?? hocPhan.MaGiangVien, ngay, caHoc, excludeLichHocId);
            foreach (var m in gv.Messages) result.Add(m);
        }
        else if (!string.IsNullOrWhiteSpace(maGiangVienDayThay))
        {
            var gv = await CheckLecturerAsync(maGiangVienDayThay, ngay, caHoc, excludeLichHocId);
            foreach (var m in gv.Messages) result.Add(m);
        }

        // (3) Trùng lớp học phần (cùng học phần không thể học 2 phòng cùng lúc)
        var cungHocPhan = await _db.LichHocs
            .Include(x => x.PhongHoc)
            .Where(x => x.MaHocPhan == maHocPhan && x.NgayHoc == ngay && x.TrangThaiHoatDong)
            .Where(x => excludeLichHocId == null || x.MaLichHoc != excludeLichHocId)
            .ToListAsync();
        foreach (var lich in cungHocPhan.Where(x => Catalogs.IsOverlap(x.CaHoc, caHoc)))
        {
            result.Add($"Học phần {maHocPhan} đã được xếp tại {lich.PhongHoc?.CodePhong} cùng khung giờ — trùng lớp sinh viên.");
        }

        // (4) Vượt sức chứa: Sĩ số lớp <= Sức chứa phòng
        if (hocPhan.SoLuongDangKy > phong.SucChua)
        {
            result.Add($"Vượt sức chứa: lớp {hocPhan.SoLuongDangKy} sinh viên > sức chứa phòng {phong.CodePhong} ({phong.SucChua} chỗ).");
        }

        if (phong.TrangThaiPhong != "AVAILABLE")
            result.Add($"Phòng {phong.CodePhong} không ở trạng thái sẵn sàng của CSVC.");
        if (!Catalogs.ToaNhaDangHoatDong(phong.ToaNha))
            result.Add($"Tòa nhà của phòng {phong.CodePhong} đang ngừng hoạt động hoặc bảo trì.");
        if (Catalogs.GetCaTime(caHoc) is not (var gioBatDau, var gioKetThuc)
            || phong.KhungTrongs.Count == 0
            || !phong.KhungTrongs.Any(k => Catalogs.NamTrongKhung(
                (k.GioBatDau, k.GioKetThuc), gioBatDau, gioKetThuc)))
            result.Add($"Phòng {phong.CodePhong} không có khung giờ CSVC khai báo bao trọn ca học này.");
        if (!RoomHasRequiredWorkingEquipment(phong))
            result.Add($"Phòng {phong.CodePhong} chưa đủ thiết bị chuẩn ở trạng thái Tốt.");

        // (5) Loại phòng chuyên dụng và nhóm ngành
        if (maHocPhan.StartsWith("GDTC", StringComparison.OrdinalIgnoreCase))
        {
            // Môn Giáo dục thể chất (mã GDTC) chỉ học ngoài sân thể dục.
            if (phong.LoaiPhong != Catalogs.SanTheDucNgoaiTroi)
            {
                result.Add($"Học phần {maHocPhan} là môn Giáo dục thể chất nhưng phòng {phong.CodePhong} là " +
                           $"{Catalogs.LabelLoaiPhong(phong.LoaiPhong)} — phải xếp ngoài sân thể dục.");
            }
        }
        else if (!Catalogs.LaPhongXepLich(phong.LoaiPhong))
        {
            result.Add($"Phòng {phong.CodePhong} là {Catalogs.LabelLoaiPhong(phong.LoaiPhong)} — không dùng để xếp lịch học.");
        }
        else if (hocPhan.LaThucHanh)
        {
            if (!Catalogs.LaPhongThucHanh(phong.LoaiPhong))
            {
                result.Add($"Học phần {maHocPhan} là lớp thực hành nhưng phòng {phong.CodePhong} là {Catalogs.LabelLoaiPhong(phong.LoaiPhong)} — sai loại phòng chuyên dụng.");
            }
            else if (!Catalogs.PhongPhuHopNhomNganh(phong.LoaiPhong, hocPhan.NhomNganh))
            {
                result.Add($"Học phần {maHocPhan} thuộc nhóm {Catalogs.LabelNhomNganh(hocPhan.NhomNganh)} " +
                           $"nhưng phòng {phong.CodePhong} là {Catalogs.LabelLoaiPhong(phong.LoaiPhong)} — không đúng phòng chuyên ngành.");
            }
        }
        else if (phong.LoaiPhong != Catalogs.RoomTheory)
        {
            result.Add($"Học phần {maHocPhan} là lớp lý thuyết nhưng phòng {phong.CodePhong} là {Catalogs.LabelLoaiPhong(phong.LoaiPhong)} — chỉ xếp lớp lý thuyết vào phòng lý thuyết.");
        }

        // Phòng đang bảo trì/không khả dụng
        return result;
    }

    private static bool RoomHasRequiredWorkingEquipment(PhongHoc phong)
    {
        if (!Catalogs.ThietBiTheoLoaiPhong.TryGetValue(phong.LoaiPhong, out var required)
            || required.Length == 0)
            return true;

        var workingEquipment = phong.PhongThietBis
            .Where(x => x.TrangThai == "GOOD" && x.ThietBi is not null)
            .Select(x => x.ThietBi!.TenThietBi)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return required.All(workingEquipment.Contains);
    }
}
