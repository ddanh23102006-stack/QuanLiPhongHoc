using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiPhongHoc.Web.Models.Domain;

[Table("NguoiDung")]
public class NguoiDung
{
    [Key]
    [Column("ma_nguoi_dung")]
    [StringLength(50)]
    public string MaNguoiDung { get; set; } = string.Empty;

    [Column("ho_ten")]
    [StringLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Column("mat_khau")]
    [StringLength(255)]
    public string MatKhau { get; set; } = string.Empty;

    [Column("bi_khoa")]
    public bool BiKhoa { get; set; }

    [Column("ngay_tao")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [Column("so_dien_thoai")]
    [StringLength(20)]
    public string? SoDienThoai { get; set; }

    [Column("ngay_sinh")]
    [DataType(DataType.Date)]
    public DateTime? NgaySinh { get; set; }

    [Column("gioi_tinh")]
    [StringLength(20)]
    public string? GioiTinh { get; set; }

    [Column("noi_sinh")]
    [StringLength(200)]
    public string? NoiSinh { get; set; }

    public ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
}

[Table("VaiTro")]
public class VaiTro
{
    [Key]
    [Column("ma_vai_tro")]
    public int MaVaiTro { get; set; }

    [Column("code_vai_tro")]
    [StringLength(50)]
    public string CodeVaiTro { get; set; } = string.Empty;

    [Column("ten_vai_tro")]
    [StringLength(100)]
    public string? TenVaiTro { get; set; }

    public ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
}

[Table("NguoiDung_VaiTro")]
public class NguoiDungVaiTro
{
    [Column("ma_nguoi_dung")]
    [StringLength(50)]
    public string MaNguoiDung { get; set; } = string.Empty;

    [Column("ma_vai_tro")]
    public int MaVaiTro { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public VaiTro VaiTro { get; set; } = null!;
}

[Table("ToaNha")]
public class ToaNha
{
    [Key]
    [Column("ma_toa_nha")]
    public int MaToaNha { get; set; }

    [Column("code_toa_nha")]
    [StringLength(20)]
    public string CodeToaNha { get; set; } = string.Empty;

    [Column("ten_toa_nha")]
    [StringLength(100)]
    public string? TenToaNha { get; set; }

    [Column("dia_chi")]
    [StringLength(255)]
    public string? DiaChi { get; set; }

    [Column("so_tang")]
    public int? SoTang { get; set; }

    /// <summary>Tòa nhà ngừng hoạt động/bảo trì thì toàn bộ phòng bên trong bị coi là offline khi mượn phòng.</summary>
    [Column("trang_thai_hoat_dong")]
    [StringLength(30)]
    public string TrangThaiHoatDong { get; set; } = "ACTIVE";

    public ICollection<PhongHoc> PhongHocs { get; set; } = new List<PhongHoc>();
}

[Table("PhongHoc")]
public class PhongHoc
{
    [Key]
    [Column("ma_phong")]
    public int MaPhong { get; set; }

    [Column("code_phong")]
    [StringLength(50)]
    public string CodePhong { get; set; } = string.Empty;

    [Column("ma_toa_nha")]
    public int? MaToaNha { get; set; }

    [Column("loai_phong")]
    [StringLength(50)]
    public string LoaiPhong { get; set; } = string.Empty;

    [Column("suc_chua")]
    public int SucChua { get; set; }

    [Column("trang_thai_phong")]
    [StringLength(30)]
    public string TrangThaiPhong { get; set; } = "AVAILABLE";

    /// <summary>
    /// CSVC bật để phòng xuất hiện trên trang đăng ký mượn của giảng viên/sinh viên.
    /// Tắt thì phòng bị ẩn hoàn toàn, khung giờ trống của phòng cũng không được dùng để đăng ký.
    /// </summary>
    [Column("hien_thi_dang_ky_muon")]
    public bool HienThiDangKyMuon { get; set; } = true;

