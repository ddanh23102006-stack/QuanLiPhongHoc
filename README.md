# Hệ thống Quản lý Phòng học — Đại học Thủ Dầu Một

ASP.NET Core MVC + Web API, Entity Framework Core (khớp schema `QuanLiPhongHoc`), Cookie Authentication / RBAC, MemoryCache KPI, Chart.js.

## 1. Yêu cầu cài đặt

| Thành phần | Bắt buộc |
|---|---|
| .NET SDK **9.0** | tải tại <https://dotnet.microsoft.com/download/dotnet/9.0> (kiểm tra: `dotnet --version`) |
| SQL Server 2019 trở lên — bản Express, Developer hoặc LocalDB | LocalDB đi kèm khi tích chọn "SQL Server Data Tools" trong Visual Studio |
| Visual Studio 2022 (17.12+) | tùy chọn — có thể chạy bằng `dotnet run` mà không cần VS |

Lần chạy đầu tiên cần internet để `dotnet restore` tải 3 package NuGet (ClosedXML, EF Core SqlServer, EF Core Tools). Sau đó máy có thể chạy hoàn toàn offline — **toàn bộ CSS/JS/font đã được copy vào `wwwroot/lib`, không tải gì từ CDN**.

## 2. Chuỗi kết nối

Mặc định trong `QuanLiPhongHoc.Web/appsettings.json`:

```
Server=localhost;Database=QuanLiPhongHoc;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

`Trusted_Connection=True` nghĩa là SQL Server phải công nhận **tài khoản Windows đang đăng nhập**. Chọn đúng dòng cho máy của bạn:

| Cấu hình SQL Server trên máy | `Server=` viết là |
|---|---|
| Bản đầy đủ, instance mặc định | `localhost` |
| SQL Server **Express** | `localhost\\SQLEXPRESS` |
| **LocalDB** (không có service riêng) | `(localdb)\\mssqllocaldb` |
| Đăng nhập bằng user SQL, không dùng Windows Auth | bỏ `Trusted_Connection=True`, thêm `User ID=sa;Password=<mật khẩu>` |

Hai dấu `\\` là bắt buộc trong file JSON vì JSON tự escape chuỗi. Nếu ghi đè bằng biến môi trường thì chỉ một dấu `\`:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=QuanLiPhongHoc;Trusted_Connection=True;TrustServerCertificate=True"
```

Không kết nối được CSDL, ứng dụng sẽ in khối hướng dẫn sửa chuỗi kết nối ra cửa sổ Output/terminal rồi dừng — đó là lỗi duy nhất chặn khởi động.

## 3. Tạo database

**Không cần làm gì.** Lúc khởi động, ứng dụng tự chạy migration và seed dữ liệu mẫu (kể cả tạo mới database nếu chưa tồn tại). File `Database/QuanLiPhongHoc.sql` chỉ là phương án thay thế khi muốn dựng CSDL thủ công; nếu đã chạy script đó thì phần mềm tự nhận diện và không tạo lại.

Chạy lại từ đầu với dữ liệu sạch:

```powershell
sqlcmd -S localhost -E -Q "DROP DATABASE IF EXISTS QuanLiPhongHoc"
```

Nếu chạy tay `Database/QuanLiPhongHoc.sql` bằng `sqlcmd -i`, giữ file ở định dạng **UTF-8 có BOM**
và thêm `SET QUOTED_IDENTIFIER ON;` ở đầu (cả hai đã có sẵn trong file). Thiếu BOM thì sqlcmd đọc
file theo codepage ANSI và toàn bộ dấu tiếng Việt trong `N'...'` sẽ bị lưu sai thành `Ná»¯`, `Ã`.

## 4. Chạy

```
cd QuanLiPhongHoc.Web
dotnet run
```

Mở <http://localhost:5045> (cổng khai báo trong `Properties/launchSettings.json`). Trong Visual Studio: chọn project `QuanLiPhongHoc.Web`, profile **http**, bấm F5.

## 5. Tài khoản mẫu (mật khẩu `123456`)

| Mã        | Vai trò                                    |
|-----------|--------------------------------------------|
| AD01      | ROLE_ADMIN (Quản trị: tài khoản & nhật ký)  |
| CSVC501   | ROLE_FACILITY (Nhân viên cơ sở vật chất)   |
| ST501     | ROLE_TRAINING (Nhân viên đào tạo)          |
| GV1005    | ROLE_LECTURER                              |
| GV1006    | ROLE_LECTURER (dạy thay)                   |
| SV202601  | ROLE_STUDENT                               |

## 6. Quên mật khẩu và cấu hình email

