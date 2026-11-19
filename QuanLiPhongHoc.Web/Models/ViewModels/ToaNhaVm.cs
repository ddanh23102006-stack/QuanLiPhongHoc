using System.ComponentModel.DataAnnotations;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Models.ViewModels;

public class ToaNhaFormVm
{
    public int MaToaNha { get; set; }

    [Required(ErrorMessage = "Nhập mã tòa nhà")]
    [StringLength(20, ErrorMessage = "Mã tòa nhà tối đa 20 ký tự")]
    [Display(Name = "Mã tòa nhà")]
    public string CodeToaNha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập tên tòa nhà")]
    [StringLength(100, ErrorMessage = "Tên tòa nhà tối đa 100 ký tự")]
    [Display(Name = "Tên tòa nhà")]
    public string TenToaNha { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [Range(0, 100, ErrorMessage = "Số tầng phải từ 0 đến 100 (0 = khu vực không có tầng)")]
    [Display(Name = "Số tầng")]
    public int? SoTang { get; set; }

    [Display(Name = "Trạng thái hoạt động")]
    public string TrangThaiHoatDong { get; set; } = "ACTIVE";
}

/// <summary>Một dòng tòa nhà kèm chỉ số tổng hợp của các phòng bên trong.</summary>
public class ToaNhaRowVm
{
    public required ToaNha ToaNha { get; init; }
    public int SoPhong { get; init; }
    public int SucChua { get; init; }
    public int PhongBaoTri { get; init; }
    public int ThietBi { get; init; }
    public int ThietBiCanXuLy { get; init; }
    public int SuCoMo { get; init; }
}

public class ToaNhaQuanLyVm
{
    public IReadOnlyList<ToaNhaRowVm> Rows { get; init; } = [];
    public string? Q { get; init; }
    public string? TrangThai { get; init; }

    public int TongToa { get; init; }
    public int DangHoatDong { get; init; }
    public int BaoTri { get; init; }
    public int NgungHoatDong { get; init; }
    public int TongPhong { get; init; }
}

public class ToaNhaPhongRowVm
{
    public required PhongHoc Phong { get; init; }
    public int ThietBi { get; init; }
    public int ThietBiCanXuLy { get; init; }
    public int SuCoMo { get; init; }
}

public class ToaNhaChiTietVm
{
    public required ToaNha ToaNha { get; init; }
    public required IReadOnlyList<ToaNhaPhongRowVm> Phong { get; init; }
    public required IReadOnlyList<SuCo> SuCo { get; init; }

    public int TongSucChua { get; init; }
    public int PhongBaoTri { get; init; }
    public int SoThietBi { get; init; }
    public int ThietBiCanXuLy { get; init; }
    public int SuCoMo => SuCo.Count(x => x.TrangThai != "RESOLVED");
}