    public ToaNha? ToaNha { get; set; }
    public ICollection<PhongThietBi> PhongThietBis { get; set; } = new List<PhongThietBi>();
    public ICollection<LichHoc> LichHocs { get; set; } = new List<LichHoc>();
    public ICollection<PhongKhungTrong> KhungTrongs { get; set; } = new List<PhongKhungTrong>();
}

/// <summary>
/// Một khung giờ phòng trống do CSVC khai báo (ví dụ 07:00 - 11:25). Người mượn đăng ký
/// theo giờ chứ không theo ca, nên khung giờ này là căn cứ duy nhất để biết phòng còn trống lúc nào.
/// </summary>
[Table("Phong_KhungTrong")]
public class PhongKhungTrong
{
    [Key]
    [Column("ma_khung_trong")]
    public int MaKhungTrong { get; set; }

    [Column("ma_phong")]
    public int MaPhong { get; set; }

    [Column("gio_bat_dau")]
    public TimeOnly GioBatDau { get; set; }

    [Column("gio_ket_thuc")]
    public TimeOnly GioKetThuc { get; set; }

    public PhongHoc? PhongHoc { get; set; }
}

[Table("ThietBi")]
public class ThietBi
{
    [Key]
    [Column("ma_thiet_bi")]
    public int MaThietBi { get; set; }

    [Column("loai_phong")]
    [StringLength(50)]
    public string? LoaiPhong { get; set; }

    [Column("ten_thiet_bi")]
    [StringLength(100)]
    public string TenThietBi { get; set; } = string.Empty;

    /// <summary>Mã quản lý của loại thiết bị, do CSVC đặt (VD: TB-MC-01).</summary>
    [Column("ma_code_thiet_bi")]
    [StringLength(30)]
    public string? MaCodeThietBi { get; set; }
}

[Table("Phong_ThietBi")]
public class PhongThietBi
{
    [Key]
    [Column("ma_phong_thiet_bi")]
    public int MaPhongThietBi { get; set; }

    [Column("ma_phong")]
    public int? MaPhong { get; set; }

    [Column("ma_thiet_bi")]
    public int? MaThietBi { get; set; }

    [Column("nhan_dien_thiet_bi")]
    [StringLength(50)]
    public string? NhanDienThietBi { get; set; }

    [Column("trang_thai")]
    [StringLength(30)]
    public string TrangThai { get; set; } = "GOOD";

    public PhongHoc? PhongHoc { get; set; }
    public ThietBi? ThietBi { get; set; }
}

[Table("HocPhan")]
public class HocPhan
{
    [Key]
    [Column("ma_hoc_phan")]
    [StringLength(50)]
    public string MaHocPhan { get; set; } = string.Empty;

    [Column("ten_hoc_phan")]
    [StringLength(150)]
    public string TenHocPhan { get; set; } = string.Empty;

    [Column("so_luong_dang_ky")]
    public int SoLuongDangKy { get; set; }

    [Column("ma_giang_vien")]
    [StringLength(50)]
    public string? MaGiangVien { get; set; }

    [Column("la_thuc_hanh")]
    public bool LaThucHanh { get; set; }

    /// <summary>Nhóm ngành để ghép học phần thực hành với đúng họ phòng chuyên ngành.</summary>
    [Column("nhom_nganh")]
    [StringLength(50)]
    public string? NhomNganh { get; set; }

    public NguoiDung? GiangVien { get; set; }
    public ICollection<LichHoc> LichHocs { get; set; } = new List<LichHoc>();
}

[Table("LichHoc")]
public class LichHoc
{
    [Key]
    [Column("ma_lich_hoc")]
    public int MaLichHoc { get; set; }

    [Column("ma_hoc_phan")]
    [StringLength(50)]
    public string? MaHocPhan { get; set; }

    [Column("ma_phong")]
    public int? MaPhong { get; set; }

    [Column("ngay_hoc")]
    public DateOnly NgayHoc { get; set; }

    [Column("ca_hoc")]
    [StringLength(30)]
    public string CaHoc { get; set; } = string.Empty;

    [Column("trang_thai_hoat_dong")]
    public bool TrangThaiHoatDong { get; set; } = true;

    [Column("ma_giang_vien_day_thay", TypeName = "varchar(50)")]
    [StringLength(50)]
    public string? MaGiangVienDayThay { get; set; }

    [Column("ghi_chu_dieu_phoi")]
    [StringLength(500)]
    public string? GhiChuDieuPhoi { get; set; }

