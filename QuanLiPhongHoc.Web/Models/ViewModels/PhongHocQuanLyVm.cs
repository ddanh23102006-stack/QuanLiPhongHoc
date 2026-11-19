using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Models.ViewModels;

/// <summary>Một dòng trên màn hình Quản lý phòng học của nhân viên cơ sở vật chất.</summary>
public class PhongHocRowVm
{
    public int MaPhong { get; init; }
    public string CodePhong { get; init; } = string.Empty;
    public string LoaiPhong { get; init; } = string.Empty;
    public int SucChua { get; init; }
    public string TrangThaiPhong { get; init; } = string.Empty;
    public int? MaToaNha { get; init; }
    public string? Toa { get; init; }
    public IReadOnlyList<string> ThietBi { get; init; } = [];

    /// <summary>CSVC có đang đăng phòng lên trang mượn phòng hay không.</summary>
    public bool HienThiDangKyMuon { get; init; }

    /// <summary>Khung giờ trống đã khai, dạng "HH:mm-HH:mm" — dùng để điền sẵn khi mở form sửa.</summary>
    public IReadOnlyList<string> KhungTrong { get; init; } = [];

    /// <summary>Phòng vẫn AVAILABLE nhưng có lịch học/phiếu đã duyệt trong ngày — hiển thị "Đang có lớp".</summary>
    public bool CoLopHomNay { get; init; }
    public string? GhiChuGanNhat { get; init; }
    public string? GhiChiTietGanNhat { get; init; }
    public bool CoSuCoMo { get; init; }
}

public class PhongHocQuanLyVm
{
    public IReadOnlyList<PhongHocRowVm> Rows { get; init; } = [];
    public IReadOnlyList<ToaNha> DanhSachToa { get; init; } = [];

    public string? Q { get; init; }
    public int? MaToaNha { get; init; }
    public string? LoaiPhong { get; init; }
    public string? TrangThai { get; init; }
    public DateOnly Ngay { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Các chỉ số dưới đây tính trên toàn bộ phòng khớp bộ lọc, không chỉ trang đang xem.</summary>
    public int TongSo { get; init; }
    public int BaoTri { get; init; }
    public int DangCoLop { get; init; }
    public int SanSang { get; init; }

    public PhanTrangVm PhanTrang { get; init; } = new();
}
