using System.Globalization;
using System.Text.Json.Serialization;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Models;

/// <summary>
/// Một ca học có thể cấu hình: khung giờ bắt đầu/kết thúc, dải tiết và số tiết.
/// Là đối tượng được nạp/lưu tại App_Data/cau-hinh-ca-hoc.json nên toàn bộ thuộc tính
/// suy diễn bên dưới đều bỏ qua khi tuần tự hóa.
/// </summary>
public class CaHocSetting
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>Giờ bắt đầu dạng HH:mm.</summary>
    public string BatDau { get; set; } = string.Empty;

    /// <summary>Giờ kết thúc dạng HH:mm.</summary>
    public string KetThuc { get; set; } = string.Empty;

    /// <summary>Tiết bắt đầu của ca (1, 4, 7, 10, 13...).</summary>
    public int Tiet { get; set; }

    /// <summary>Số tiết ca chiếm: 3 tiết (lý thuyết) hoặc 5 tiết (thực hành).</summary>
    public int SoTiet { get; set; }

    [JsonIgnore]
    public TimeOnly? GioBatDau => Parse(BatDau);

    [JsonIgnore]
    public TimeOnly? GioKetThuc => Parse(KetThuc);

    [JsonIgnore]
    public string TimeRange =>
        GioBatDau is { } s && GioKetThuc is { } e ? $"{s:HH\\:mm} - {e:HH\\:mm}" : "";

    [JsonIgnore]
    public bool LaThucHanh => SoTiet == 5;

    private static TimeOnly? Parse(string? value) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t
            : null;
}

public static class AppRoles
{
    public const string Student = "ROLE_STUDENT";
    public const string Lecturer = "ROLE_LECTURER";

    /// <summary>Nhân viên cơ sở vật chất: phòng học, tòa nhà, thiết bị, sự cố, duyệt mượn phòng.</summary>
    public const string Facility = "ROLE_FACILITY";

    /// <summary>Nhân viên đào tạo: học phần, thời khóa biểu, xếp lịch, duyệt đơn xin nghỉ.</summary>
    public const string Training = "ROLE_TRAINING";

    /// <summary>Quản trị hệ thống: chỉ quản lý tài khoản/phân quyền và theo dõi nhật ký hoạt động.</summary>
    public const string Admin = "ROLE_ADMIN";

    /// <summary>Mọi vai trò nghiệp vụ (tất cả trừ Quản trị hệ thống) — dùng cho các màn hình dùng chung.</summary>
    public const string BusinessUsers = Student + "," + Lecturer + "," + Facility + "," + Training;

    /// <summary>Toàn bộ người dùng đã đăng nhập, gồm cả Quản trị — dùng cho trang xem thông báo.</summary>
    public const string Everyone = BusinessUsers + "," + Admin;

    /// <summary>Nhóm mở được màn hình Quản trị thông báo: Đào tạo đăng/sửa/xóa bài, Quản trị chỉ theo dõi.</summary>
    public const string ThongBaoAccess = Training + "," + Admin;
}

/// <summary>Nhóm đối tượng được nhận một bản tin thông báo.</summary>
public static class ThongBaoDoiTuong
{
    public const string All = "ALL";
    public const string Student = AppRoles.Student;
    public const string Lecturer = AppRoles.Lecturer;
    public const string Facility = AppRoles.Facility;
    public const string Training = AppRoles.Training;

    public static readonly Dictionary<string, string> Ten = new()
    {
        [All] = "Tất cả người dùng",
        [Student] = "Sinh viên",
        [Lecturer] = "Giảng viên",
        [Facility] = "Nhân viên cơ sở vật chất",
        [Training] = "Nhân viên đào tạo"
    };

    public static string Label(string? code) =>
        code != null && Ten.TryGetValue(code, out var v) ? v : code ?? "";
}

public static class Catalogs
{
    /// <summary>
    /// Bộ khung giờ ca học mặc định (quy chuẩn module 3):
    /// môn lý thuyết dùng khung 3 tiết, môn thực hành/thí nghiệm dùng khung 5 tiết bao trọn 2 ca lý thuyết.
    /// </summary>
    public static CaHocSetting[] MacDinhCaHoc() =>
    [
        new() { Code = "CA_S1", Label = "Sáng 1 (S1)", BatDau = "07:00", KetThuc = "09:30", Tiet = 1, SoTiet = 3 },
        new() { Code = "CA_S2", Label = "Sáng 2 (S2)", BatDau = "09:45", KetThuc = "12:15", Tiet = 4, SoTiet = 3 },
        new() { Code = "CA_C1", Label = "Chiều 1 (C1)", BatDau = "13:00", KetThuc = "15:30", Tiet = 7, SoTiet = 3 },
        new() { Code = "CA_C2", Label = "Chiều 2 (C2)", BatDau = "15:45", KetThuc = "18:15", Tiet = 10, SoTiet = 3 },
        new() { Code = "CA_T1", Label = "Tối (T)", BatDau = "18:30", KetThuc = "21:00", Tiet = 13, SoTiet = 3 },
        new() { Code = "CA_TH_SANG", Label = "Thực hành sáng", BatDau = "07:00", KetThuc = "11:25", Tiet = 1, SoTiet = 5 },
        new() { Code = "CA_TH_CHIEU", Label = "Thực hành chiều", BatDau = "13:00", KetThuc = "17:25", Tiet = 7, SoTiet = 5 }
    ];

    private static CaHocSetting[] _caHocSettings = MacDinhCaHoc();
    private static Dictionary<string, (TimeOnly Start, TimeOnly End)> _caHocTime = XayCaHocTime(_caHocSettings);