    public HocPhan? HocPhan { get; set; }
    public PhongHoc? PhongHoc { get; set; }
    public NguoiDung? GiangVienDayThay { get; set; }
}

[Table("DatPhong")]
public class DatPhong
{
    [Key]
    [Column("ma_dat_phong")]
    public int MaDatPhong { get; set; }

    [Column("nguoi_yeu_cau")]
    [StringLength(50)]
    public string? NguoiYeuCau { get; set; }

    [Column("ma_phong")]
    public int? MaPhong { get; set; }

    [Column("ngay_dat")]
    public DateOnly NgayDat { get; set; }

    /// <summary>Khung giờ xin mượn — tự do trong các khung giờ trống của phòng, không bó theo ca.</summary>
    [Column("gio_bat_dau")]
    public TimeOnly GioBatDau { get; set; }

    [Column("gio_ket_thuc")]
    public TimeOnly GioKetThuc { get; set; }

    [Column("muc_dich")]
    public string? MucDich { get; set; }

    [Column("so_luong_nguoi_tham_gia")]
    public int? SoLuongNguoiThamGia { get; set; }

    [Column("trang_thai")]
    [StringLength(30)]
    public string TrangThai { get; set; } = "PENDING";

    [Column("nguoi_duyet")]
    [StringLength(50)]
    public string? NguoiDuyet { get; set; }

    /// <summary>Thời điểm CSVC duyệt/từ chối phiếu — mốc để báo kết quả cho người gửi.</summary>
    [Column("thoi_diem_xu_ly")]
    public DateTime? ThoiDiemXuLy { get; set; }

    /// <summary>Người gửi đã mở trang Phiếu đã gửi và xem kết quả xử lý chưa.</summary>
    [Column("da_xem_ket_qua")]
    public bool DaXemKetQua { get; set; }

    public NguoiDung? NguoiYeuCauNav { get; set; }
    public NguoiDung? NguoiDuyetNav { get; set; }
    public PhongHoc? PhongHoc { get; set; }
}

[Table("SuCo")]
public class SuCo
{
    [Key]
    [Column("ma_su_co")]
    public int MaSuCo { get; set; }

    [Column("ma_phong")]
    public int? MaPhong { get; set; }

    [Column("nguoi_bao_cao")]
    [StringLength(50)]
    public string? NguoiBaoCao { get; set; }

    [Column("loai_su_co")]
    [StringLength(50)]
    public string? LoaiSuCo { get; set; }

    [Column("mo_ta")]
    public string? MoTa { get; set; }

    [Column("muc_do_nghiem_trong")]
    [StringLength(20)]
    public string? MucDoNghiemTrong { get; set; }

    [Column("trang_thai")]
    [StringLength(30)]
    public string TrangThai { get; set; } = "NEW";

