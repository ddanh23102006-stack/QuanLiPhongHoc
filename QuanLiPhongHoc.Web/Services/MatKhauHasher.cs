using System.Security.Cryptography;
using System.Text;

namespace QuanLiPhongHoc.Web.Services;

public enum KetQuaKiemTraMatKhau
{
    Sai,
    Dung,
    /// <summary>Mật khẩu đúng nhưng đang lưu dạng cũ (chưa băm) — cần băm lại và lưu.</summary>
    DungCanBamLai
}

/// <summary>
/// Băm mật khẩu bằng PBKDF2-HMAC-SHA256. Mỗi mật khẩu có salt ngẫu nhiên riêng nên hai người
/// dùng cùng một mật khẩu vẫn ra hai chuỗi khác nhau; việc so sánh dùng thời gian hằng để
/// không lộ thông tin qua thời gian phản hồi.
/// Định dạng lưu: PBKDF2-SHA256$&lt;số vòng lặp&gt;$&lt;salt base64&gt;$&lt;hash base64&gt;
/// </summary>
public static class MatKhauHasher
{
    private const string TienTo = "PBKDF2-SHA256";
    private const int SoVongLap = 210_000;
    private const int KichThuocSalt = 16;
    private const int KichThuocHash = 32;

    public static bool DaBam(string? giaTriLuu) =>
        giaTriLuu is not null && giaTriLuu.StartsWith(TienTo + "$", StringComparison.Ordinal);

    public static string Bam(string matKhau)
    {
        var salt = RandomNumberGenerator.GetBytes(KichThuocSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, SoVongLap, HashAlgorithmName.SHA256, KichThuocHash);
        return $"{TienTo}${SoVongLap}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static KetQuaKiemTraMatKhau KiemTra(string matKhau, string? giaTriLuu)
    {
        if (string.IsNullOrEmpty(giaTriLuu)) return KetQuaKiemTraMatKhau.Sai;

        // Dữ liệu tạo trước khi có băm: vẫn cho đăng nhập đúng một lần rồi nâng cấp sang PBKDF2.
        if (!DaBam(giaTriLuu))
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(matKhau), Encoding.UTF8.GetBytes(giaTriLuu))
                ? KetQuaKiemTraMatKhau.DungCanBamLai
                : KetQuaKiemTraMatKhau.Sai;

        var phan = giaTriLuu.Split('$');
        if (phan.Length != 4 ||
            !int.TryParse(phan[1], out var soVong) ||
            soVong <= 0 || soVong > 2_000_000)
            return KetQuaKiemTraMatKhau.Sai;

        try
        {
            var salt = Convert.FromBase64String(phan[2]);
            var mongDoi = Convert.FromBase64String(phan[3]);
            var tinhDuoc = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, soVong, HashAlgorithmName.SHA256, mongDoi.Length);
            return CryptographicOperations.FixedTimeEquals(tinhDuoc, mongDoi)
                ? KetQuaKiemTraMatKhau.Dung
                : KetQuaKiemTraMatKhau.Sai;
        }
        catch (FormatException)
        {
            return KetQuaKiemTraMatKhau.Sai;
        }
    }
}

/// <summary>
/// Token đặt lại mật khẩu: 32 byte ngẫu nhiên dạng base64url (đủ lớn để không thể đoán),
/// chỉ phần băm SHA-256 được lưu vào CSDL nên lộ dữ liệu cũng không dựng lại được link trong email.
/// </summary>
public static class TokenDatLaiMatKhau
{
    public static string Tao() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=');

    public static string Bam(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
