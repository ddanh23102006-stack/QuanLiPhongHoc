using System.Text.Json;
using QuanLiPhongHoc.Web.Models;

namespace QuanLiPhongHoc.Web.Services;

public interface ICauHinhCaHocService
{
    /// <summary>Cấu hình ca học đang áp dụng.</summary>
    IReadOnlyList<CaHocSetting> CaHocs { get; }

    /// <summary>Nạp cấu hình từ App_Data; nếu không có / hỏng thì giữ bộ mặc định theo quy chuẩn.</summary>
    void Nap();

    /// <summary>Kiểm tra và lưu cấu hình ca học xuống JSON, đồng thời áp dụng cho toàn hệ thống.</summary>
    Task<(bool Ok, List<string> Errors)> LuuAsync(List<CaHocSetting> items);
}

/// <summary>
/// Quản lý cấu hình khung giờ ca học dưới dạng file JSON trong App_Data.
/// Cấu hình nạp một lần lúc khởi động và được áp dụng lại ngay khi nhân viên đào tạo lưu.
/// </summary>
public class CauHinhCaHocService : ICauHinhCaHocService
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _file;
    private readonly SemaphoreSlim _khoa = new(1, 1);

    public CauHinhCaHocService(IWebHostEnvironment env)
    {
        _file = Path.Combine(env.ContentRootPath, "App_Data", "cau-hinh-ca-hoc.json");
    }

    public IReadOnlyList<CaHocSetting> CaHocs => Catalogs.CaHocSettings;

    public void Nap()
    {
        if (!File.Exists(_file)) return;
        try
        {
            var items = JsonSerializer.Deserialize<List<CaHocSetting>>(File.ReadAllText(_file), Json);
            if (items is { Count: > 0 }) Catalogs.ApDungCauHinhCaHoc(items);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            // File cấu hình hỏng/không đọc được: giữ bộ mặc định, không làm sập ứng dụng.
        }
    }

    public async Task<(bool Ok, List<string> Errors)> LuuAsync(List<CaHocSetting> items)
    {
        var ds = items.Where(x => !string.IsNullOrWhiteSpace(x.Code)).ToList();
        var errors = new List<string>();

        if (ds.Count == 0)
            errors.Add("Cần ít nhất một ca học.");

        foreach (var c in ds)
        {
            if (c.GioBatDau is null || c.GioKetThuc is null)
                errors.Add($"Ca {c.Code}: khung giờ phải theo định dạng HH:mm.");
            else if (c.GioKetThuc <= c.GioBatDau)
                errors.Add($"Ca {c.Code}: giờ kết thúc phải sau giờ bắt đầu.");

            if (c.SoTiet is not (3 or 5))
                errors.Add($"Ca {c.Code}: ca lý thuyết phải có 3 tiết, ca thực hành phải có 5 tiết.");
            if (c.Tiet < 1 || c.Tiet + c.SoTiet - 1 > 15)
                errors.Add($"Ca {c.Code}: dải tiết phải nằm trong tiết 1 đến tiết 15.");
        }

        if (ds.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ds.Count)
            errors.Add("Mã ca học không được trùng nhau.");

        if (errors.Count > 0)
            return (false, errors);

        foreach (var c in ds)
        {
            c.Code = c.Code.Trim();
            c.Label = string.IsNullOrWhiteSpace(c.Label) ? c.Code : c.Label.Trim();
            c.BatDau = c.GioBatDau!.Value.ToString("HH\\:mm");
            c.KetThuc = c.GioKetThuc!.Value.ToString("HH\\:mm");
        }

        await _khoa.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            await File.WriteAllTextAsync(_file, JsonSerializer.Serialize(ds, Json));
            Catalogs.ApDungCauHinhCaHoc(ds);
        }
        finally
        {
            _khoa.Release();
        }

        return (true, []);
    }
}