    [Column("ngay_tao")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    /// <summary>Nhân viên CSVC tiếp nhận và xử lý báo cáo.</summary>
    [Column("nguoi_xu_ly", TypeName = "varchar(50)")]
    [StringLength(50)]
    public string? NguoiXuLy { get; set; }

    [Column("ngay_xu_ly")]
    public DateTime? NgayXuLy { get; set; }

    /// <summary>Đánh giá của CSVC sau khi kiểm tra hiện trường.</summary>
    [Column("danh_gia")]
    public string? DanhGia { get; set; }

    /// <summary>RESTORED = đã phục hồi, TRANSFERRED = điều chuyển phòng.</summary>
    [Column("ket_qua_xu_ly")]
    [StringLength(30)]
    public string? KetQuaXuLy { get; set; }

    [Column("phong_dieu_chuyen")]
    public int? PhongDieuChuyen { get; set; }

    /// <summary>Nội dung thông báo gửi lại người báo cáo.</summary>
    [Column("thong_bao_phan_hoi")]
    [StringLength(500)]
    public string? ThongBaoPhanHoi { get; set; }

    [Column("da_xem_phan_hoi")]
    public bool DaXemPhanHoi { get; set; }

    /// <summary>SINH_VIEN hoặc GIANG_VIEN, chốt tại thời điểm gửi báo cáo.</summary>
    [Column("doi_tuong_bao_cao")]
    [StringLength(20)]
    public string? DoiTuongBaoCao { get; set; }

    /// <summary>Họ tên chốt tại thời điểm gửi, tránh lệch nếu hồ sơ tài khoản đổi sau đó.</summary>
    [Column("ho_ten_nguoi_bao_cao")]
    [StringLength(100)]
    public string? HoTenNguoiBaoCao { get; set; }

    [Column("so_dien_thoai_lien_he")]
    [StringLength(20)]
    public string? SoDienThoaiLienHe { get; set; }

    /// <summary>Góp ý / đề xuất đổi phòng học của người báo cáo.</summary>
    [Column("noi_dung_gop_y")]
    [StringLength(1000)]
    public string? NoiDungGopY { get; set; }

    /// <summary>Đường dẫn ảnh hiện trường trong wwwroot/uploads/suco.</summary>
    [Column("anh_dinh_kem")]
    [StringLength(255)]
    public string? AnhDinhKem { get; set; }

    public PhongHoc? PhongHoc { get; set; }
    public NguoiDung? NguoiBaoCaoNav { get; set; }
    public NguoiDung? NguoiXuLyNav { get; set; }
    public PhongHoc? PhongDieuChuyenNav { get; set; }
    public ICollection<ChiTietSuCo> ChiTietSuCos { get; set; } = new List<ChiTietSuCo>();
}

/// <summary>
/// Phiếu yêu cầu xử lý do CSVC lập khi cử nhân viên đến sửa thiết bị.
/// Tách khỏi SuCo để nhân viên vẫn ghi được yêu cầu ngay trên trang thiết bị,
/// kể cả khi không có người dùng nào gửi báo cáo trước đó.
/// </summary>
[Table("YeuCauSuaChua")]
public class YeuCauSuaChua
{
    [Key]
    [Column("ma_yeu_cau")]
    public int MaYeuCau { get; set; }

    [Column("ma_phong_thiet_bi")]
    public int? MaPhongThietBi { get; set; }

    [Column("ma_phong")]
    public int? MaPhong { get; set; }

    [Column("nguoi_yeu_cau", TypeName = "varchar(50)")]
    [StringLength(50)]
    public string? NguoiYeuCau { get; set; }

    [Column("nhan_vien_xu_ly")]
    [StringLength(100)]
    public string? NhanVienXuLy { get; set; }

    [Column("noi_dung")]
    public string NoiDung { get; set; } = string.Empty;

    [Column("ngay_hen")]
    public DateTime? NgayHen { get; set; }

    [Column("trang_thai")]
    [StringLength(30)]
    public string TrangThai { get; set; } = "MOI";

    [Column("ngay_tao")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [Column("ngay_hoan_thanh")]
    public DateTime? NgayHoanThanh { get; set; }

    [Column("ket_qua")]
    public string? KetQua { get; set; }

    public PhongThietBi? PhongThietBi { get; set; }
    public PhongHoc? PhongHoc { get; set; }
    public NguoiDung? NguoiYeuCauNav { get; set; }
}

[Table("ChiTietSuCo")]
public class ChiTietSuCo
{
    [Key]
    [Column("ma_chi_tiet_su_co")]
    public int MaChiTietSuCo { get; set; }

    [Column("ma_su_co")]
    public int? MaSuCo { get; set; }

    [Column("ma_phong_thiet_bi")]
    public int? MaPhongThietBi { get; set; }

    [Column("ghi_chu_loi")]
    [StringLength(255)]
    public string? GhiChuLoi { get; set; }

    public SuCo? SuCo { get; set; }
    public PhongThietBi? PhongThietBi { get; set; }
}

[Table("DonXinNghi")]
public class DonXinNghi
{
    [Key]
    [Column("ma_don")]
    public int MaDon { get; set; }

    [Column("ma_giang_vien")]
    [StringLength(50)]
    public string? MaGiangVien { get; set; }

    [Column("ma_hoc_phan")]
    [StringLength(50)]
    public string? MaHocPhan { get; set; }

    [Column("ly_do")]
    public string LyDo { get; set; } = string.Empty;

    [Column("phuong_an_giai_quyet")]
    [StringLength(30)]
    public string PhuongAnGiaiQuyet { get; set; } = string.Empty;

    [Column("ma_giang_vien_day_thay")]
    [StringLength(50)]
    public string? MaGiangVienDayThay { get; set; }

    [Column("trang_thai")]
    [StringLength(30)]
    public string TrangThai { get; set; } = "PENDING";

