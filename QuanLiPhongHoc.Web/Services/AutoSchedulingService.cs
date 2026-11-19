using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public class AutoScheduleOptions
{
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(28));
    public List<string> CaHocs { get; set; } = [];
    public int MaxNodes { get; set; } = 200_000;
    public bool Persist { get; set; } = true;
}

public class SlotAssignment
{
    public string MaHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public int MaPhong { get; set; }
    public string CodePhong { get; set; } = string.Empty;
    public DateOnly NgayHoc { get; set; }
    public string CaHoc { get; set; } = string.Empty;
    public string? MaGiangVien { get; set; }

    /// <summary>Tên giảng viên phụ trách, phục vụ hiển thị và xuất Excel.</summary>
    public string? TenGiangVien { get; set; }

    /// <summary>Số tiết của ca được xếp: 3 tiết (lý thuyết) hoặc 5 tiết (thực hành).</summary>
    public int SoTiet { get; set; }
}

public class AutoScheduleFailure
{
    public string MaHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class AutoScheduleResult
{
    public bool Success { get; set; }
    public List<SlotAssignment> Assigned { get; set; } = [];
    public List<AutoScheduleFailure> Failed { get; set; } = [];
    public List<string> Skipped { get; set; } = [];
    public int NodesExplored { get; set; }
    public double ElapsedMs { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IAutoSchedulingService
{
    Task<AutoScheduleResult> RunAsync(List<string> maHocPhans, AutoScheduleOptions options);
}

/// <summary>
/// Xếp lịch tự động bằng thuật toán quay lui (Backtracking) kết hợp heuristic MRV
/// (chọn lớp có ít phương án khả thi nhất trước). Khi bế tắc, thuật toán lùi lại
/// để điều chỉnh các lớp đã xếp trước đó nhằm tìm phương án hợp lệ.
/// </summary>
public class AutoSchedulingService : IAutoSchedulingService
{
    private readonly QuanLiPhongHocContext _db;

    public AutoSchedulingService(QuanLiPhongHocContext db) => _db = db;

    private sealed record Slot(int MaPhong, DateOnly Ngay, string CaHoc, int SoTiet);

    // Ca học lý thuyết (3 tiết) / thực hành (5 tiết) lấy từ cấu hình đang áp dụng.
    private static HashSet<string> CaLyThuyetHienTai() =>
        Catalogs.CaLyThuyet.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> CaThucHanhHienTai() =>
        Catalogs.CaThucHanh.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<AutoScheduleResult> RunAsync(List<string> maHocPhans, AutoScheduleOptions options)
    {
        var sw = Stopwatch.StartNew();
        var result = new AutoScheduleResult();

        if (options.CaHocs.Count == 0)
            options.CaHocs = Catalogs.CaHoc.Select(x => x.Code).ToList();

        var rooms = await _db.PhongHocs
            .AsNoTracking()
            .Include(x => x.ToaNha)
            .Include(x => x.KhungTrongs)
            .Include(x => x.PhongThietBis)
                .ThenInclude(x => x.ThietBi)
            .Where(x => x.TrangThaiPhong == "AVAILABLE")
            .ToListAsync();
        rooms = rooms.Where(x => Catalogs.ToaNhaDangHoatDong(x.ToaNha)).ToList();

        var courses = await _db.HocPhans
            .Include(x => x.GiangVien)
            .Where(x => maHocPhans.Contains(x.MaHocPhan))
            .ToListAsync();

        if (rooms.Count == 0 || courses.Count == 0)
        {
            result.Message = "Không có phòng khả dụng hoặc không có lớp học phần nào được chọn.";
            return result;
        }
        var roomsWithWorkingEquipment = rooms.Where(RoomCoDuThietBiHoatDong).ToList();

        // Lịch đã có trong hệ thống được coi là ràng buộc cố định.
        var existing = await _db.LichHocs
            .Include(x => x.HocPhan)
            .Where(x => x.TrangThaiHoatDong)
            .ToListAsync();

        var roomBusy = new Dictionary<(int, DateOnly), List<(TimeOnly Start, TimeOnly End)>>();
        var lecturerBusy = new Dictionary<(string, DateOnly), List<(TimeOnly Start, TimeOnly End)>>();

        // Sân thể dục ngoài trời dùng chung: tối đa 3 lớp trong cùng một ca.
        var sanRoomIds = rooms.Where(r => r.LoaiPhong == Catalogs.SanTheDucNgoaiTroi)
            .Select(r => r.MaPhong).ToHashSet();

        foreach (var lich in existing)
        {
            if (lich.MaPhong is null) continue;
            if (Catalogs.GetCaTime(lich.CaHoc) is not (var batDau, var ketThuc)) continue;

            AddBusy(roomBusy, (lich.MaPhong.Value, lich.NgayHoc), batDau, ketThuc);
            var gv = lich.MaGiangVienDayThay ?? lich.HocPhan?.MaGiangVien;
            if (!string.IsNullOrWhiteSpace(gv))
                AddBusy(lecturerBusy, (gv, lich.NgayHoc), batDau, ketThuc);
        }

        // Phiếu mượn đã duyệt cũng là ràng buộc cố định, dù không nằm trong LichHoc.
        var phieuDaDuyet = await _db.DatPhongs
            .Where(x => x.TrangThai == "APPROVED" && x.MaPhong != null
                        && x.NgayDat >= options.StartDate && x.NgayDat <= options.EndDate)
            .ToListAsync();
        foreach (var phieu in phieuDaDuyet)
            AddBusy(roomBusy, (phieu.MaPhong!.Value, phieu.NgayDat), phieu.GioBatDau, phieu.GioKetThuc);

        // Bỏ qua các lớp đã có lịch hoạt động.
        var daXep = existing.Where(x => x.MaHocPhan != null).Select(x => x.MaHocPhan!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = new List<HocPhan>();
        foreach (var c in courses)
        {
            if (daXep.Contains(c.MaHocPhan))
                result.Skipped.Add($"{c.MaHocPhan} - {c.TenHocPhan} (đã có lịch)");
            else
                pending.Add(c);
        }

        if (pending.Count == 0)
        {
            result.Success = true;
            result.Message = "Tất cả lớp được chọn đã có lịch, không cần xếp thêm.";
            result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
            return result;
        }

        var days = new List<DateOnly>();
        for (var d = options.StartDate; d <= options.EndDate; d = d.AddDays(1))
            days.Add(d);

        var assignment = new Dictionary<string, Slot>(StringComparer.OrdinalIgnoreCase);
        var remaining = pending.ToDictionary(x => x.MaHocPhan, StringComparer.OrdinalIgnoreCase);
        var nodes = 0;
        var aborted = false;

        // Sinh các slot tĩnh khả thi (đã lọc theo sức chứa + loại phòng).
        var staticSlots = new Dictionary<string, List<Slot>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in pending)
            staticSlots[c.MaHocPhan] = BuildStaticSlots(c, roomsWithWorkingEquipment, days, options.CaHocs);

        bool Backtrack()
        {
            if (remaining.Count == 0) return true;
            if (++nodes > options.MaxNodes) { aborted = true; return false; }

            // MRV: chọn lớp có ít phương án khả thi nhất.
            HocPhan? next = null;
            var bestCount = int.MaxValue;
            foreach (var c in remaining.Values)
            {
                var count = FeasibleSlots(c, staticSlots[c.MaHocPhan], roomBusy, lecturerBusy, sanRoomIds).Count;
                if (count < bestCount)
                {
                    bestCount = count;
                    next = c;
                    if (count == 0) break;
                }
            }

            if (next is null) return true;
            if (bestCount == 0) return false;

            foreach (var slot in FeasibleSlots(next, staticSlots[next.MaHocPhan], roomBusy, lecturerBusy, sanRoomIds))
            {
                Place(roomBusy, lecturerBusy, next, slot);
                assignment[next.MaHocPhan] = slot;
                remaining.Remove(next.MaHocPhan);

                if (Backtrack()) return true;

                remaining[next.MaHocPhan] = next;
                assignment.Remove(next.MaHocPhan);
                Unplace(roomBusy, lecturerBusy, next, slot);
            }
            return false;
        }

        var solved = Backtrack();

        // Khi quay lui không sắp được trọn bộ, vẫn tận dụng phòng/giảng viên còn trống
        // để xếp tối đa số lớp có thể; phần dư sẽ báo cho người dùng.
        if (!solved)
        {
            bool tienTrien;
            do
            {
                tienTrien = false;
                foreach (var c in remaining.Values.ToList())
                {
                    var slot = FeasibleSlots(c, staticSlots[c.MaHocPhan], roomBusy, lecturerBusy, sanRoomIds).FirstOrDefault();
                    if (slot is null) continue;
                    Place(roomBusy, lecturerBusy, c, slot);
                    assignment[c.MaHocPhan] = slot;
                    remaining.Remove(c.MaHocPhan);
                    tienTrien = true;
                }
            } while (tienTrien);
            solved = remaining.Count == 0;
        }

        result.NodesExplored = nodes;
        result.Success = solved;

        // Ghi nhận kết quả đã xếp được (kể cả khi chỉ xếp được một phần).
        foreach (var c in pending)
        {
            if (!assignment.TryGetValue(c.MaHocPhan, out var slot)) continue;
            result.Assigned.Add(new SlotAssignment
            {
                MaHocPhan = c.MaHocPhan,
                TenHocPhan = c.TenHocPhan,
                MaPhong = slot.MaPhong,
                CodePhong = rooms.First(r => r.MaPhong == slot.MaPhong).CodePhong,
                NgayHoc = slot.Ngay,
                CaHoc = slot.CaHoc,
                MaGiangVien = c.MaGiangVien,
                TenGiangVien = c.GiangVien?.HoTen,
                SoTiet = slot.SoTiet
            });
        }

        if (options.Persist && result.Assigned.Count > 0)
        {
            foreach (var a in result.Assigned)
            {
                _db.LichHocs.Add(new LichHoc
                {
                    MaHocPhan = a.MaHocPhan,
                    MaPhong = a.MaPhong,
                    NgayHoc = a.NgayHoc,
                    CaHoc = a.CaHoc,
                    TrangThaiHoatDong = true
                });
            }
            await _db.SaveChangesAsync();
        }

        if (solved)
        {
            result.Message = $"Xếp lịch thành công cho {result.Assigned.Count} lớp sau khi duyệt {nodes} nút tìm kiếm.";
        }
        else
        {
            // Báo cáo rõ các lớp chưa xếp được và lý do để nhân viên xử lý thủ công.
            foreach (var c in pending)
            {
                if (assignment.ContainsKey(c.MaHocPhan)) continue;
                result.Failed.Add(new AutoScheduleFailure
                {
                    MaHocPhan = c.MaHocPhan,
                    TenHocPhan = c.TenHocPhan,
                    Reason = staticSlots[c.MaHocPhan].Count == 0
                        ? LyDoKhongCoSlot(c, rooms)
                        : "Hết chỗ trống sau khi quay lui toàn bộ không gian tìm kiếm."
                });
            }

            result.Message = aborted
                ? $"Quay lui vượt giới hạn {options.MaxNodes} nút. " +
                  (result.Assigned.Count > 0
                      ? $"Đã xếp được {result.Assigned.Count} lớp, còn {result.Failed.Count} lớp chưa xếp được."
                      : "Chưa xếp được lớp nào. Hãy mở rộng khoảng ngày, thêm ca học hoặc thêm phòng khả dụng.")
                : result.Assigned.Count > 0
                    ? $"Đã xếp được {result.Assigned.Count} lớp, còn {result.Failed.Count} lớp chưa xếp được."
                    : "Không tìm được phương án xếp lịch hợp lệ cho danh sách lớp.";
        }

        result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
        return result;
    }

    private static List<Slot> BuildStaticSlots(HocPhan course, List<PhongHoc> rooms, List<DateOnly> days, List<string> caHocs)
    {
        var slots = new List<Slot>();
        foreach (var room in rooms)
        {
            if (!RoomMatches(room, course)) continue;

            // Phòng chưa khai khung giờ thì dùng ba khung mặc định, đồng bộ với
            // trang mượn phòng và kiểm tra trùng lịch.
            List<(TimeOnly Start, TimeOnly End)> khung = room.KhungTrongs.Count > 0
                ? room.KhungTrongs.Select(x => (x.GioBatDau, x.GioKetThuc)).ToList()
                : [.. Catalogs.KhungTrongMacDinh];

            var caDung = course.LaThucHanh || LaGdtc(course)
                ? CaThucHanhHienTai()
                : CaLyThuyetHienTai();
            var caDuocChon = caHocs.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var day in days)
                foreach (var ca in Catalogs.CaHocSettings.Where(x =>
                             caDung.Contains(x.Code) && caDuocChon.Contains(x.Code)))
                {
                    if (ca.GioBatDau is not { } batDau || ca.GioKetThuc is not { } ketThuc) continue;
                    if (!khung.Any(k => Catalogs.NamTrongKhung(k, batDau, ketThuc))) continue;
                    slots.Add(new Slot(room.MaPhong, day, ca.Code, ca.SoTiet));
                }
        }
        return slots;
    }

    /// <summary>Học phần Giáo dục thể chất (mã bắt đầu bằng GDTC) luôn học ngoài sân thể dục.</summary>
    private static bool LaGdtc(HocPhan course) =>
        course.MaHocPhan.StartsWith("GDTC", StringComparison.OrdinalIgnoreCase);

    private static bool RoomMatches(PhongHoc room, HocPhan course)
    {
        if (course.SoLuongDangKy > room.SucChua) return false;
        if (!Catalogs.LaPhongXepLich(room.LoaiPhong)) return false;

        if (LaGdtc(course))
            return room.LoaiPhong == Catalogs.SanTheDucNgoaiTroi;

        if (!course.LaThucHanh)
            return room.LoaiPhong == Catalogs.RoomTheory;

        if (!Catalogs.LaPhongThucHanh(room.LoaiPhong)) return false;
        return string.IsNullOrWhiteSpace(course.NhomNganh)
            || Catalogs.PhongPhuHopNhomNganh(room.LoaiPhong, course.NhomNganh);
    }

    /// <summary>
    /// CSVC quản lý thiết bị theo từng phòng. Một phòng chỉ đủ điều kiện khi mỗi thiết bị
    /// chuẩn của loại phòng có ít nhất một đơn vị đang ở trạng thái Tốt.
    /// </summary>
    private static bool RoomCoDuThietBiHoatDong(PhongHoc room)
    {
        if (!Catalogs.ThietBiTheoLoaiPhong.TryGetValue(room.LoaiPhong, out var thietBiYeuCau)
            || thietBiYeuCau.Length == 0)
            return true;

        var thietBiTot = room.PhongThietBis
            .Where(x => x.TrangThai == "GOOD" && x.ThietBi is not null)
            .Select(x => x.ThietBi!.TenThietBi)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return thietBiYeuCau.All(thietBiTot.Contains);
    }

    private static string LyDoKhongCoSlot(HocPhan course, List<PhongHoc> rooms)
    {
        var phongDungLoaiVaSucChua = rooms.Where(x => RoomMatches(x, course)).ToList();
        if (phongDungLoaiVaSucChua.Count == 0)
            return "Không có phòng CSVC đang hoạt động phù hợp loại lớp, nhóm ngành và sức chứa.";

        if (!phongDungLoaiVaSucChua.Any(RoomCoDuThietBiHoatDong))
            return "Phòng phù hợp chưa đủ thiết bị chuẩn đang ở trạng thái Tốt theo dữ liệu CSVC.";

        return "Phòng phù hợp không có khung giờ trống hoặc ca học khả dụng trong khoảng ngày đã chọn.";
    }

    private static List<Slot> FeasibleSlots(
        HocPhan course,
        List<Slot> staticSlots,
        Dictionary<(int, DateOnly), List<(TimeOnly Start, TimeOnly End)>> roomBusy,
        Dictionary<(string, DateOnly), List<(TimeOnly Start, TimeOnly End)>> lecturerBusy,
        HashSet<int> sanRoomIds)
    {
        var list = new List<Slot>();
        foreach (var slot in staticSlots)
        {
            if (Catalogs.GetCaTime(slot.CaHoc) is not (var batDau, var ketThuc)) continue;

            // Ràng buộc giảng viên: 1 giảng viên chỉ dạy 1 lớp trong 1 ca.
            if (!string.IsNullOrWhiteSpace(course.MaGiangVien)
                && IsBusy(lecturerBusy, (course.MaGiangVien, slot.Ngay), batDau, ketThuc)) continue;

            var key = (slot.MaPhong, slot.Ngay);
            if (sanRoomIds.Contains(slot.MaPhong))
            {
                // Ràng buộc sân thể dục: dùng chung, tối đa 3 lớp trong cùng khung giờ giao nhau.
                if (SoLanGiao(roomBusy, key, batDau, ketThuc) >= Catalogs.SucChuaSanMoiCa) continue;
            }
            else if (IsBusy(roomBusy, key, batDau, ketThuc)) continue;

            list.Add(slot);
        }
        return list;
    }

    private static bool IsBusy<TKey>(
        Dictionary<TKey, List<(TimeOnly Start, TimeOnly End)>> map,
        TKey key, TimeOnly start, TimeOnly end) where TKey : notnull
        => map.TryGetValue(key, out var list)
           && list.Any(x => Catalogs.IsOverlap(x.Start, x.End, start, end));

    /// <summary>Số lớp đang giữ chỗ giao với khung giờ đang xét (dùng cho sân dùng chung).</summary>
    private static int SoLanGiao<TKey>(
        Dictionary<TKey, List<(TimeOnly Start, TimeOnly End)>> map,
        TKey key, TimeOnly start, TimeOnly end) where TKey : notnull
        => map.TryGetValue(key, out var list)
            ? list.Count(x => Catalogs.IsOverlap(x.Start, x.End, start, end))
            : 0;

    private static void AddBusy<TKey>(
        Dictionary<TKey, List<(TimeOnly Start, TimeOnly End)>> map,
        TKey key, TimeOnly start, TimeOnly end) where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }
        list.Add((start, end));
    }

