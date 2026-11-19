using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <summary>
    /// Đồng bộ danh mục tòa nhà/phòng học theo sơ đồ khuôn viên: 21 tòa, 211 phòng.
    /// Chỉ xóa danh mục cũ khi còn mã phòng kiểu 'A2-301'; CSDL đã theo sơ đồ thì giữ nguyên.
    /// </summary>
    public partial class DongBoDanhMucPhongTheoSoDo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"-- Danh mục cũ dùng mã kiểu 'A2-301'. Khi phát hiện, xóa toàn bộ phòng cũ cùng dữ liệu phụ thuộc
-- (phiếu mượn, lịch học, sự cố, khung giờ, thiết bị) để nạp danh mục theo sơ đồ khuôn viên.
IF EXISTS (SELECT 1 FROM dbo.PhongHoc WHERE code_phong LIKE '%-%')
BEGIN
    DELETE FROM dbo.ChiTietSuCo;
    DELETE FROM dbo.SuCo;
    DELETE FROM dbo.DatPhong;
    DELETE FROM dbo.LichHoc;
    DELETE FROM dbo.Phong_ThietBi;
    DELETE FROM dbo.Phong_KhungTrong;
    DELETE FROM dbo.Phong_CaTrong;
    DELETE FROM dbo.PhongHoc;
    DELETE FROM dbo.ToaNha;
END

IF NOT EXISTS (SELECT 1 FROM dbo.PhongHoc WHERE code_phong = N'A3.101')
BEGIN
    SET IDENTITY_INSERT dbo.ToaNha ON;
    INSERT INTO dbo.ToaNha (ma_toa_nha, code_toa_nha, ten_toa_nha) VALUES
(1, 'A1', N'Tòa nhà Hành chính A1'),
(2, 'A2', N'Tòa nhà Lý thuyết A2'),
(3, 'A3', N'Tòa nhà A3'),
(4, 'A4', N'Tòa nhà A4'),
(5, 'B1', N'Tòa nhà B1'),
(6, 'C1', N'Tòa nhà C1'),
(7, 'D1', N'Tòa nhà D1'),
(8, 'E1', N'Tòa nhà E1'),
(9, 'E2', N'Tòa nhà E2'),
(10, 'E3', N'Tòa nhà E3'),
(11, 'F1', N'Tòa nhà F1'),
(12, 'F2', N'Tòa nhà F2'),
(13, 'F3', N'Tòa nhà F3'),
(14, 'H1', N'Tòa nhà H1'),
(15, 'H2', N'Tòa nhà H2'),
(16, 'I3', N'Tòa nhà I3'),
(17, 'I4', N'Tòa nhà I4'),
(18, 'J1', N'Tòa nhà J1'),
(19, 'K1', N'Tòa nhà K1'),
(20, 'K2', N'Tòa nhà K2'),
(21, 'K23', N'Tòa nhà K23');
    SET IDENTITY_INSERT dbo.ToaNha OFF;

    SET IDENTITY_INSERT dbo.PhongHoc ON;
    INSERT INTO dbo.PhongHoc (ma_phong, code_phong, ma_toa_nha, loai_phong, suc_chua, trang_thai_phong, hien_thi_dang_ky_muon) VALUES