    [Column("ngay_tao")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [Column("nguoi_duyet")]
    [StringLength(50)]
    public string? NguoiDuyet { get; set; }

    public NguoiDung? GiangVien { get; set; }
    public NguoiDung? GiangVienDayThay { get; set; }
    public NguoiDung? NguoiDuyetNav { get; set; }
    public HocPhan? HocPhan { get; set; }
}

[Table("NhatKyHeThong")]
public class NhatKyHeThong
{
    [Key]
    [Column("ma_nhat_ky")]
    public int MaNhatKy { get; set; }

    [Column("thoi_gian")]
    public DateTime ThoiGian { get; set; } = DateTime.Now;

    [Column("ma_nguoi_thuc_hien")]
    [StringLength(50)]
    public string? MaNguoiThucHien { get; set; }

    [Column("loai_hanh_dong")]
    [StringLength(50)]
    public string LoaiHanhDong { get; set; } = string.Empty;

    [Column("doi_tuong_tac_dong")]
    [StringLength(100)]
    public string? DoiTuongTacDong { get; set; }

    [Column("du_lieu_cu")]
    public string? DuLieuCu { get; set; }

    [Column("du_lieu_moi")]
    public string? DuLieuMoi { get; set; }

    [Column("dia_chi_ip")]
    [StringLength(45)]
    public string? DiaChiIp { get; set; }

    public NguoiDung? NguoiThucHien { get; set; }
}

/// <summary>
/// Bản tin thông báo hiển thị trên trang chủ của từng nhóm người dùng.
/// </summary>
[Table("ThongBao")]
public class ThongBao
{
    [Key]
    [Column("ma_thong_bao")]
    public int MaThongBao { get; set; }

    [Column("tieu_de")]
    [StringLength(255)]
    public string TieuDe { get; set; } = string.Empty;

    /// <summary>Nội dung toàn văn, lưu dạng văn bản thuần; dòng trống tách đoạn.</summary>
    [Column("noi_dung")]
    public string NoiDung { get; set; } = string.Empty;

    [Column("anh_dai_dien")]
    [StringLength(255)]
    public string? AnhDaiDien { get; set; }

    /// <summary>Nhóm được xem: ALL hoặc một mã vai trò (ROLE_STUDENT, ROLE_LECTURER...).</summary>
    [Column("doi_tuong")]
    [StringLength(50)]
    public string DoiTuong { get; set; } = ThongBaoDoiTuong.All;

    [Column("la_moi")]
    public bool LaMoi { get; set; }

    /// <summary>Tin nổi bật hiển thị ở cột ảnh lớn trên trang chủ.</summary>
    [Column("noi_bat")]
    public bool NoiBat { get; set; }

    [Column("trang_thai")]
    [StringLength(30)]
    public string TrangThai { get; set; } = "PUBLISHED";

    [Column("ngay_dang")]
    public DateTime NgayDang { get; set; } = DateTime.Now;

    /// <summary>Mã người đăng, lưu đối chiếu thuần túy nên không ràng buộc khóa ngoại.</summary>
    [Column("nguoi_dang")]
    [StringLength(50)]
    public string? NguoiDang { get; set; }
}

/// <summary>
/// Yêu cầu đặt lại mật khẩu qua email. Chỉ lưu băm SHA-256 của token (không lưu token gốc),
/// mỗi người dùng có tối đa một yêu cầu còn hiệu lực và bản ghi bị xóa ngay sau khi dùng.
/// Không khai khóa ngoại sang NguoiDung: CSDL tạo bằng script dùng VARCHAR cho mã người dùng,
/// còn CSDL tạo bằng migration dùng NVARCHAR nên FK sẽ vỡ kiểu dữ liệu trên một trong hai.
/// </summary>
[Table("DatLaiMatKhau")]
public class DatLaiMatKhau
{
    [Key]
    [Column("ma_dat_lai")]
    public int MaDatLai { get; set; }

    [Column("ma_nguoi_dung")]
    [StringLength(50)]
    public string MaNguoiDung { get; set; } = string.Empty;

    [Column("token_hash")]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    [Column("het_han")]
    public DateTime HetHan { get; set; }

    [Column("ngay_tao")]
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
