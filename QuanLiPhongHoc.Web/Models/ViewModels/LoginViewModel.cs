using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QuanLiPhongHoc.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Nhập mã TDMU ID")]
    [Display(Name = "Mã số")]
    public string MaNguoiDung { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;

    public bool GhiNho { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Nhập email hoặc mã tài khoản")]
    [StringLength(100, ErrorMessage = "Thông tin tối đa 100 ký tự")]
    [Display(Name = "Email hoặc mã tài khoản")]
    public string EmailHoacMa { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 100 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập lại mật khẩu mới")]
    [DataType(DataType.Password)]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string XacNhanMatKhau { get; set; } = string.Empty;
}

/// <summary>Biểu mẫu đổi mật khẩu của người đang đăng nhập (mở từ menu tài khoản trên thanh đầu trang).</summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string MatKhauHienTai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 100 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập lại mật khẩu mới")]
    [DataType(DataType.Password)]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string XacNhanMatKhau { get; set; } = string.Empty;
}

public class PhongHocFormVm
{
    public int MaPhong { get; set; }
    [Required(ErrorMessage = "Nhập mã phòng")] public string CodePhong { get; set; } = string.Empty;
    public int? MaToaNha { get; set; }
    [Required(ErrorMessage = "Chọn loại phòng")] public string LoaiPhong { get; set; } = "ROOM_THEORY";
    [Range(1, 500, ErrorMessage = "Sức chứa từ 1 đến 500 chỗ")] public int SucChua { get; set; } = 40;
    public string TrangThaiPhong { get; set; } = "AVAILABLE";

    /// <summary>Bật để giảng viên/sinh viên thấy phòng trên trang đăng ký mượn.</summary>
    public bool HienThiDangKyMuon { get; set; } = true;

    /// <summary>Các khung giờ phòng trống do CSVC khai, tùy ý thêm/bỏ dòng.</summary>
    public List<KhaiKhungVm> KhungTrong { get; set; } = [];
}

/// <summary>Một dòng nhập khung giờ trống của phòng (không gắn với ca học).</summary>
public class KhaiKhungVm
{
    public string Tu { get; set; } = string.Empty;
    public string Den { get; set; } = string.Empty;
}

public class LichHocFormVm
{
    public int MaLichHoc { get; set; }
    [Required(ErrorMessage = "Chọn học phần")] public string MaHocPhan { get; set; } = string.Empty;
    [Required(ErrorMessage = "Chọn phòng học")] public int? MaPhong { get; set; }
    [Required(ErrorMessage = "Chọn ngày học")] public DateOnly NgayHoc { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required(ErrorMessage = "Chọn ca học")] public string CaHoc { get; set; } = "CA_S1";
    public string? MaGiangVienDayThay { get; set; }
    [StringLength(500, ErrorMessage = "Ghi chú điều phối tối đa 500 ký tự")]
    public string? GhiChuDieuPhoi { get; set; }
    public bool TrangThaiHoatDong { get; set; } = true;
}

public class DatPhongFormVm
{
    [Required(ErrorMessage = "Chưa chọn phòng mượn")]
    [Display(Name = "Phòng mượn")]
    public int MaPhong { get; set; }

    [Required(ErrorMessage = "Chưa chọn ngày mượn")]
    [Display(Name = "Ngày mượn phòng")]
    public DateOnly NgayDat { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Chưa chọn giờ bắt đầu mượn")]
    [Display(Name = "Bắt đầu")]
    public string Tu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chưa chọn giờ kết thúc mượn")]
    [Display(Name = "Kết thúc")]
    public string Den { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mục đích sử dụng phòng")]
    [StringLength(500, ErrorMessage = "Mục đích tối đa 500 ký tự")]
    [Display(Name = "Mục đích mượn phòng")]
    public string? MucDich { get; set; }

    [Range(1, 500, ErrorMessage = "Số người dự kiến từ 1 đến 500")]
    [Display(Name = "Số lượng người dự kiến")]
    public int? SoLuongNguoiThamGia { get; set; }

    [Display(Name = "Cam kết bảo quản tài sản")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Cần tích vào cam kết bảo quản tài sản trước khi gửi phiếu.")]
    public bool CamKet { get; set; }
}

/// <summary>Toàn bộ dữ liệu màn hình tra cứu + lập phiếu mượn phòng.</summary>
public class DatPhongCreateVm
{
    public required Services.PhongTraCuuFilter Filter { get; init; }

    /// <summary>Toàn bộ phòng khớp bộ lọc — dùng cho số đếm và tra cứu phòng đã chọn trong phiếu.</summary>
    public required IReadOnlyList<Services.PhongTraCuuVm> Phong { get; init; }

    /// <summary>Phần phòng của trang đang xem; giao diện chỉ vẽ danh sách này.</summary>
    public required IReadOnlyList<Services.PhongTraCuuVm> PhongTrang { get; init; }

    public required IReadOnlyList<Models.Domain.ToaNha> DanhSachToa { get; init; }
    public required DatPhongFormVm Phieu { get; init; }

    public PhanTrangVm PhanTrang { get; init; } = new();

    public string HoTen { get; init; } = string.Empty;
    public string MaNguoiDung { get; init; } = string.Empty;
    public string? SoDienThoai { get; init; }

    public int PhongTrong => Phong.Count(x => x.TrangThai == Models.Catalogs.PhongTrong);
}


public class SuCoFormVm
{
    [Required(ErrorMessage = "Chọn phòng gặp sự cố")] public int MaPhong { get; set; }
    public string? LoaiSuCo { get; set; } = "SU_CO_KHAC";

    [Required(ErrorMessage = "Mô tả danh mục lỗi gặp phải")]
    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
    public string? MoTa { get; set; }

    public string? MucDoNghiemTrong { get; set; } = "MEDIUM";
    public int? MaPhongThietBi { get; set; }
    public string? GhiChuLoi { get; set; }

    /// <summary>SINH_VIEN hoặc GIANG_VIEN; mặc định theo vai trò tài khoản đang đăng nhập.</summary>
    public string? DoiTuongBaoCao { get; set; } = "SINH_VIEN";

    [Required(ErrorMessage = "Nhập họ và tên người báo cáo")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string? HoTenNguoiBaoCao { get; set; }

    [Required(ErrorMessage = "Nhập số điện thoại liên hệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    [RegularExpression(@"^[0-9+\s.\-]{8,20}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? SoDienThoaiLienHe { get; set; }

    [StringLength(1000, ErrorMessage = "Nội dung góp ý tối đa 1000 ký tự")]
    public string? NoiDungGopY { get; set; }

    /// <summary>Ảnh hiện trường: JPG/PNG, tối đa 10 MB.</summary>
    public IFormFile? AnhDinhKem { get; set; }
}

public class DonXinNghiFormVm
{
    [Required(ErrorMessage = "Chọn học phần")] public string MaHocPhan { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập lý do nghỉ")] public string LyDo { get; set; } = string.Empty;
    [Required(ErrorMessage = "Chọn phương án xử lý")] public string PhuongAnGiaiQuyet { get; set; } = "MAKEUP_CLASS";
    public string? MaGiangVienDayThay { get; set; }
}

public class NguoiDungFormVm
{
    [Required(ErrorMessage = "Nhập mã tài khoản")] public string MaNguoiDung { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập họ tên")] public string HoTen { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
    public string? MatKhau { get; set; }
    public bool BiKhoa { get; set; }
    public List<int> MaVaiTroIds { get; set; } = [];
}

public class HocPhanFormVm
{
    [Required(ErrorMessage = "Nhập mã học phần")]
    public string MaHocPhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập tên học phần")]
    public string TenHocPhan { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Số lượng đăng ký từ 1 đến 1000")]
    public int SoLuongDangKy { get; set; } = 40;

    [Display(Name = "Giảng viên")]
    public string? MaGiangVien { get; set; }

    [Display(Name = "Là lớp thực hành")]
    public bool LaThucHanh { get; set; }

    /// <summary>Nhóm ngành quyết định họ phòng được phép xếp lịch.</summary>
    [Display(Name = "Nhóm ngành")]
    public string? NhomNganh { get; set; }
}

public class ThietBiFormVm
{
    public int MaThietBi { get; set; }

    [Display(Name = "Mã thiết bị")]
    [StringLength(30, ErrorMessage = "Mã thiết bị tối đa 30 ký tự")]
    public string? MaCodeThietBi { get; set; }

    [Required(ErrorMessage = "Nhập tên thiết bị")]
    public string TenThietBi { get; set; } = string.Empty;

    [Display(Name = "Loại phòng áp dụng")]
    public string? LoaiPhong { get; set; } = "ROOM_THEORY";

    /// <summary>PHONG = gán vào một phòng, TOA = gán cho mọi phòng của tòa; để trống khi sửa nghĩa là không bổ sung.</summary>
    public string? PhamViPhanBo { get; set; } = "PHONG";

    public int? MaToaNha { get; set; }
    public int? MaPhong { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mỗi phòng từ 1 đến 100")]
    public int SoLuong { get; set; } = 1;

    public string? NhanDienThietBi { get; set; }
    public string TrangThai { get; set; } = "GOOD";
}

public class SuCoXuLyVm
{
    public int MaSuCo { get; set; }
    public string? DanhGia { get; set; }
    public string? KetQua { get; set; }
    public int? PhongDieuChuyen { get; set; }
    public string? ThongBao { get; set; }
    public string? TrangThai { get; set; }
    public string? ReturnUrl { get; set; }
}

public class GanThietBiVm
{
    [Required(ErrorMessage = "Chọn phòng")] public int MaPhong { get; set; }
    [Required(ErrorMessage = "Chọn thiết bị")] public int MaThietBi { get; set; }
    [Display(Name = "Nhãn nhận diện")] public string? NhanDienThietBi { get; set; }
    public string trangThai { get; set; } = "GOOD";
    public int SoLuong { get; set; } = 1;
}

/// <summary>Thống kê nhanh số lượng / số hỏng / số trong kho của một loại thiết bị.</summary>
public class ThietBiStatDto
{
    public int Tong { get; set; }
    public int Hong { get; set; }
    public int Kho { get; set; }
}

public class ThongBaoFormVm
{
    public int MaThongBao { get; set; }

    [Required(ErrorMessage = "Nhập tiêu đề thông báo")]
    [StringLength(255, ErrorMessage = "Tiêu đề tối đa 255 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập nội dung toàn văn")]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự")]
    [Display(Name = "Ảnh đại diện (đường dẫn)")]
    public string? AnhDaiDien { get; set; }

    [Display(Name = "Tải ảnh lên")]
    public IFormFile? AnhFile { get; set; }

    [Display(Name = "Gỡ ảnh, dùng ảnh mặc định")]
    public bool XoaAnh { get; set; }

    /// <summary>Ảnh đại diện chỉ chấp nhận ảnh bitmap (không nhận .svg vì chứa được script).</summary>
    public const long MaxDungLuongByte = 2 * 1024 * 1024;
    public static readonly string[] DinhDanhChoPhep = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    [Display(Name = "Đối tượng nhận tin")]
    public string DoiTuong { get; set; } = ThongBaoDoiTuong.All;

    [Display(Name = "Gắn nhãn Mới")]
    public bool LaMoi { get; set; }

    [Display(Name = "Tin nổi bật (cột ảnh trang chủ)")]
    public bool NoiBat { get; set; }

    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = "PUBLISHED";

    [DataType(DataType.Date)]
    [Display(Name = "Ngày đăng")]
    public DateTime NgayDang { get; set; } = DateTime.Today;
}

public class ThongBaoDetailVm
{
    public required Models.Domain.ThongBao ThongBao { get; set; }

    /// <summary>"Họ tên (mã tài khoản)" của người thao tác đăng bài, hoặc null khi tài khoản không còn.</summary>
    public string? NguoiQuanTri { get; set; }

    public IReadOnlyList<Models.Domain.ThongBao> BaiVietLienQuan { get; set; } = [];
}

/// <summary>Khối thông báo trên trang chủ: cột ảnh nổi bật + danh sách dấu ».</summary>
public class ThongBaoHomeVm
{
    public IReadOnlyList<Models.Domain.ThongBao> TinNoiBat { get; set; } = [];
    public IReadOnlyList<Models.Domain.ThongBao> TinKhac { get; set; } = [];
}