Luồng khôi phục mật khẩu nằm ở `/Account/ForgotPassword`: người dùng nhập **email hoặc mã tài khoản**,
hệ thống tạo token ngẫu nhiên 32 byte, chỉ lưu **SHA-256** của token trong bảng `DatLaiMatKhau`
(không lưu token gốc). Token có hiệu lực 30 phút, mỗi tài khoản chỉ giữ một yêu cầu còn hiệu lực
và token bị xóa ngay sau khi đặt lại thành công. Trang luôn trả về cùng một thông báo dù tài khoản
có tồn tại hay không, để không dò được danh sách email.

Khi **đang đăng nhập**, người dùng đổi mật khẩu ngay trong menu tài khoản ở góc phải thanh đầu trang
(mục **Đổi mật khẩu**): phải nhập đúng mật khẩu hiện tại, mật khẩu mới tối thiểu 8 ký tự và khác mật khẩu
cũ. Mật khẩu được băm lại bằng PBKDF2 và thao tác được ghi nhật ký (`ACTION_UPDATE`).

**Chạy thử không cần SMTP (mặc định):** khi `Email:Host` để trống và đang ở môi trường `Development`,
thư được ghi thành tệp `.eml` trong `App_Data/EmailOutbox`, đồng thời liên kết đặt lại hiển thị ngay
trên trang Quên mật khẩu để bấm trực tiếp. Có thể mở tệp `.eml` bằng Outlook để xem đúng nội dung thư.

**Gửi email thật:** điền mục `Email` trong `QuanLiPhongHoc.Web/appsettings.json` (hoặc ghi đè bằng
biến môi trường khi không muốn lưu mật khẩu vào file):

```powershell
$env:Email__Host = "smtp.gmail.com"
$env:Email__Port = "587"
$env:Email__UseStartTls = "true"
$env:Email__UserName = "your-account@gmail.com"
$env:Email__Password = "<mật khẩu ứng dụng Gmail>"
$env:Email__FromEmail = "your-account@gmail.com"
$env:Email__PublicBaseUrl = "http://localhost:5045"   # địa chỉ dùng để dựng liên kết trong thư
```

Gmail yêu cầu **App Password** (bật xác thực 2 bước) — không dùng mật khẩu đăng nhập thường.
Khi triển khai ngoài `Development`, bắt buộc cấu hình `Email:Host`; nếu thiếu, ứng dụng chỉ ghi log lỗi
chứ không gửi thư, còn người dùng vẫn thấy thông báo chung trên giao diện.

## 7. Sao chép sang máy khác (hoặc khi nộp bài)

Thư mục chứa nhiều artifact chỉ đúng trên máy đã build, **phải xóa trước khi copy**:

```powershell
dotnet clean
Remove-Item -Recurse -Force QuanLiPhongHoc.Web\bin, QuanLiPhongHoc.Web\obj, .vs, QuanLiPhongHoc.Web\App_Data
```

Lý do: `obj/project.assets.json` ghi đường dẫn tuyệt đối và thư mục NuGet cache **của máy cũ**, nên mang sang máy khác sẽ báo lỗi restore/build. `.vs` chứa cấu hình cục bộ của Visual Studio, còn `App_Data/DataProtection-Keys` chứa khóa cookie mã hóa theo DPAPI của tài khoản Windows hiện tại — chép sang máy khác làm mọi phiên đăng nhập giải mã lỗi. `.gitignore` đã loại các mục này khỏi git; nếu gửi bài bằng file zip thì phải xóa tay.

Project gọn còn ~5 MB sau khi dọn (bản đầy đủ đang là ~204 MB).

## 8. Lỗi thường gặp

| Hiện tượng | Nguyên nhân và cách sửa |
|---|---|
| `Failed to bind to address http://127.0.0.1:5045: address already in use` | Cổng 5045 đang bị tiến trình khác giữ (thường là một cửa sổ `dotnet run` chưa tắt). Tìm PID: `netstat -ano \| findstr :5045` rồi tắt tiến trình đó, hoặc đổi `applicationUrl` trong `Properties/launchSettings.json` |
| `A network-related or instance-specific error occurred` | Sai tên instance ở mục 2 — máy cài Express phải dùng `localhost\\SQLEXPRESS` |
| `Cannot open database "QuanLiPhongHoc" requested by the login. The login failed.` | Tài khoản Windows hiện tại chưa là SQL login. Mở SSMS → Security → Logins → Add Windows Login cho user đang dùng, gán role `sysadmin` |
| Trang web hiển thị vỡ, không có kiểu dáng | Missing `wwwroot/lib` — thư mục này phải được copy cùng project vì app không dùng CDN ngoài |
| `dotnet run` báo "Couldn't find a project to run" | Đang đứng ở thư mục solution; phải `cd QuanLiPhongHoc.Web` |
| Build báo lỗi restore sau khi copy thư mục | Chưa xóa `bin`/`obj`/`.vs` — xem mục 6 |