    /// <summary>Cấu hình ca học đang áp dụng (nạp từ App_Data/cau-hinh-ca-hoc.json, mặc định theo quy chuẩn).</summary>
    public static IReadOnlyList<CaHocSetting> CaHocSettings => _caHocSettings;

    /// <summary>Danh mục ca học dạng (Mã, Nhãn, Khung giờ) phục vụ hiển thị và chọn ca.</summary>
    public static (string Code, string Label, string Time)[] CaHoc =>
        _caHocSettings.Select(x => (x.Code, x.Label, x.TimeRange)).ToArray();

    /// <summary>Áp dụng một cấu hình ca học mới cho toàn hệ thống (chỉ nhận ca có giờ hợp lệ).</summary>
    public static void ApDungCauHinhCaHoc(IEnumerable<CaHocSetting> items)
    {
        var ds = items.Where(x => !string.IsNullOrWhiteSpace(x.Code) && x.GioBatDau is not null && x.GioKetThuc is not null).ToArray();
        if (ds.Length == 0) return;
        _caHocSettings = ds;
        _caHocTime = XayCaHocTime(ds);
    }

    private static Dictionary<string, (TimeOnly Start, TimeOnly End)> XayCaHocTime(IEnumerable<CaHocSetting> items) =>
        items.Where(x => !string.IsNullOrWhiteSpace(x.Code) && x.GioBatDau is not null && x.GioKetThuc is not null)
             .GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase)
             .ToDictionary(g => g.Key, g => (g.First().GioBatDau!.Value, g.First().GioKetThuc!.Value), StringComparer.OrdinalIgnoreCase);

    public const string RoomTheory = "ROOM_THEORY";
    public const string PhongMayTinh = "PHONG_MAY_TINH";
    public const string LabHoaSinh = "LAB_HOA_SINH";
    public const string LabCntp = "LAB_CNTP";
    public const string LabDienDienTu = "LAB_DIEN_DIEN_TU";
    public const string XuongCoKhi = "XUONG_CO_KHI";
    public const string PhongLuyenThanh = "PHONG_LUYEN_THANH";
    public const string PhongHoaTau = "PHONG_HOA_TAU";
    public const string PhongHoaVienMyThuat = "PHONG_HOA_VIEN_MY_THUAT";
    public const string PhongDoAnKienTruc = "PHONG_DO_AN_KIEN_TRUC";
    public const string SanTheDucNgoaiTroi = "SAN_THE_DUC_NGOAI_TROI";
    public const string PhongChucNang = "PHONG_CHUC_NANG";

    /// <summary>Sân thể dục là không gian dùng chung: cho phép tối đa 3 lớp học trong cùng một ca.</summary>
    public const int SucChuaSanMoiCa = 3;

    /// <summary>Danh mục loại phòng chuẩn hóa theo chức năng đào tạo.</summary>
    public static readonly Dictionary<string, string> LoaiPhong = new()
    {
        [RoomTheory] = "Lý thuyết chung",
        [PhongMayTinh] = "Máy tính – CNTT & Phần mềm",
        [LabHoaSinh] = "Thí nghiệm Hóa – Sinh",
        [LabCntp] = "Thí nghiệm Công nghệ Thực phẩm",
        [LabDienDienTu] = "Thực hành Điện – Điện tử – Tự động hóa",
        [XuongCoKhi] = "Xưởng Cơ khí – Ô tô – Chế tạo",
        [PhongLuyenThanh] = "Phòng luyện thanh",
        [PhongHoaTau] = "Phòng hòa tấu",
        [PhongHoaVienMyThuat] = "Họa viện Mỹ thuật",
        [PhongDoAnKienTruc] = "Phòng đồ án Kiến trúc",
        [SanTheDucNgoaiTroi] = "Sân thể dục ngoài trời",
        [PhongChucNang] = "Phòng chức năng (không xếp lịch)"
    };

    /// <summary>Nhãn các mã loại phòng cũ, giữ để hiển thị dữ liệu trước khi migration chuyển đổi.</summary>
    private static readonly Dictionary<string, string> LoaiPhongCu = new()
    {
        ["ROOM_PRACTICE"] = "Thực hành",
        ["ROOM_LAB"] = "Phòng thí nghiệm",
        ["ROOM_MULTI"] = "Đa phương tiện",
        ["ROOM_SPORT"] = "Thể dục – thể thao"
    };

    /// <summary>Các loại phòng dùng cho học phần thực hành (khung 5 tiết).</summary>
    public static readonly HashSet<string> NhomPhongThucHanh =
    [
        PhongMayTinh, LabHoaSinh, LabCntp, LabDienDienTu, XuongCoKhi,
        PhongLuyenThanh, PhongHoaTau, PhongHoaVienMyThuat, PhongDoAnKienTruc,
        SanTheDucNgoaiTroi
    ];

    public static bool LaPhongThucHanh(string? loaiPhong) =>
        loaiPhong != null && NhomPhongThucHanh.Contains(loaiPhong);

    /// <summary>Phòng chức năng/hành chính không đưa vào xếp lịch và đăng ký mượn.</summary>
    public static bool LaPhongXepLich(string? loaiPhong) =>
        !string.IsNullOrWhiteSpace(loaiPhong) && loaiPhong != PhongChucNang;

    /// <summary>Nhóm ngành dùng để ghép học phần thực hành với đúng họ phòng chuyên ngành.</summary>
    public const string NganhDaiCuong = "NGANH_DAI_CUONG";
    public const string NganhCntt = "NGANH_CNTT";
    public const string NganhHoaSinh = "NGANH_HOA_SINH";
    public const string NganhCntp = "NGANH_CNTP";
    public const string NganhDien = "NGANH_DIEN";
    public const string NganhCoKhi = "NGANH_CO_KHI";
    public const string NganhAmNhac = "NGANH_AM_NHAC";
    public const string NganhKienTruc = "NGANH_KIEN_TRUC";
    public const string NganhTheThao = "NGANH_THE_THAO";

    public static readonly Dictionary<string, string> NhomNganh = new()
    {
        [NganhDaiCuong] = "Đại cương – Lý thuyết",
        [NganhCntt] = "CNTT & Phần mềm",
        [NganhHoaSinh] = "Hóa – Sinh",
        [NganhCntp] = "Công nghệ Thực phẩm",
        [NganhDien] = "Điện – Điện tử – Tự động hóa",
        [NganhCoKhi] = "Cơ khí – Ô tô – Chế tạo",
        [NganhAmNhac] = "Âm nhạc & Nghệ thuật biểu diễn",
        [NganhKienTruc] = "Kiến trúc & Mỹ thuật",
        [NganhTheThao] = "Giáo dục thể chất"
    };

    /// <summary>Loại phòng mà một nhóm ngành được phép sử dụng khi xếp lịch thực hành.</summary>
    public static readonly Dictionary<string, string[]> LoaiPhongTheoNhomNganh = new()
    {
        [NganhDaiCuong] = [RoomTheory],
        [NganhCntt] = [PhongMayTinh],
        [NganhHoaSinh] = [LabHoaSinh],
        [NganhCntp] = [LabCntp],
        [NganhDien] = [LabDienDienTu],
        [NganhCoKhi] = [XuongCoKhi],
        [NganhAmNhac] = [PhongLuyenThanh, PhongHoaTau],
        [NganhKienTruc] = [PhongHoaVienMyThuat, PhongDoAnKienTruc],
        [NganhTheThao] = [SanTheDucNgoaiTroi]
    };

    public static string LabelNhomNganh(string? code) =>
        code != null && NhomNganh.TryGetValue(code, out var v) ? v : code ?? "";

    /// <summary>Học phần (theo nhóm ngành) có được xếp vào loại phòng này không.</summary>
    public static bool PhongPhuHopNhomNganh(string? loaiPhong, string? nhomNganh)
    {
        if (string.IsNullOrWhiteSpace(loaiPhong)) return false;
        if (string.IsNullOrWhiteSpace(nhomNganh)) return true;
        return LoaiPhongTheoNhomNganh.TryGetValue(nhomNganh, out var ds) && ds.Contains(loaiPhong);
    }

    /// <summary>Trạng thái lưu trong PhongHoc.trang_thai_phong. "Đang có lớp" không lưu ở đây mà suy ra từ LichHoc/DatPhong.</summary>
    public static readonly Dictionary<string, string> TrangThaiPhong = new()
    {
        ["AVAILABLE"] = "Sẵn sàng",
        ["IN_USE"] = "Đang sử dụng",
        ["MAINTENANCE"] = "Bảo trì",
        ["UNAVAILABLE"] = "Không khả dụng"
    };

    /// <summary>Trạng thái suy ra của một phòng tại một ngày + ca cụ thể, dùng cho màn hình mượn phòng.</summary>
    public const string PhongTrong = "FREE";
    public const string PhongBan = "BUSY";
    public const string PhongBaoTri = "OFFLINE";

    public static readonly Dictionary<string, string> TrangThaiMuonPhong = new()
    {
        [PhongTrong] = "Trống",
        [PhongBan] = "Đang có lớp",
        [PhongBaoTri] = "Bảo trì"
    };

    /// <summary>Trạng thái hoạt động của tòa nhà. Tòa ngừng hoạt động/bảo trì làm toàn bộ phòng bên trong offline.</summary>
    public static readonly Dictionary<string, string> TrangThaiToaNha = new()
    {
        ["ACTIVE"] = "Đang hoạt động",
        ["MAINTENANCE"] = "Đang bảo trì",
        ["INACTIVE"] = "Ngừng hoạt động"
    };

    public static string LabelTrangThaiToaNha(string? code) =>
        code != null && TrangThaiToaNha.TryGetValue(code, out var v) ? v : code ?? "";

    public static bool ToaNhaDangHoatDong(ToaNha? toaNha) =>
        toaNha is null || toaNha.TrangThaiHoatDong == "ACTIVE";

    public static readonly Dictionary<string, string> TrangThaiDatPhong = new()
    {
        ["PENDING"] = "Chờ duyệt",
        ["APPROVED"] = "Đã duyệt",
        ["REJECTED"] = "Từ chối",
        ["CANCELLED"] = "Đã hủy"
    };

    public static readonly Dictionary<string, string> TrangThaiThietBi = new()
    {
        ["GOOD"] = "Tốt",
        ["NEEDS_REPAIR"] = "Cần sửa chữa",
        ["REPAIRING"] = "Đang sửa chữa",
        ["DAMAGED"] = "Hỏng",
        ["LOST"] = "Mất",
        ["RETIRED"] = "Đã thanh lý"
    };

    /// <summary>Thiết bị cần CSVC xử lý (mọi trạng thái trừ Tốt và Đã thanh lý).</summary>
    public static bool ThietBiCanXuLy(string? code) =>
        code is not null && code != "GOOD" && code != "RETIRED";

    /// <summary>Thiết bị chuẩn gán mặc định cho từng loại phòng; CSVC sửa lại theo thực tế.</summary>
    public static readonly Dictionary<string, string[]> ThietBiTheoLoaiPhong = new()
    {
        [RoomTheory] =
        [
            "Máy chiếu Projector", "Màn chiếu", "Bảng viết", "Hệ thống Loa/Micro",
            "Điều hòa", "Bàn ghế sinh viên", "Bàn giáo viên"
        ],
        [PhongMayTinh] =
        [
            "Máy tính để bàn", "Màn hình", "Bàn phím & chuột", "Máy chiếu Projector",
            "Điều hòa", "Bàn máy tính", "Ghế xoay", "Switch mạng"
        ],
        [LabHoaSinh] =
        [
            "Tủ hút khí độc (Fume Hood)", "Bồn rửa hóa chất", "Kính hiển vi quang học",
            "Cân phân tích", "Máy đo pH", "Bộ chưng cất", "Tủ an toàn sinh học",
            "Hệ thống thông gió"
        ],
        [LabCntp] =
        [
            "Tủ lên men", "Nồi hấp tiệt trùng", "Tủ sấy", "Máy đo độ ẩm",
            "Cân phân tích", "Kính hiển vi quang học", "Bồn rửa", "Tủ hút khí độc (Fume Hood)"
        ],
        [LabDienDienTu] =
        [
            "Bàn thực hành mạch điện", "Bộ vi điều khiển", "PLC", "Máy hiện sóng Oscilloscope",
            "Mỏ hàn", "Đồng hồ vạn năng", "Nguồn DC", "Bộ linh kiện điện tử"
        ],
        [XuongCoKhi] =
        [
            "Máy tiện", "Máy phay", "Máy CNC", "Mô hình động cơ ô tô",
            "Cầu nâng ô tô", "Máy khoan", "Bộ dụng cụ cầm tay", "Hệ thống khí nén"
        ],
        [PhongLuyenThanh] =
        [
            "Đàn Piano", "Micro", "Loa kiểm âm", "Gương soi toàn thân",
            "Tấm tiêu âm", "Sàn gỗ chuyên dụng", "Bộ gõ"
        ],
        [PhongHoaTau] =
        [
            "Đàn Piano", "Bộ gõ", "Micro", "Loa kiểm âm",
            "Tấm tiêu âm", "Sàn gỗ chuyên dụng"
        ],
        [PhongHoaVienMyThuat] =
        [
            "Giá vẽ (Easel)", "Bàn vẽ A0/A1", "Tượng thạch cao mẫu", "Đèn chiếu điểm Spotlight",
            "Bồn rửa cọ"
        ],
        [PhongDoAnKienTruc] =
        [
            "Bàn đồ án A0", "Giá vẽ (Easel)", "Máy in 3D", "Máy cắt Laser",
            "Đèn chiếu điểm Spotlight", "Tủ mô hình"
        ],
        [SanTheDucNgoaiTroi] =
        [
            "Đèn chiếu sáng sân", "Cột bóng rổ", "Khung thành bóng đá",
            "Lưới chắn bóng", "Hệ thống thoát nước sân"
        ],
        [PhongChucNang] =
        [
            "Bàn làm việc", "Máy tính văn phòng", "Máy in", "Điều hòa", "Tủ hồ sơ"
        ]
    };

    /// <summary>Loại sự cố đặc thù theo từng loại phòng, dùng lọc danh sách khi báo cáo.</summary>
    public static readonly Dictionary<string, string[]> SuCoTheoLoaiPhong = new()
    {
        [RoomTheory] = ["SU_CO_TV_CHIEU", "SU_CO_AM_THANH", "SU_CO_DIEU_HOA_DEN", "SU_CO_BAN_GHE"],
        [PhongMayTinh] = ["SU_CO_MAY_TINH", "SU_CO_CHUOT_BAN_PHIM", "SU_CO_DIEU_HOA_DEN", "SU_CO_BAN_GHE"],
        [LabHoaSinh] = ["SU_CO_TB_THI_NGHIEM", "SU_CO_DUNG_CU_THUY_TINH", "SU_CO_HOA_CHAT", "SU_CO_TU_HUT", "SU_CO_CAP_THOAT_NUOC"],
        [LabCntp] = ["SU_CO_TB_THI_NGHIEM", "SU_CO_DUNG_CU_THUY_TINH", "SU_CO_HOA_CHAT", "SU_CO_TU_HUT", "SU_CO_CAP_THOAT_NUOC"],
        [LabDienDienTu] = ["SU_CO_NGUON_DIEN", "SU_CO_THIET_BI_DO", "SU_CO_MAY_CONG_CU", "SU_CO_KHI_NEN"],
        [XuongCoKhi] = ["SU_CO_MAY_CONG_CU", "SU_CO_NGUON_DIEN", "SU_CO_KHI_NEN", "SU_CO_THIET_BI_DO"],
        [PhongLuyenThanh] = ["SU_CO_NHAC_CU", "SU_CO_AM_THANH", "SU_CO_CACH_AM", "SU_CO_GUONG_SAN"],
        [PhongHoaTau] = ["SU_CO_NHAC_CU", "SU_CO_AM_THANH", "SU_CO_CACH_AM"],
        [PhongHoaVienMyThuat] = ["SU_CO_GIA_VE", "SU_CO_TUONG_MAU", "SU_CO_DEN_SPOTLIGHT", "SU_CO_BON_RUA"],
        [PhongDoAnKienTruc] = ["SU_CO_GIA_VE", "SU_CO_MAY_IN_3D_LASER", "SU_CO_DEN_SPOTLIGHT", "SU_CO_TUONG_MAU"],
        [SanTheDucNgoaiTroi] = ["SU_CO_SAN_NGAP", "SU_CO_DEN_SAN", "SU_CO_DUNG_CU_THE_THAO"],
        [PhongChucNang] = ["SU_CO_DIEU_HOA_DEN", "SU_CO_MAY_TINH", "SU_CO_BAN_GHE"]
    };

    /// <summary>Mọi loại sự cố dùng chung cho mọi phòng.</summary>
    public static readonly string[] SuCoChung = ["SU_CO_KHAC"];

    /// <summary>
    /// Từ khóa tên thiết bị có khả năng gặp từng loại sự cố, dùng thu hẹp danh sách
    /// "Thiết bị trong phòng" khi người dùng chọn loại sự cố. Mảng rỗng = không lọc.
    /// </summary>
    public static readonly Dictionary<string, string[]> ThietBiTheoLoaiSuCo = new()
    {
        ["SU_CO_TV_CHIEU"] = ["máy chiếu", "màn chiếu", "màn hình", "tivi", "tv", "bảng thông minh", "bảng tương tác"],
        ["SU_CO_AM_THANH"] = ["micro", "loa", "âm thanh"],
        ["SU_CO_DIEU_HOA_DEN"] = ["điều hòa", "máy lạnh", "đèn", "quạt", "rèm"],
        ["SU_CO_BAN_GHE"] = ["bàn ghế", "bàn giáo viên", "bàn sinh viên", "bàn học", "ghế"],
        ["SU_CO_MAY_TINH"] = ["máy tính", "workstation", "switch", "máy in", "server"],
        ["SU_CO_CHUOT_BAN_PHIM"] = ["chuột", "bàn phím"],
        ["SU_CO_TB_THI_NGHIEM"] =
        [
            "kính hiển vi", "cân", "máy đo", "tủ sấy", "nồi hấp", "tủ lên men",
            "bộ chưng cất", "tủ an toàn sinh học"
        ],
        ["SU_CO_DUNG_CU_THUY_TINH"] = ["ống nghiệm", "dụng cụ", "khay"],
        ["SU_CO_HOA_CHAT"] = ["hóa chất", "khí"],
        ["SU_CO_TU_HUT"] = ["tủ hút", "thông gió"],
        ["SU_CO_CAP_THOAT_NUOC"] = ["bồn rửa", "thoát nước", "van", "cấp nước"],
        ["SU_CO_NGUON_DIEN"] = ["nguồn", "điện", "cầu chì", "cb"],
        ["SU_CO_THIET_BI_DO"] = ["oscilloscope", "hiện sóng", "vạn năng", "đồng hồ", "máy đo"],
        ["SU_CO_MAY_CONG_CU"] = ["máy tiện", "máy phay", "cnc", "mỏ hàn", "máy khoan", "máy hàn"],
        ["SU_CO_KHI_NEN"] = ["khí nén", "khí"],
        ["SU_CO_NHAC_CU"] = ["đàn", "piano", "bộ gõ"],
        ["SU_CO_CACH_AM"] = ["tiêu âm", "cách âm", "cửa"],
        ["SU_CO_GUONG_SAN"] = ["gương", "sàn"],
        ["SU_CO_GIA_VE"] = ["giá vẽ", "bàn vẽ", "bàn đồ án"],
        ["SU_CO_TUONG_MAU"] = ["tượng"],
        ["SU_CO_MAY_IN_3D_LASER"] = ["in 3d", "cắt laser", "laser"],
        ["SU_CO_DEN_SPOTLIGHT"] = ["spotlight", "đèn chiếu điểm"],
        ["SU_CO_BON_RUA"] = ["bồn rửa"],
        ["SU_CO_SAN_NGAP"] = ["thoát nước", "sân"],
        ["SU_CO_DEN_SAN"] = ["đèn"],
        ["SU_CO_DUNG_CU_THE_THAO"] = ["bóng", "khung thành", "cột", "lưới", "bảng điểm", "dụng cụ"],
        ["SU_CO_KHAC"] = []
    };

    /// <summary>Thiết bị có tên khớp từ khóa của loại sự cố; loại sự cố không có từ khóa thì giữ toàn bộ.</summary>
    public static bool ThietBiKhopLoaiSuCo(string? tenThietBi, string? loaiSuCo)
    {
        if (string.IsNullOrWhiteSpace(loaiSuCo)) return true;
        if (!ThietBiTheoLoaiSuCo.TryGetValue(loaiSuCo, out var tuKhoa) || tuKhoa.Length == 0) return true;
        if (string.IsNullOrWhiteSpace(tenThietBi)) return false;

        var ten = tenThietBi.ToLowerInvariant();
        return tuKhoa.Any(k => ten.Contains(k, StringComparison.Ordinal));
    }

    /// <summary>Sự cố hạ tầng phòng: mức cao sẽ chuyển phòng sang bảo trì.</summary>
    public static readonly HashSet<string> SuCoHaTang =
    [
        "SU_CO_DIEU_HOA_DEN", "SU_CO_CAP_THOAT_NUOC", "SU_CO_NGUON_DIEN", "SU_CO_TU_HUT",
        "SU_CO_CACH_AM", "SU_CO_SAN_NGAP", "SU_CO_DEN_SAN", "SU_CO_KHI_NEN",
        "LOAI_PHONG", "LOAI_CO_SO"
    ];

    public static readonly Dictionary<string, string> LoaiSuCo = new()
    {
        ["SU_CO_TV_CHIEU"] = "Hư TV / màn hình chiếu",
        ["SU_CO_AM_THANH"] = "Hư micro / loa",
        ["SU_CO_DIEU_HOA_DEN"] = "Hỏng máy lạnh / đèn",
        ["SU_CO_BAN_GHE"] = "Thiếu / hỏng bàn ghế",
        ["SU_CO_MAY_TINH"] = "Hư máy tính",
        ["SU_CO_CHUOT_BAN_PHIM"] = "Hư chuột / bàn phím",
        ["SU_CO_TB_THI_NGHIEM"] = "Trục trặc máy móc chuyên ngành (kính hiển vi, cân phân tích, máy đo…)",
        ["SU_CO_DUNG_CU_THUY_TINH"] = "Thiếu / hư dụng cụ thủy tinh",
        ["SU_CO_HOA_CHAT"] = "Thiếu / chậm cung cấp hóa chất, vật tư",
        ["SU_CO_TU_HUT"] = "Hỏng tủ hút khí độc / hệ thống thông gió",
        ["SU_CO_CAP_THOAT_NUOC"] = "Nghẹt / rò rỉ cấp thoát nước, van khí",
        ["SU_CO_NGUON_DIEN"] = "Hỏng nguồn tổng / cầu chì / CB chống giật",
        ["SU_CO_THIET_BI_DO"] = "Hỏng thiết bị đo (Oscilloscope, đồng hồ vạn năng…)",
        ["SU_CO_MAY_CONG_CU"] = "Hỏng máy tiện / phay / CNC / mỏ hàn",
        ["SU_CO_KHI_NEN"] = "Rò rỉ khí nén / dầu mỡ",
        ["SU_CO_NHAC_CU"] = "Hỏng đàn Piano / nhạc cụ",
        ["SU_CO_CACH_AM"] = "Bong tróc tiêu âm / hỏng cách âm cửa",
        ["SU_CO_GUONG_SAN"] = "Hỏng gương / trầy xước sàn",
        ["SU_CO_GIA_VE"] = "Gãy giá vẽ / bàn đồ án",
        ["SU_CO_TUONG_MAU"] = "Sứt mẻ tượng thạch cao mẫu",
        ["SU_CO_MAY_IN_3D_LASER"] = "Hỏng máy cắt Laser / máy in 3D",
        ["SU_CO_DEN_SPOTLIGHT"] = "Hỏng đèn chiếu điểm Spotlight",
        ["SU_CO_BON_RUA"] = "Nghẹt bồn rửa cọ / màu",
        ["SU_CO_SAN_NGAP"] = "Sân bị ngập nước",
        ["SU_CO_DEN_SAN"] = "Hỏng đèn chiếu sáng sân",
        ["SU_CO_DUNG_CU_THE_THAO"] = "Hỏng dụng cụ tập luyện",
        ["SU_CO_KHAC"] = "Cơ sở vật chất khác"
    };

    /// <summary>Nhãn loại sự cố cũ, giữ để hiển thị các báo cáo đã tạo trước khi chuẩn hóa.</summary>
    private static readonly Dictionary<string, string> LoaiSuCoCu = new()
    {
        ["LOAI_THIET_BI"] = "Thiết bị",
        ["LOAI_PHONG"] = "Phòng / hạ tầng",
        ["LOAI_CO_SO"] = "Cơ sở vật chất khác"
    };

    public static string LabelLoaiSuCo(string? code) =>
        code == null ? "" :
        LoaiSuCo.TryGetValue(code, out var v) ? v :
        LoaiSuCoCu.TryGetValue(code, out var cu) ? cu : code;

    public static readonly Dictionary<string, string> MucDoSuCo = new()
    {
        ["LOW"] = "Thấp",
        ["MEDIUM"] = "Bình thường",
        ["HIGH"] = "Cao",
        ["CRITICAL"] = "Nghiêm trọng"
    };

    public static readonly Dictionary<string, string> TrangThaiSuCo = new()
    {
        ["NEW"] = "Mới tạo",
        ["IN_PROGRESS"] = "Đang xử lý",
        ["RESOLVED"] = "Đã giải quyết"
    };

    /// <summary>Kết quả CSVC gửi lại người báo cáo sau khi xử lý.</summary>
    public static readonly Dictionary<string, string> KetQuaSuCo = new()
    {
        ["RESTORED"] = "Đã phục hồi",
        ["TRANSFERRED"] = "Điều chuyển phòng"
    };

    public static readonly Dictionary<string, string> TrangThaiYeuCauSuaChua = new()
    {
        ["MOI"] = "Mới ghi nhận",
        ["DANG_XU_LY"] = "Đang xử lý",
        ["HOAN_THANH"] = "Hoàn thành",
        ["HUY"] = "Đã hủy"
    };

    public static string Label(Dictionary<string, string> bang, string? code) =>
        code != null && bang.TryGetValue(code, out var v) ? v : code ?? "";

    public static readonly Dictionary<string, string> PhuongAnNghi = new()
    {
        ["MAKEUP_CLASS"] = "Dạy bù",
        ["SUBSTITUTE_TEACHER"] = "Giảng viên dạy thay"
    };

    public static string LabelPhuongAn(string? code) =>
        code != null && PhuongAnNghi.TryGetValue(code, out var v) ? v : code ?? "";

    /// <summary>
    /// Khung giờ bắt đầu / kết thúc của từng ca học (lấy từ cấu hình đang áp dụng).
    /// Ca thực hành (5 tiết) bao phủ trọn 2 ca lý thuyết (3 tiết) nên khi so trùng
    /// phải so theo khoảng thời gian chứ không so theo mã ca.
    /// </summary>
    public static IReadOnlyDictionary<string, (TimeOnly Start, TimeOnly End)> CaHocTime => _caHocTime;

    /// <summary>Ca học lý thuyết (đúng 3 tiết) đang áp dụng.</summary>
    public static CaHocSetting[] CaLyThuyet => _caHocSettings.Where(x => x.SoTiet == 3).ToArray();

    /// <summary>Ca học thực hành / thí nghiệm (đúng 5 tiết) đang áp dụng.</summary>
    public static CaHocSetting[] CaThucHanh => _caHocSettings.Where(x => x.SoTiet == 5).ToArray();

    public static (TimeOnly Start, TimeOnly End)? GetCaTime(string? code) =>
        code != null && CaHocTime.TryGetValue(code, out var t) ? t : null;

    /// <summary>
    /// Khung giờ trống mặc định của phòng đang hoạt động: sáng, chiều, tối.
    /// Phòng chưa được CSVC khai khung giờ nào thì màn hình mượn phòng dùng bộ giờ này.
    /// Khung phải phủ trọn cả ca lý thuyết 3 tiết lẫn ca thực hành 5 tiết.
    /// </summary>
    public static readonly (TimeOnly Start, TimeOnly End)[] KhungTrongMacDinh =
    [
        (new TimeOnly(7, 0), new TimeOnly(12, 15)),
        (new TimeOnly(13, 0), new TimeOnly(18, 15)),
        (new TimeOnly(18, 30), new TimeOnly(21, 0))
    ];

    public static string MoTaKhoang(TimeOnly start, TimeOnly end) => $"{start:HH\\:mm} - {end:HH\\:mm}";

    /// <summary>
    /// Đọc giờ người dùng nhập: chấp nhận 17:00, 17h00, 17h, 1700, 17, 5h chiều, 5 giờ tối.
    /// Sai định dạng hoặc ngoài 00:00–23:59 trả về null.
    /// </summary>
    public static TimeOnly? ParseGio(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var s = new string(value.Trim().ToLowerInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray());
        var buoiChieu = s.Contains("chieu") || s.Contains("chiều") || s.Contains("toi") || s.Contains("tối");
        s = s.Replace("chiều", "").Replace("chieu", "").Replace("tối", "").Replace("toi", "")
             .Replace("sáng", "").Replace("sang", "").Replace("giờ", "")
             .Replace('h', ':').Replace('g', ':').Replace('.', ':');

        var phan = s.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (phan.Length == 0) return null;

        int gio, phut = 0;
        if (phan.Length == 1)
        {
            if (!phan[0].All(char.IsDigit)) return null;
            var so = phan[0];
            if (so.Length <= 2)
            {
                gio = int.Parse(so);
            }
            else if (so.Length is 3 or 4)
            {
                so = so.PadLeft(4, '0');
                gio = int.Parse(so[..2]);
                phut = int.Parse(so[2..]);
            }
            else return null;
        }
        else
        {
            if (!phan[0].All(char.IsDigit) || !phan[1].All(char.IsDigit)) return null;
            gio = int.Parse(phan[0]);
            phut = int.Parse(phan[1]);
        }

        if (buoiChieu && gio is >= 1 and <= 11) gio += 12;
        if (gio is < 0 or > 23 || phut is < 0 or > 59) return null;
        return new TimeOnly(gio, phut);
    }

    /// <summary>
    /// Cắt các khoảng đã bị giữ (lịch học, phiếu mượn) ra khỏi các khung giờ trống,
    /// trả về đúng phần còn trống theo thứ tự thời gian.
    /// </summary>
    public static List<(TimeOnly Start, TimeOnly End)> ConKhungTrong(
        IEnumerable<(TimeOnly Start, TimeOnly End)> khungTrong,
        IEnumerable<(TimeOnly Start, TimeOnly End)> daGi)
    {
        var gi = daGi.Select(x => (Start: x.Start.ToTimeSpan(), End: x.End.ToTimeSpan()))
                     .OrderBy(x => x.Start).ToList();
        var ketQua = new List<(TimeOnly Start, TimeOnly End)>();

        foreach (var (batDau, ketThuc) in khungTrong
                     .Select(x => (Start: x.Start.ToTimeSpan(), End: x.End.ToTimeSpan()))
                     .OrderBy(x => x.Start))
        {
            var hienTai = batDau;
            foreach (var g in gi)
            {
                if (g.End <= hienTai) continue;
                if (g.Start >= ketThuc) break;
                if (g.Start > hienTai)
                    ketQua.Add((TimeOnly.FromTimeSpan(hienTai), TimeOnly.FromTimeSpan(g.Start)));
                hienTai = g.End > hienTai ? g.End : hienTai;
                if (hienTai >= ketThuc) break;
            }
            if (hienTai < ketThuc)
                ketQua.Add((TimeOnly.FromTimeSpan(hienTai), TimeOnly.FromTimeSpan(ketThuc)));
        }

        return ketQua;
    }

    /// <summary>Khung xin mượn có nằm trọn trong một khung giờ trống nào đó không.</summary>
    public static bool NamTrongKhung((TimeOnly Start, TimeOnly End) khung, TimeOnly tu, TimeOnly den)
        => khung.Start <= tu && den <= khung.End;

    /// <summary>
    /// Công thức kiểm tra giao nhau khoảng thời gian: StartA &lt; EndB AND StartB &lt; EndA.
    /// </summary>
    public static bool IsOverlap(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB)
        => startA < endB && startB < endA;

    /// <summary>Kiểm tra giao nhau giữa 2 mã ca học dựa trên khung giờ thực tế.</summary>
    public static bool IsOverlap(string? caA, string? caB)
    {
        var a = GetCaTime(caA);
        var b = GetCaTime(caB);
        if (a is null || b is null) return false;
        return IsOverlap(a.Value.Start, a.Value.End, b.Value.Start, b.Value.End);
    }

    public static string CaTimeRange(string? code)
    {
        var t = GetCaTime(code);
        return t is null ? "" : $"{t.Value.Start:HH\\:mm} - {t.Value.End:HH\\:mm}";
    }

    public static string LabelCa(string? code) =>
        CaHoc.FirstOrDefault(x => x.Code == code).Label ?? code ?? "";

    public static string LabelLoaiPhong(string? code) =>
        code == null ? "" :
        LoaiPhong.TryGetValue(code, out var v) ? v :
        LoaiPhongCu.TryGetValue(code, out var cu) ? cu : code;

    public static string LabelTrangThaiPhong(string? code) =>
        code != null && TrangThaiPhong.TryGetValue(code, out var v) ? v : code ?? "";
}

