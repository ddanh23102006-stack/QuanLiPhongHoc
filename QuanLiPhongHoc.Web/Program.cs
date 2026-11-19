using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Data;
using QuanLiPhongHoc.Web.Repositories;
using QuanLiPhongHoc.Web.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuanLiPhongHocContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DataProtection dùng cho cookie đăng nhập & antiforgery. Mặc định ghi vào %LOCALAPPDATA%
// có thể bị chặn quyền trên một số máy, nên lưu khóa vào thư mục của ứng dụng.
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("QuanLiPhongHoc");

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IConflictDetectionService, ConflictDetectionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPhongTraCuuService, PhongTraCuuService>();
builder.Services.AddScoped<ITroLyTimPhongService, TroLyTimPhongService>();
builder.Services.AddScoped<ILichHocService, LichHocService>();
builder.Services.AddScoped<IDatPhongService, DatPhongService>();
builder.Services.AddScoped<ISuCoService, SuCoService>();
builder.Services.AddScoped<IDonXinNghiService, DonXinNghiService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IAutoSchedulingService, AutoSchedulingService>();
builder.Services.AddScoped<IThongKeService, ThongKeService>();

// Cấu hình khung giờ ca học lưu trong App_Data/cau-hinh-ca-hoc.json, dùng chung toàn ứng dụng.
builder.Services.AddSingleton<ICauHinhCaHocService, CauHinhCaHocService>();

builder.Services.AddControllersWithViews(options =>
{
    // Toàn bộ thông báo lỗi nhập liệu hiển thị bằng tiếng Việt.
    var thongBao = options.ModelBindingMessageProvider;
    thongBao.SetAttemptedValueIsInvalidAccessor((value, field) => $"Giá trị '{value}' không hợp lệ cho {field}.");
    thongBao.SetMissingBindRequiredValueAccessor(field => $"Thiếu giá trị cho {field}.");
    thongBao.SetMissingKeyOrValueAccessor(() => "Thiếu giá trị.");
    thongBao.SetMissingRequestBodyRequiredValueAccessor(() => "Thiếu nội dung yêu cầu.");
    thongBao.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
    thongBao.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
    thongBao.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là số.");
    thongBao.SetUnknownValueIsInvalidAccessor(field => $"Giá trị không hợp lệ cho {field}.");
    thongBao.SetValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
    thongBao.SetValueMustBeANumberAccessor(field => $"{field} phải là số.");
    thongBao.SetValueMustNotBeNullAccessor(value => $"Giá trị '{value}' không được để trống.");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "TDMU.PhongHoc.Auth";

        // RBAC: người dùng đã đăng nhập nhưng không đủ vai trò -> trả về mã lỗi 403
        // (mặc định Cookie handler sẽ redirect 302, ở đây ép về đúng HTTP 403 Forbidden).
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Nạp cấu hình khung giờ ca học trước khi phục vụ request để mọi màn hình dùng đúng khung giờ đã lưu.
app.Services.GetRequiredService<ICauHinhCaHocService>().Nap();

// Ảnh sự cố mẫu nằm trong wwwroot/images/suco-mau (không bị .gitignore); chép sang thư mục
// upload để báo cáo test vẫn hiển thị ảnh khi dự án được chạy trên máy khác.
var soAnhKhoiPhuc = SuCoAnhMau.KhoiPhuc(app.Environment.WebRootPath);
if (soAnhKhoiPhuc > 0)
    app.Logger.LogInformation("Đã khôi phục {SoAnh} ảnh sự cố mẫu vào wwwroot/uploads/suco.", soAnhKhoiPhuc);

// Chạy migration + seed dữ liệu mẫu khi khởi động.
// Không kết nối được CSDL là lý do số 1 khiến dự án chạy hỏng trên máy khác (SQL Server cài ở
// instance có tên, hoặc tài khoản Windows không phải SQL login), nên in hướng dẫn cụ thể
// thay vì để người dùng đối mặt stack trace của SqlException.
try
{
    using var scope = app.Services.CreateScope();


    //lỗi 530:
    // await DbSeeder.SeedAsync(scope.ServiceProvider);
}
catch (Exception ex) when (ex is SqlException or InvalidOperationException)
{
    app.Logger.LogCritical(
        """
        ────────────────────────────────────────────────────────────────────────
        KHÔNG KẾT NỐI ĐƯỢC DATABASE — ứng dụng cần SQL Server để khởi động.

        Chuỗi kết nối đang dùng:
          {ConnectionString}

        Sửa `appsettings.json` (mục ConnectionStrings:DefaultConnection) theo đúng máy:
          • Bản mặc định:      Server=localhost;Database=QuanLiPhongHoc;Trusted_Connection=True;TrustServerCertificate=True
          • SQL Server Express: Server=localhost\\SQLEXPRESS;Database=QuanLiPhongHoc;Trusted_Connection=True;TrustServerCertificate=True
          • LocalDB:            Server=(localdb)\\mssqllocaldb;Database=QuanLiPhongHoc;Trusted_Connection=True;TrustServerCertificate=True
          • Đăng nhập bằng SQL: thêm User ID=sa;Password=<mật khẩu> thay cho Trusted_Connection=True

        Không muốn sửa file thì ghi đè bằng biến môi trường trước khi chạy
        (PowerShell viết một dấu \, còn trong appsettings.json phải viết hai dấu \\):
          $env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=QuanLiPhongHoc;Trusted_Connection=True;TrustServerCertificate=True"

        Bảng và dữ liệu mẫu sẽ tự tạo lúc khởi động, không cần chạy tay script nào.
        Lỗi gốc: {ErrorType}: {ErrorMessage}
        ────────────────────────────────────────────────────────────────────────
        """,
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ex.GetType().Name,
        ex.Message);
    throw;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Hiển thị trang 401/403 thân thiện nhưng vẫn giữ đúng mã trạng thái HTTP.
// Phải đặt trước UseRouting/UseAuthorization: nếu đặt sau, mã 403 do AuthorizationMiddleware
// chặn sẽ không đi qua middleware này và trình duyệt chỉ nhận về một trang trắng.
app.UseStatusCodePagesWithReExecute("/Account/Denied", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
