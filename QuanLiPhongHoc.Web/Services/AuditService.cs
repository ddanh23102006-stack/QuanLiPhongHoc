using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Services;

public interface IAuditService
{
    Task WriteAsync(string? userId, string action, string? target, string? oldData, string? newData, string? ip);
}

public class AuditService : IAuditService
{
    private readonly QuanLiPhongHocContext _db;

    public AuditService(QuanLiPhongHocContext db) => _db = db;

    public async Task WriteAsync(string? userId, string action, string? target, string? oldData, string? newData, string? ip)
    {
        _db.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            MaNguoiThucHien = userId,
            LoaiHanhDong = action,
            DoiTuongTacDong = target,
            DuLieuCu = oldData,
            DuLieuMoi = newData,
            DiaChiIp = ip,
            ThoiGian = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }
}
