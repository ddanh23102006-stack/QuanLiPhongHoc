using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Services;

namespace QuanLiPhongHoc.Web.Controllers.Api;

[Authorize]
[Route("api/dashboard")]
[ApiController]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly QuanLiPhongHocContext _db;

    public DashboardApiController(IDashboardService dashboard, QuanLiPhongHocContext db)
    {
        _dashboard = dashboard;
        _db = db;
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis([FromQuery] bool refresh = false)
        => Ok(await _dashboard.GetKpisAsync(refresh));

    [HttpGet("charts")]
    public async Task<IActionResult> Charts()
    {
        return Ok(new
        {
            buildings = await _dashboard.OccupancyByBuildingAsync(),
            incidents = await _dashboard.IncidentsByStatusAsync()
        });
    }

    [HttpGet("today-schedule")]
    public async Task<IActionResult> TodaySchedule()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var data = await _db.LichHocs
            .Include(x => x.HocPhan)
            .Include(x => x.PhongHoc)
            .Where(x => x.NgayHoc == today && x.TrangThaiHoatDong)
            .Select(x => new
            {
                x.MaLichHoc,
                phong = x.PhongHoc!.CodePhong,
                hocPhan = x.HocPhan!.TenHocPhan,
                x.CaHoc,
                gv = x.HocPhan.MaGiangVien
            }).ToListAsync();
        return Ok(data);
    }
}