/// <summary>Nhãn tiếng Việt cho mã hành động / đối tượng / dữ liệu trong nhật ký hệ thống.</summary>
public static class NhatKyCatalog
{
    public static readonly Dictionary<string, string> HanhDong = new()
    {
        ["ACTION_LOGIN"] = "Đăng nhập",
        ["ACTION_LOGOUT"] = "Đăng xuất",
        ["ACTION_CREATE"] = "Tạo mới",
        ["ACTION_UPDATE"] = "Cập nhật",
        ["ACTION_DELETE"] = "Xóa",
        ["ACTION_APPROVE"] = "Duyệt",
        ["ACTION_REJECT"] = "Từ chối",
        ["ACTION_CANCEL"] = "Hủy",
        ["ACTION_RESET"] = "Đặt lại mật khẩu",
        ["ACTION_IMPORT"] = "Nhập dữ liệu",
        ["ACTION_AUTO_SCHEDULE"] = "Xếp lịch tự động"
    };

    public static readonly Dictionary<string, string> DoiTuong = new()
    {
        ["NguoiDung"] = "Người dùng",
        ["VaiTro"] = "Vai trò",
        ["PhanQuyen"] = "Phân quyền người dùng",
        ["Phân quyền người dùng"] = "Phân quyền người dùng",
        ["DatPhong"] = "Phiếu mượn phòng",
        ["DonXinNghi"] = "Đơn xin nghỉ",
        ["HocPhan"] = "Học phần",
        ["LichHoc"] = "Lịch học",
        ["PhongHoc"] = "Phòng học",
        ["Phong_ThietBi"] = "Thiết bị phòng học",
        ["ThietBi"] = "Thiết bị",
        ["ToaNha"] = "Tòa nhà",
        ["SuCo"] = "Sự cố",
        ["ThongBao"] = "Thông báo"
    };

