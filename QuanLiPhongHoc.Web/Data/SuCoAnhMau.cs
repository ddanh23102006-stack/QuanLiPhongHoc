namespace QuanLiPhongHoc.Web.Data;

/// <summary>
/// Ảnh sự cố mẫu đi kèm mã nguồn. Thư mục wwwroot/uploads bị .gitignore loại trừ nên khi chép
/// dự án sang máy khác các báo cáo test sẽ mất ảnh; lớp này chép lại đúng tên tệp mà cơ sở dữ liệu
/// đang tham chiếu, không ghi đè ảnh người dùng đã tải lên.
/// </summary>
public static class SuCoAnhMau
{
    private static readonly string ThuMucNguon = Path.Combine("images", "suco-mau");
    private static readonly string ThuMucDich = Path.Combine("uploads", "suco");

    /// <summary>Trả về số ảnh vừa được khôi phục.</summary>
    public static int KhoiPhuc(string webRootPath)
    {
        var nguon = Path.Combine(webRootPath, ThuMucNguon);
        if (!Directory.Exists(nguon)) return 0;

        var dich = Path.Combine(webRootPath, ThuMucDich);
        Directory.CreateDirectory(dich);

        var soAnh = 0;
        foreach (var tep in Directory.EnumerateFiles(nguon))
        {
            var duongDan = Path.Combine(dich, Path.GetFileName(tep));
            if (File.Exists(duongDan)) continue;
            File.Copy(tep, duongDan);
            soAnh++;
        }
        return soAnh;
    }
}