(1, N'A1.101', 1, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(2, N'A1.102', 1, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(3, N'A2.101', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(4, N'A2.102', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(5, N'A2.103', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(6, N'A2.104', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(7, N'A2.105', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(8, N'A2.106', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(9, N'A2.107', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(10, N'A2.108', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(11, N'A2.109', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(12, N'A2.110', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(13, N'A2.111', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(14, N'A2.112', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(15, N'A2.113', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(16, N'A2.114', 2, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(17, N'A2.203', 2, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(18, N'A2.204', 2, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(19, N'A3.101', 3, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(20, N'A3.102', 3, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(21, N'A3.103', 3, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(22, N'A3.104', 3, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(23, N'A3.105', 3, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(24, N'A4.101', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(25, N'A4.102', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(26, N'A4.103', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(27, N'A4.104', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(28, N'A4.105', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(29, N'A4.106', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(30, N'A4.107', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(31, N'A4.108', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(32, N'A4.109', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(33, N'A4.110', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(34, N'A4.111', 4, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(35, N'B1.301', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(36, N'B1.302', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(37, N'B1.303', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(38, N'B1.304', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(39, N'B1.305', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(40, N'B1.401', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(41, N'B1.402', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(42, N'B1.403', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(43, N'B1.404', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(44, N'B1.405', 5, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(45, N'C1.101', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(46, N'C1.102', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(47, N'C1.103', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(48, N'C1.104', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(49, N'C1.201', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(50, N'C1.202', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(51, N'C1.203', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(52, N'C1.204', 6, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(53, N'D1.101', 7, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(54, N'D1.102', 7, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(55, N'D1.103', 7, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(56, N'D1.104', 7, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(57, N'D1.105', 7, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(58, N'D1.106', 7, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(59, N'E1.201', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(60, N'E1.202', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(61, N'E1.203', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(62, N'E1.204', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(63, N'E1.205', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(64, N'E1.206', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(65, N'E1.207', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(66, N'E1.208', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(67, N'E1.209', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(68, N'E1.210', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(69, N'E1.301', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(70, N'E1.302', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(71, N'E1.303', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(72, N'E1.304', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(73, N'E1.305', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(74, N'E1.306', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(75, N'E1.307', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(76, N'E1.308', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(77, N'E1.309', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(78, N'E1.310', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(79, N'E1.401', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(80, N'E1.402', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(81, N'E1.403', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(82, N'E1.404', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(83, N'E1.405', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(84, N'E1.406', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(85, N'E1.407', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(86, N'E1.408', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(87, N'E1.409', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(88, N'E1.410', 8, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(89, N'E2.101', 9, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(90, N'E2.102', 9, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(91, N'E2.103', 9, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(92, N'E2.104', 9, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(93, N'E3.101', 10, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(94, N'E3.102', 10, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(95, N'E3.103', 10, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(96, N'E3.104', 10, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(97, N'F1.101', 11, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(98, N'F1.102', 11, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(99, N'F1.103', 11, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(100, N'F2.101', 12, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(101, N'F2.102', 12, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(102, N'F2.103', 12, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(103, N'F3.101', 13, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(104, N'F3.102', 13, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(105, N'F3.103', 13, 'ROOM_LAB', 40, 'AVAILABLE', 1),
(106, N'H1.101', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(107, N'H1.102', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(108, N'H1.103', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(109, N'H1.201', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(110, N'H1.202', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(111, N'H1.203', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(112, N'H1.204', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(113, N'H1.301', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(114, N'H1.302', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(115, N'H1.303', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(116, N'H1.304', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(117, N'H1.401', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(118, N'H1.402', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(119, N'H1.403', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(120, N'H1.404', 14, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(121, N'H2.101', 15, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(122, N'H2.102', 15, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(123, N'H2.103', 15, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(124, N'H2.104', 15, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(125, N'I3.101', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(126, N'I3.102', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(127, N'I3.103', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(128, N'I3.104', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(129, N'I3.105', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(130, N'I3.106', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(131, N'I3.201', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(132, N'I3.202', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(133, N'I3.203', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(134, N'I3.204', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(135, N'I3.205', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(136, N'I3.206', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(137, N'I3.301', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(138, N'I3.302', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(139, N'I3.303', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(140, N'I3.304', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(141, N'I3.305', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(142, N'I3.306', 16, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(143, N'I4.107', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(144, N'I4.108', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(145, N'I4.109', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(146, N'I4.110', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(147, N'I4.111', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(148, N'I4.112', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(149, N'I4.113', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(150, N'I4.114', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(151, N'I4.207', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(152, N'I4.208', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(153, N'I4.209', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(154, N'I4.210', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(155, N'I4.211', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(156, N'I4.212', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(157, N'I4.213', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(158, N'I4.214', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(159, N'I4.307', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(160, N'I4.308', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(161, N'I4.309', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(162, N'I4.310', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(163, N'I4.311', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(164, N'I4.312', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(165, N'I4.313', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(166, N'I4.314', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(167, N'I4.407', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(168, N'I4.408', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(169, N'I4.409', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(170, N'I4.410', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(171, N'I4.411', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(172, N'I4.412', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(173, N'I4.413', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(174, N'I4.414', 17, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(175, N'J1.101', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(176, N'J1.102', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(177, N'J1.103', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(178, N'J1.104', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(179, N'J1.105', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(180, N'J1.201', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(181, N'J1.202', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(182, N'J1.203', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(183, N'J1.204', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(184, N'J1.205', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(185, N'J1.301', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(186, N'J1.302', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(187, N'J1.303', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(188, N'J1.304', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(189, N'J1.305', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(190, N'J1.401', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(191, N'J1.402', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(192, N'J1.403', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(193, N'J1.404', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(194, N'J1.405', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(195, N'J1.501', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(196, N'J1.502', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(197, N'J1.503', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(198, N'J1.504', 18, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(199, N'K1.101', 19, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(200, N'K1.102', 19, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(201, N'K1.103', 19, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(202, N'K1.104', 19, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(203, N'K1.105', 19, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(204, N'K1.106', 19, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(205, N'K2.201', 20, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(206, N'K23.101', 21, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(207, N'K23.102', 21, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(208, N'K23.201', 21, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(209, N'K23.202', 21, 'ROOM_THEORY', 50, 'AVAILABLE', 1),
(210, N'K23.203', 21, 'ROOM_THEORY', 50, 'AVAILABLE', 1);
SET IDENTITY_INSERT dbo.PhongHoc OFF;

    IF NOT EXISTS (SELECT 1 FROM dbo.ThietBi WHERE ten_thiet_bi = N'Máy chiếu Projector')
        INSERT INTO dbo.ThietBi (loai_phong, ten_thiet_bi) VALUES ('ROOM_THEORY', N'Máy chiếu Projector');
    IF NOT EXISTS (SELECT 1 FROM dbo.ThietBi WHERE ten_thiet_bi = N'Kính hiển vi quang học')
        INSERT INTO dbo.ThietBi (loai_phong, ten_thiet_bi) VALUES ('ROOM_LAB', N'Kính hiển vi quang học');

    INSERT INTO dbo.Phong_ThietBi (ma_phong, ma_thiet_bi, nhan_dien_thiet_bi, trang_thai)
    SELECT p.ma_phong, t.ma_thiet_bi, t.ten_thiet_bi + N' - ' + p.code_phong, 'GOOD'
    FROM dbo.PhongHoc p
    CROSS APPLY (
        SELECT TOP 1 ma_thiet_bi, ten_thiet_bi
        FROM dbo.ThietBi
        WHERE ten_thiet_bi = CASE WHEN p.loai_phong = 'ROOM_LAB' THEN N'Kính hiển vi quang học' ELSE N'Máy chiếu Projector' END
        ORDER BY ma_thiet_bi
    ) t
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Phong_ThietBi x WHERE x.ma_phong = p.ma_phong);

    -- Mỗi phòng mở sẵn 3 khung giờ mặc định để giảng viên/sinh viên đăng ký mượn ngay.
    INSERT INTO dbo.Phong_KhungTrong (ma_phong, gio_bat_dau, gio_ket_thuc)
    SELECT p.ma_phong, k.tu, k.den
    FROM dbo.PhongHoc p
    CROSS JOIN (VALUES
        (CAST('07:00' AS time), CAST('11:25' AS time)),
        (CAST('13:00' AS time), CAST('17:25' AS time)),
        (CAST('18:30' AS time), CAST('21:00' AS time))
    ) k(tu, den)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Phong_KhungTrong x
        WHERE x.ma_phong = p.ma_phong AND x.gio_bat_dau = k.tu
    );
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Danh mục cũ đã bị xóa cùng dữ liệu phụ thuộc nên không hoàn tác tự động.
            migrationBuilder.Sql("SELECT 1;");
        }
    }
}