    public static string LabelHanhDong(string? code) =>
        code != null && HanhDong.TryGetValue(code, out var v) ? v : code ?? "";

    public static string LabelDoiTuong(string? code) =>
        code != null && DoiTuong.TryGetValue(code, out var v) ? v : code ?? "";

    /// <summary>
    /// Dịch giá trị cũ / mới trong nhật ký sang tiếng Việt: trạng thái, trạng thái khóa,
    /// mã vai trò và các dấu phân cách kỹ thuật (; |) do tầng nghiệp vụ ghi vào.
    /// </summary>
    public static string ChiTiet(string? raw, IReadOnlyDictionary<int, string>? tenVaiTro = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "—";

        var value = DichTuKhoa(raw.Trim());
        if (value.Contains(':'))
        {
            var parts = value
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part =>
                {
                    var idx = part.IndexOf(':');
                    if (idx < 0) return DichGiaTri(part, tenVaiTro);

                    var khoa = part[..idx].Trim();
                    var giaTri = part[(idx + 1)..].Trim();
                    return khoa switch
                    {
                        "Khoa" => $"Trạng thái khóa: {DichGiaTri(giaTri, tenVaiTro)}",
                        "VaiTro" => $"Vai trò: {DichVaiTro(giaTri, tenVaiTro)}",
                        _ => $"{khoa}: {DichGiaTri(giaTri, tenVaiTro)}"
                    };
                });
            return string.Join(" · ", parts);
        }

