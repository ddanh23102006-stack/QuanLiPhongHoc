namespace QuanLiPhongHoc.Web.Data;

/// <summary>
/// Hồ sơ cá nhân mẫu của 20 tài khoản demo. Đây là bản sao 1:1 của khối
/// <c>INSERT INTO NguoiDung</c> trong <c>Database/QuanLiPhongHoc.sql</c> — sửa một nơi thì
/// phải sửa nơi còn lại, để CSDL dựng bằng script hay để app tự seed đều ra cùng dữ liệu.
/// </summary>
public static class HoSoSeedData
{
    public record HoSo(string MaNguoiDung, string SoDienThoai, int NamSinh, int ThangSinh, int Ngay, string GioiTinh, string NoiSinh)
    {
        public DateTime NgaySinh => new(NamSinh, ThangSinh, Ngay);
    }

    public static readonly IReadOnlyList<HoSo> DanhSach =
    [
        new("SV202601", "0912345601", 2004, 03, 15, "Nam", "Bình Dương"),
        new("SV202602", "0912345602", 2004, 07, 22, "Nữ", "Tây Ninh"),
        new("SV202603", "0912345603", 2003, 11, 05, "Nam", "TP. Hồ Chí Minh"),
        new("SV202604", "0912345604", 2004, 01, 18, "Nữ", "Đồng Nai"),
        new("SV202605", "0912345605", 2004, 09, 30, "Nam", "Long An"),
        new("SV202606", "0912345606", 2003, 05, 12, "Nữ", "An Giang"),
        new("SV202607", "0912345607", 2004, 12, 08, "Nam", "Kiên Giang"),
        new("SV202608", "0912345608", 2004, 02, 25, "Nữ", "Vĩnh Long"),
        new("SV202609", "0912345609", 2003, 08, 14, "Nam", "Cà Mau"),
        new("SV202610", "0912345610", 2004, 06, 03, "Nữ", "Bình Phước"),
        new("GV1001", "0912345611", 1975, 04, 10, "Nam", "Bình Định"),
        new("GV1002", "0912345612", 1980, 10, 28, "Nữ", "TP. Hồ Chí Minh"),
        new("GV1003", "0912345613", 1983, 01, 16, "Nam", "Nghệ An"),
        new("GV1004", "0912345614", 1985, 09, 07, "Nữ", "Thanh Hóa"),
        new("GV1005", "0912345615", 1978, 03, 24, "Nữ", "Thừa Thiên Huế"),
        new("CSVC501", "0912345616", 1990, 05, 19, "Nam", "Bình Dương"),
        new("CSVC502", "0912345617", 1992, 11, 30, "Nam", "Đồng Nai"),
        new("ST501", "0912345618", 1988, 07, 11, "Nam", "Tây Ninh"),
        new("ST502", "0912345619", 1991, 02, 06, "Nữ", "Long An"),
        new("AD01", "0912345620", 1986, 12, 02, "Nam", "TP. Hồ Chí Minh")
    ];
}
