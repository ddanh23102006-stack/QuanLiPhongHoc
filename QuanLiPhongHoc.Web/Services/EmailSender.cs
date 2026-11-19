using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace QuanLiPhongHoc.Web.Services;

public sealed class EmailOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "no-reply@tdmu.edu.vn";
    public string FromName { get; set; } = "TDMU - Quản lý Phòng học";

    /// <summary>Địa chỉ công khai dùng để dựng link trong email; bỏ trống thì lấy theo request hiện tại.</summary>
    public string? PublicBaseUrl { get; set; }
}

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, IWebHostEnvironment env, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        using var thu = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = htmlBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        thu.To.Add(toEmail);

        using var smtp = new SmtpClient();
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            // Chưa cấu hình SMTP: ở môi trường phát triển ghi thư thành tệp .eml để vẫn
            // chạy thử được luồng khôi phục mật khẩu mà không cần tài khoản email thật.
            if (!_env.IsDevelopment())
                throw new InvalidOperationException("Chưa cấu hình SMTP (mục Email:Host trong appsettings.json).");

            var thuMuc = Path.Combine(_env.ContentRootPath, "App_Data", "EmailOutbox");
            Directory.CreateDirectory(thuMuc);
            smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            smtp.PickupDirectoryLocation = thuMuc;
        }
        else
        {
            smtp.Host = _options.Host;
            smtp.Port = _options.Port;
            smtp.EnableSsl = _options.UseStartTls;
            if (!string.IsNullOrWhiteSpace(_options.UserName))
                smtp.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await smtp.SendMailAsync(thu);
        _logger.LogInformation("Đã gửi email {Subject} tới {Email}.", subject, toEmail);
    }
}