        if (value.Contains('|'))
            return string.Join(" · ", value
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => DichGiaTri(x, tenVaiTro)));

        return DichGiaTri(value, tenVaiTro);
    }

    /// <summary>Chuẩn hoá vài cụm tiếng Anh còn sót trong dữ liệu nhật ký cũ.</summary>
    private static string DichTuKhoa(string value) => value
        .Replace("Admin đăng nhập", "Quản trị viên đăng nhập")
        .Replace("Admin đăng xuất", "Quản trị viên đăng xuất")
        .Replace("Reset mật khẩu", "Đặt lại mật khẩu");

    private static string DichVaiTro(string giaTri, IReadOnlyDictionary<int, string>? tenVaiTro) =>
        string.Join(", ", giaTri
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => tenVaiTro is not null && int.TryParse(x, out var id) && tenVaiTro.TryGetValue(id, out var ten)
                ? ten
                : x));

    private static string DichGiaTri(string value, IReadOnlyDictionary<int, string>? tenVaiTro)
    {
        if (value.Equals("True", StringComparison.OrdinalIgnoreCase)) return "Có";
        if (value.Equals("False", StringComparison.OrdinalIgnoreCase)) return "Không";
        if (value == "FAIL") return "Thất bại";
        if (tenVaiTro is not null && int.TryParse(value, out var id) && tenVaiTro.TryGetValue(id, out var ten))
            return ten;
        return NhanTrangThai(value) ?? value;
    }

    private static string? NhanTrangThai(string code)
    {
        foreach (var bang in new[]
                 {
                     Catalogs.TrangThaiDatPhong, Catalogs.TrangThaiSuCo, Catalogs.TrangThaiThietBi,
                     Catalogs.TrangThaiPhong, Catalogs.LoaiPhong, Catalogs.PhuongAnNghi,
                     Catalogs.TrangThaiToaNha
                 })
        {
            if (bang.TryGetValue(code, out var v)) return v;
        }

        return Catalogs.CaHocTime.ContainsKey(code) ? Catalogs.LabelCa(code) : null;
    }
}
