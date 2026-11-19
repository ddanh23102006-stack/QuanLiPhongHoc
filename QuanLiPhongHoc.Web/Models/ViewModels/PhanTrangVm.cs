namespace QuanLiPhongHoc.Web.Models.ViewModels;

/// <summary>Dữ liệu cho partial _PhanTrang — dùng chung mọi màn hình danh sách.</summary>
public class PhanTrangVm
{
    public int Trang { get; init; } = 1;
    public int SoTrang { get; init; } = 1;
    public int TongSo { get; init; }
    public int KichThuoc { get; init; } = 20;

    /// <summary>Đơn vị hiển thị, ví dụ "phòng", "phiếu", "thiết bị".</summary>
    public string DonVi { get; init; } = "mục";

    public Func<int, string> TaoUrl { get; init; } = _ => "#";

    public int Tu => TongSo == 0 ? 0 : (Trang - 1) * KichThuoc + 1;
    public int Den => Math.Min(TongSo, Trang * KichThuoc);
}
