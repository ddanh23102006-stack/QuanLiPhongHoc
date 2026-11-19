using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Data;

/// <summary>
/// Bộ thông báo mẫu cho trang chủ các vai trò, khớp nội dung bản tin của cổng TDMU.
/// </summary>
public static class ThongBaoSeedData
{
    public static List<ThongBao> Build(string? nguoiDang)
    {
        ThongBao Tao(string tieuDe, string noiDung, DateTime ngayDang, string doiTuong,
            bool laMoi = false, bool noiBat = false, string? anhDaiDien = null) => new()
        {
            TieuDe = tieuDe,
            NoiDung = noiDung,
            NgayDang = ngayDang,
            DoiTuong = doiTuong,
            LaMoi = laMoi,
            NoiBat = noiBat,
            AnhDaiDien = anhDaiDien,
            NguoiDang = nguoiDang
        };

        return
        [
            Tao(
                "KẾ HOẠCH TỔ CHỨC ĐĂNG KÝ HỌC PHẦN - HỌC KỲ 1 NĂM HỌC 2026-2027",
                """
                Đối tượng đăng ký:
                - Sinh viên đại học hệ đào tạo chính quy.

                Thời gian đăng ký:
                1. Đăng ký trực tuyến chính thức: từ ngày 21/07/2026 đến 26/07/2026
                2. Đăng ký học phần bổ sung (nguyện vọng): từ ngày 27/07/2026 đến 29/07/2026

                (Chi tiết các khung thời gian đăng ký của từng ngành đào tạo, đơn vị đào tạo và từng khóa đào tạo, xem thêm tại kế hoạch được đính kèm hoặc xem tại link: https://tdmu.dev/khdkmh261).

                Lưu ý:
                - Sinh viên phải hoàn thành học phí học kỳ trước đó thì hệ thống mới mở quyền đăng ký học phần.
                - Sau thời gian đăng ký bổ sung, hệ thống sẽ tự động đóng và không hỗ trợ mở lại.
                - Lớp học phần dưới 15 sinh viên đăng ký có nguy cơ bị hủy; sinh viên chủ động kiểm tra danh sách lớp sau khi đăng ký.
                """,
                new DateTime(2026, 7, 17), ThongBaoDoiTuong.All, noiBat: true),

            Tao(
                "HƯỚNG DẪN THIẾT LẬP EMAIL VÀ CÁCH SỬ DỤNG CỔNG THÔNG TIN SINH VIÊN",
                """
                1. Kích hoạt email trường
                - Truy cập https://mail.tdmu.edu.vn và đăng nhập bằng tài khoản TDMU ID (mã số sinh viên).
                - Mật khẩu lần đầu là mật khẩu của tài khoản TDMU ID. Sau khi đăng nhập, hệ thống yêu cầu đổi mật khẩu.

                2. Sử dụng cổng thông tin sinh viên
                - Địa chỉ: https://portal.tdmu.edu.vn
                - Các chức năng chính: xem điểm, xem thời khóa biểu, đăng ký học phần, đăng ký xét tuyển học bổng, tra cứu xác nhận nhập học và in đơn xin nghỉ.

                3. Khuyến nghị
                - Kiểm tra email trường ít nhất một lần mỗi tuần; mọi thông báo kết quả học tập và xử lý học vụ đều gửi qua địa chỉ này.
                - Không dùng email cá nhân để nhận thông báo của nhà trường.
                - Nếu quên mật khẩu, liên hệ Trung tâm Thông tin - Học liệu để được đặt lại.
                """,
                new DateTime(2026, 9, 7), ThongBaoDoiTuong.Student, laMoi: true, noiBat: true),

            Tao(
                "THÔNG BÁO LỊCH SINH HOẠT CÔNG DÂN - SINH VIÊN CUỐI KHÓA, GIỮA KHÓA CẤP TRƯỜNG NĂM HỌC 2026-2027",
                """
                Phòng Công tác Sinh viên thông báo lịch sinh hoạt công dân - sinh viên năm học 2026-2027 như sau:

                1. Đối tượng sinh hoạt cuối khóa
                - Thời gian: 07:30, ngày 15/08/2026
                - Địa điểm: Hội trường lớn, cơ sở chính
                - Nội dung: phổ biến quy chế tốt nghiệp, thủ tục xét công nhận tốt nghiệp, chương trình lễ tốt nghiệp và lấy bằng.

                2. Đối tượng sinh hoạt giữa khóa
                - Thời gian: 13:30, ngày 22/08/2026
                - Địa điểm: Giảng đường A2, chia 2 ca theo khóa
                - Nội dung: đánh giá kết quả rèn luyện, chế độ chính sách, an ninh học đường và phòng chống tội phạm công nghệ cao.

                3. Yêu cầu
                - Sinh viên đi đầy đủ, mặc đồng phục, mang theo thẻ sinh viên để điểm danh.
                - Kết quả tham gia được cộng vào điểm rèn luyện học kỳ 1.
                - Sinh viên vắng mặt không lý do phải sinh hoạt bù và bị trừ điểm rèn luyện.
                """,
                new DateTime(2026, 8, 10), ThongBaoDoiTuong.Student, noiBat: true),

            Tao(
                "THÔNG BÁO THU THẬP THÔNG TIN SINH VIÊN THUỘC ĐỐI TƯỢNG CHÍNH SÁCH, HOÀN CẢNH KHÓ KHĂN HỌC KỲ 2 2025-2026",
                """
                Nhà trường tổ chức thu thập thông tin sinh viên thuộc diện chính sách và hoàn cảnh khó khăn học kỳ 2 năm học 2025-2026.

                1. Đối tượng
                - Sinh viên là con thương binh, bệnh binh, thân nhân liệt sĩ, người có công với cách mạng.
                - Sinh viên thuộc hộ nghèo, cận nghèo theo quy định.
                - Sinh viên khuyết tật hoặc có hoàn cảnh đặc biệt khó khăn có xác nhận của địa phương.

                2. Hồ sơ gồm
                - Đơn đề nghị miễn giảm học phí theo mẫu của Bộ Giáo dục và Đào tạo.
                - Bản sao công chứng giấy tờ chứng nhận đối tượng chính sách.
                - Sổ hộ nghèo, cận nghèo hoặc giấy xác nhận hoàn cảnh khó khăn còn hiệu lực.

                3. Thời gian và cách nộp
                - Từ ngày 08/05/2026 đến hết ngày 22/05/2026.
                - Sinh viên upload hồ sơ tại mục "Chế độ chính sách" trên cổng thông tin sinh viên và nộp bản giấy cho Cố vấn học tập.

                Quá thời hạn trên, nhà trường không giải quyết bổ sung cho bất kỳ trường hợp nào.
                """,
                new DateTime(2026, 5, 8), ThongBaoDoiTuong.Student, laMoi: true),

            Tao(
                "Kế hoạch Tổ chức đăng ký học phần HK3 năm học 2025 - 2026",
                """
                Học kỳ 3 năm học 2025-2026 bắt đầu từ ngày 03/08/2026.

                - Đăng ký trực tuyến chính thức: từ 10/07/2026 đến 15/07/2026.
                - Đăng ký bổ sung: từ 16/07/2026 đến 18/07/2026.
                - Công bố thời khóa biểu: 25/07/2026.

                Sinh viên xem kỹ điều kiện tiên quyết của từng học phần trước khi đăng ký. Mọi phản hồi về trùng lịch, vượt sĩ lượng gửi về Phòng Đào tạo trong vòng 48 giờ kể từ khi công bố thời khóa biểu.
                """,
                new DateTime(2026, 4, 27), ThongBaoDoiTuong.Student),

            Tao(
                "Kế hoạch Tổ chức đăng ký học phần HK2 năm học 2025 - 2026",
                """
                - Đăng ký trực tuyến chính thức: từ 20/11/2025 đến 26/11/2025.
                - Đăng ký bổ sung: từ 27/11/2025 đến 30/11/2025.

                Sinh viên cần hoàn thành việc đánh giá giảng viên qua hệ thống lấy ý kiến trước khi được mở quyền đăng ký học phần.
                """,
                new DateTime(2025, 11, 19), ThongBaoDoiTuong.Student),

            Tao(
                "Sinh viên thực hiện cập nhật lý lịch sinh viên dự kiến từ ngày 13/9/2025",
                """
                Nhà trường yêu cầu toàn bộ sinh viên chính quy rà soát và cập nhật lý lịch sinh viên trên cổng thông tin.

                Các thông tin cần kiểm tra: họ tên, ngày sinh, nơi sinh, số định danh cá nhân, địa chỉ thường trú, số điện thoại liên hệ, thông tin người giám hộ và trình độ ngôn ngữ.

                Lý lịch chưa được xác nhận sẽ ảnh hưởng tới việc xét tốt nghiệp và cấp bằng. Sinh viên hoàn thành trước ngày 30/09/2025.
                """,
                new DateTime(2025, 9, 5), ThongBaoDoiTuong.Student),

            Tao(
                "Hướng dẫn đăng nhập Email và đăng nhập cổng thông tin sinh viên",
                """
                Tài khoản đăng nhập thống nhất (TDMU ID) gồm:
                - Tên đăng nhập: mã số sinh viên.
                - Mật khẩu: do hệ thống cấp lần đầu, sinh viên bắt buộc đổi khi đăng nhập đầu tiên.

                Nếu tài khoản bị khóa do nhập sai mật khẩu quá 5 lần, vui lòng liên hệ Bộ phận một cửa hoặc gửi yêu cầu tới địa chỉ hotro@tdmu.edu.vn để được mở khóa trong vòng 1 ngày làm việc.
                """,
                new DateTime(2025, 8, 22), ThongBaoDoiTuong.Student, laMoi: true),

            Tao(
                "Thông báo Tổ chức đăng ký học phần Học kỳ 1, Năm học 2025-2026",
                """
                Phòng Đào tạo thông báo lịch đăng ký học phần học kỳ 1 năm học 2025-2026 cho các khóa. Sinh viên đăng ký theo đúng khung giờ dành riêng cho khóa của mình để tránh nghẽn hệ thống.
                """,
                new DateTime(2025, 7, 18), ThongBaoDoiTuong.Student),

            Tao(
                "Thời gian các tiết học Bậc Đại học Chính quy và Quy chế sử dụng phòng học",
                """
                1. Thời gian tiết học (bậc đại học chính quy)
                - Sáng 1 (S1): 07:00 - 09:30
                - Sáng 2 (S2): 09:40 - 12:10
                - Chiều 1 (C1): 13:00 - 15:30
                - Chiều 2 (C2): 15:40 - 18:10
                - Tối 1: 18:20 - 20:50
                - Thực hành sáng (TH1): 07:00 - 11:30
                - Thực hành chiều (TH2): 13:00 - 17:30

                2. Quy định sử dụng phòng học
                - Giảng viên, sinh viên có mặt trước giờ bắt đầu tối thiểu 5 phút.
                - Không tự ý di chuyển máy chiếu, loa, bàn ghế giữa các phòng.
                - Trước khi rời phòng, tắt máy chiếu, điều hòa, đèn và đóng cửa phòng.
                - Mọi hư hỏng phát hiện trong ca học phải được báo cáo ngay qua mục "Báo cáo sự cố CSVC" của hệ thống để được ghi nhận và xử lý; nếu không báo cáo, lớp trưởng và giảng viên phụ trách ca học chịu trách nhiệm liên quan.
                """,
                new DateTime(2024, 9, 23), ThongBaoDoiTuong.All),

            Tao(
                "Quy định về việc dạy bù và đổi phòng học kỳ 1 năm học 2026-2027",
                """
                1. Đổi phòng học
                - Giảng viên gửi đề xuất đổi phòng trước ca dạy tối thiểu 02 ngày trên hệ thống.
                - Nhân viên đào tạo kiểm tra phòng trống và phê duyệt; hệ thống tự động cập nhật thời khóa biểu sau khi duyệt.

                2. Dạy bù
                - Đơn xin nghỉ có giảng viên dạy thay được ưu tiên xử lý trước.
                - Trường hợp dạy bù, giảng viên chủ động đăng ký phòng và ca học trên hệ thống trong vòng 07 ngày kể từ buổi nghỉ.

                3. Trách nhiệm
                - Giảng viên thông báo cho lớp về lịch học bù trong vòng 24 giờ sau khi được duyệt.
                - Các ca nghỉ không có đơn được ghi nhận là nghỉ không phép và ảnh hưởng tới đánh giá thi đua.
                """,
                new DateTime(2026, 10, 22), ThongBaoDoiTuong.Lecturer, laMoi: true),

            Tao(
                "Hướng dẫn sử dụng hệ thống báo cáo sự cố cơ sở vật chất mới",
                """
                Từ học kỳ 1 năm học 2026-2027, toàn bộ phản ánh về hư hỏng thiết bị được tiếp nhận qua hệ thống Quản lý phòng học, không tiếp nhận qua điện thoại hay tin nhắn riêng.

                Quy trình:
                1. Chọn mục "Báo cáo sự cố", chọn tòa nhà và phòng học.
                2. Chọn loại sự cố (thiết bị, bàn ghế, điều hòa, mạng, vệ sinh) và mức độ nghiêm trọng.
                3. Tích chọn chính xác thiết bị bị lỗi trong danh sách hiển thị.
                4. Mô tả ngắn gọn hiện trạng và gửi báo cáo.

                Trạng thái xử lý được cập nhật tự động: Mới -> Đang xử lý -> Đã khắc phục. Giảng viên nhận thông báo khi sự cố của ca học mình đóng.
                """,
                new DateTime(2026, 10, 15), ThongBaoDoiTuong.Lecturer),

            Tao(
                "Thông báo nâng cấp phần mềm trên máy tính phòng thực hành",
                """
                Trung tâm Thông tin - Học liệu phối hợp với Khoa Công nghệ Thông tin nâng cấp môi trường máy tính phòng thực hành.

                - Phòng B1-204: nâng cấp ngày 12/10/2026 (ca chiều và ca tối).
                - Phòng B1-205: nâng cấp ngày 13/10/2026 (cả ngày).

                Trong thời gian nâng cấp, các ca thực hành tại phòng bị hủy được chuyển sang phòng thay thế; giảng viên phụ trách theo dõi thông báo trên hệ thống và chủ động thông báo cho lớp.
                """,
                new DateTime(2026, 10, 10), ThongBaoDoiTuong.All),

            Tao(
                "Nhắc nhở tắt thiết bị điện sau khi kết thúc ca dạy",
                """
                Qua rà soát hóa đơn tiền điện học kỳ vừa qua, nhà trường ghi nhận nhiều phòng học vẫn bật máy chiếu, điều hòa và đèn sau ca tối.

                Đề nghị giảng viên và sinh viên lớp trưởng:
                - Tắt máy chiếu bằng remote, chờ quạt máy chiếu tắt hẳn rồi mới cắt điện.
                - Tắt điều hòa, đèn và đóng cửa sổ trước khi rời phòng.
                - Bàn giao chìa khóa phòng thực hành cho nhân viên quản lý tòa nhà.

                Tổ kiểm tra sẽ lập danh sách phòng vi phạm và công bố hằng tuần trên hệ thống.
                """,
                new DateTime(2026, 10, 5), ThongBaoDoiTuong.Lecturer),

            Tao(
                "BÀN GIAO VÀ ĐƯA VÀO VẬN HÀNH KHỐI PHÒNG HỌC MỚI KHU A2",
                """
                Trung tâm Quản lý Cơ sở vật chất thông báo đưa vào vận hành 12 phòng học lý thuyết và 4 phòng thực hành thuộc khối A2 từ ngày 01/11/2026.

                1. Hạ tầng đã nghiệm thu
                - Máy chiếu laser, hệ thống âm thanh 4 loa, điều hòa 2 chiều từng phòng.
                - Network switch và 06 điểm LAN/mạng không dây phủ toàn tầng.
                - Bàn ghế đạt chuẩn 60 chỗ, bảng trượt hai cánh.

                2. Trách nhiệm của nhân viên quản lý tòa nhà
                - Cập nhật danh mục phòng và thiết bị lên hệ thống trước ngày 28/10/2026.
                - Lập ca trực mở cửa, kiểm tra thiết bị đầu ca và khóa phòng cuối ca.
                - Ghi nhận toàn bộ tài sản bàn giao vào phiếu quản lý phòng.

                3. Lưu ý
                Các phòng A2 chỉ tiếp nhận lớp lý thuyết có sĩ số từ 50 sinh viên trở lên; đề xuất xếp phòng gửi về Phòng Đào tạo trước 05 ngày.
                """,
                new DateTime(2026, 10, 22), ThongBaoDoiTuong.Facility, laMoi: true, noiBat: true,
                anhDaiDien: "/img/news/thudaumot.jpg"),

            Tao(
                "KẾ HOẠCH BẢO TRÌ ĐỊNH KỲ HỆ THỐNG MÁY CHIẾU VÀ ĐIỀU HÒA",
                """
                Tổ Cơ điện - Điện lạnh triển khai bảo trì phòng ngừa định kỳ toàn bộ thiết bị phòng học trong giai đoạn từ 02/11/2026 đến 20/11/2026.

                1. Nội dung thực hiện
                - Vệ sinh buồng chiếu, kiểm tra tuổi thọ bóng và căn chỉnh độ sáng.
                - Bảo dưỡng dàn nóng, dàn lạnh, vệ sinh lưới lọc và bổ sung gas lạnh.
                - Siết lại hệ thống treo giá đỡ, kiểm tra dây nguồn và tiếp địa.

                2. Quy trình phối hợp
                - Nhân viên CSVC lập lịch bảo trì theo từng tòa nhà và cập nhật trạng thái "Bảo trì" trên hệ thống trước khi thực hiện 01 ngày.
                - Phòng đang bảo trì sẽ bị chặn xếp lịch tự động; các ca học bị ảnh hưởng do Đào tạo bố trí phòng thay thế.
                - Sau khi hoàn thành, xác nhận trạng thái thiết bị về "Tốt" và ghi chú linh kiện đã thay.

                3. Chỉ tiêu
                Hoàn thành 100% phòng học khu A, B trước ngày 20/11/2026 và báo cáo tổng hợp về Ban Giám hiệu.
                """,
                new DateTime(2026, 10, 18), ThongBaoDoiTuong.Facility, noiBat: true,
                anhDaiDien: "/img/news/phong-may-chieu.jpg"),

            Tao(
                "Hướng dẫn lập phiếu theo dõi tiêu thụ điện, nước và vật tư thay thế phòng học",
                """
                Từ học kỳ 1 năm học 2026-2027, mọi số liệu tiêu thụ và vật tư thay thế phải được ghi nhận trên hệ thống thay cho sổ giấy.

                - Nhập chỉ số công tơ điện, đồng hồ nước của từng tòa nhà vào ngày 28 hằng tháng.
                - Khi thay thế linh kiện, tạo phiếu vật tư ghi rõ mã thiết bị, đơn giá và người thực hiện.
                - Đối chiếu hóa đơn tập trung với tổng tiêu thụ toàn trường; chênh lệch quá 5% phải giải trình.

                Mẫu nhập liệu và danh mục vật tư chuẩn tải về từ mục "Thiết bị CSVC" của hệ thống.
                """,
                new DateTime(2026, 10, 8), ThongBaoDoiTuong.Facility),

            Tao(
                "KẾ HOẠCH RÀ SOÁT BỐ TRÍ PHÒNG HỌC VÀ GIẢNG ĐƯỜNG ĐỢT 2 HỌC KỲ 1",
                """
                Phòng Đào tạo tổ chức rà soát việc bố trí phòng học đợt 2 trước khi khóa đăng ký học phần học kỳ 2.

                1. Phạm vi rà soát
                - Sĩ số thực tế so với sức chứa được phép của từng phòng.
                - Các ca vượt sĩ lượng, các ca xếp phòng không đúng loại hình (lý thuyết / thực hành).
                - Phòng học trực tuyến và phòng học dùng chung giữa các khoa.

                2. Cách thức thực hiện
                - Nhân viên đào tạo xuất báo cáo ma trận thời khóa biểu theo tuần trên hệ thống.
                - Lập danh sách lớp cần chuyển phòng và phối hợp với CSVC kiểm tra phòng trống.
                - Hoàn tất điều chỉnh và công bố lại thời khóa biểu trước ngày 05/11/2026.

                3. Yêu cầu
                Không điều chỉnh thời khóa biểu đã công bố khi chưa có phê duyệt của Trưởng phòng Đào tạo.
                """,
                new DateTime(2026, 10, 20), ThongBaoDoiTuong.Training, laMoi: true, noiBat: true),

            Tao(
                "Quy định xử lý xung đột thời khóa biểu và phòng học trực tuyến",
                """
                1. Nguyên tắc xử lý
                - Một phòng chỉ được xếp tối đa một lớp trong cùng ca học; hệ thống chặn cứng trường hợp trùng lịch.
                - Xung đột phát sinh sau khi công bố phải xử lý trong vòng 24 giờ kể từ lúc ghi nhận.

                2. Thẩm quyền
                - Nhân viên đào tạo chủ động đổi phòng trong cùng tòa nhà nếu còn phòng trống cùng loại và đủ sĩ số.
                - Trường hợp phải đổi ca, đổi ngày hoặc hủy lớp trưởng phòng Đào tạo phê duyệt.

                3. Phòng học trực tuyến
                - Mỗi lớp trực tuyến gắn với mã phòng ảo và giảng viên phụ trách; không dùng chung tài khoản.
                - Kiểm tra kết nối và ghi hình tối thiểu 10 phút đầu ca để làm căn cứ xác nhận dạy - học.
                """,
                new DateTime(2026, 10, 12), ThongBaoDoiTuong.Training),

            Tao(
                "Tiếp nhận dữ liệu thời khóa biểu học kỳ 2 năm học 2026-2027 từ các khoa",
                """
                Các khoa gửi file thời khóa biểu học kỳ 2 theo mẫu Excel của trường về Phòng Đào tạo trước ngày 30/11/2026.

                Quy trình tiếp nhận trên hệ thống:
                1. Vào mục Import dữ liệu TKB, tải mẫu và điền đúng tiêu chí cột.
                2. Tải file lên; hệ thống kiểm tra trùng lịch, sĩ số vượt sức chứa và mã phòng không tồn tại.
                3. Xem danh sách lỗi cảnh báo, xử lý xong mới xác nhận nhập chính thức.

                Dữ liệu đã nhập có thể nhập lại nhiều lần trước khi khóa kỳ; sau khi khóa, mọi điều chỉnh thực hiện qua nghiệp vụ đổi phòng.
                """,
                new DateTime(2026, 10, 16), ThongBaoDoiTuong.Training)
        ];
    }
}