    private static void Place(
        Dictionary<(int, DateOnly), List<(TimeOnly Start, TimeOnly End)>> roomBusy,
        Dictionary<(string, DateOnly), List<(TimeOnly Start, TimeOnly End)>> lecturerBusy,
        HocPhan course,
        Slot slot)
    {
        if (Catalogs.GetCaTime(slot.CaHoc) is not (var batDau, var ketThuc)) return;

        AddBusy(roomBusy, (slot.MaPhong, slot.Ngay), batDau, ketThuc);
        if (!string.IsNullOrWhiteSpace(course.MaGiangVien))
            AddBusy(lecturerBusy, (course.MaGiangVien, slot.Ngay), batDau, ketThuc);
    }

    private static void Unplace(
        Dictionary<(int, DateOnly), List<(TimeOnly Start, TimeOnly End)>> roomBusy,
        Dictionary<(string, DateOnly), List<(TimeOnly Start, TimeOnly End)>> lecturerBusy,
        HocPhan course,
        Slot slot)
    {
        if (Catalogs.GetCaTime(slot.CaHoc) is not (var batDau, var ketThuc)) return;

        if (roomBusy.TryGetValue((slot.MaPhong, slot.Ngay), out var rb))
            rb.Remove((batDau, ketThuc));
        if (!string.IsNullOrWhiteSpace(course.MaGiangVien)
            && lecturerBusy.TryGetValue((course.MaGiangVien, slot.Ngay), out var lb))
            lb.Remove((batDau, ketThuc));
    }
}
