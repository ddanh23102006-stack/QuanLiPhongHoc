using System.Security.Claims;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public static class ThongBaoQuery
{
    /// <summary>
    /// Lọc các bản tin đã công bố mà người dùng hiện tại thuộc nhóm nhận tin.
    /// Quản trị được xem mọi bản tin để xem trước nội dung đang quản lý.
    /// </summary>
    public static IQueryable<ThongBao> VisibleTo(this IQueryable<ThongBao> source, ClaimsPrincipal user)
    {
        var published = source.Where(x => x.TrangThai == "PUBLISHED");
        if (user.IsInRole(AppRoles.Admin)) return published;

        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return published.Where(x => x.DoiTuong == ThongBaoDoiTuong.All || roles.Contains(x.DoiTuong));
    }
}
