using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <summary>
    /// Chuẩn hóa cơ sở vật chất theo ngành: đổi tên tòa J1 thành I1 đúng sơ đồ,
    /// phân lại 211 phòng thành các loại phòng chuyên ngành, nạp thiết bị chuẩn cho
    /// từng loại phòng và gán nhóm ngành cho học phần để xếp lịch tự động.
    /// </summary>
    public partial class ChuanHoaCoSoVatChatTheoNganh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nhom_nganh",
                table: "HocPhan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(@"-- 1) Sơ đồ ghi I1 nhưng danh mục cũ dùng J1: đổi mã tòa và mã phòng cho khớp.
IF EXISTS (SELECT 1 FROM dbo.ToaNha WHERE code_toa_nha = 'J1')
    UPDATE dbo.ToaNha SET code_toa_nha = 'I1', ten_toa_nha = N'Tòa nhà I1' WHERE code_toa_nha = 'J1';

UPDATE dbo.PhongHoc
SET code_phong = N'I1.' + SUBSTRING(code_phong, 4, 50)
WHERE code_phong LIKE N'J1.%';

-- 2) Phân loại lại phòng theo chức năng đào tạo.
UPDATE dbo.PhongHoc SET loai_phong = 'ROOM_THEORY'
WHERE code_phong IN (N'A1.101', N'A1.102', N'C1.103', N'C1.104', N'C1.203', N'C1.204')
   OR code_phong LIKE N'A3.%'
   OR code_phong LIKE N'A4.%'
   OR code_phong LIKE N'D1.%'
   OR code_phong LIKE N'H1.%'
   OR code_phong LIKE N'I1.%'
   OR code_phong LIKE N'I3.%'
   OR code_phong LIKE N'I4.%';

UPDATE dbo.PhongHoc SET loai_phong = 'PHONG_MAY_TINH' WHERE code_phong LIKE N'E1.%';

UPDATE dbo.PhongHoc SET loai_phong = 'LAB_HOA_SINH'
WHERE code_phong IN (N'A2.101', N'A2.102', N'A2.103', N'A2.104', N'A2.105', N'A2.106', N'A2.107');

UPDATE dbo.PhongHoc SET loai_phong = 'LAB_CNTP'
WHERE code_phong IN (N'A2.108', N'A2.109', N'A2.110', N'A2.111', N'A2.112', N'A2.113', N'A2.114');

UPDATE dbo.PhongHoc SET loai_phong = 'LAB_DIEN_DIEN_TU'
WHERE code_phong LIKE N'E2.%' OR code_phong LIKE N'E3.%';

UPDATE dbo.PhongHoc SET loai_phong = 'XUONG_CO_KHI'
WHERE code_phong LIKE N'F2.%' OR code_phong LIKE N'F3.%';

UPDATE dbo.PhongHoc SET loai_phong = 'PHONG_LUYEN_THANH' WHERE code_phong IN (N'C1.101', N'C1.102');
UPDATE dbo.PhongHoc SET loai_phong = 'PHONG_HOA_TAU' WHERE code_phong IN (N'C1.201', N'C1.202');
UPDATE dbo.PhongHoc SET loai_phong = 'PHONG_HOA_VIEN_MY_THUAT' WHERE code_phong IN (N'H2.101', N'H2.102');
UPDATE dbo.PhongHoc SET loai_phong = 'PHONG_DO_AN_KIEN_TRUC' WHERE code_phong IN (N'H2.103', N'H2.104');

UPDATE dbo.PhongHoc SET loai_phong = 'PHONG_CHUC_NANG', hien_thi_dang_ky_muon = 0
WHERE code_phong IN (N'A2.203', N'A2.204', N'K2.201')
   OR code_phong LIKE N'B1.%'
   OR code_phong LIKE N'F1.%'
   OR code_phong LIKE N'K1.%'
   OR code_phong LIKE N'K23.%';

UPDATE dbo.PhongHoc SET hien_thi_dang_ky_muon = 1 WHERE loai_phong <> 'PHONG_CHUC_NANG';

-- 3) Sức chứa theo quy mô lớp thực tế: giảng đường lớn, phòng máy và xưởng vừa.
UPDATE dbo.PhongHoc SET suc_chua = 150 WHERE code_phong IN (N'A1.101', N'A1.102');
UPDATE dbo.PhongHoc SET suc_chua = 70  WHERE code_phong LIKE N'E1.%';
UPDATE dbo.PhongHoc SET suc_chua = 45  WHERE code_phong IN (N'A2.101', N'A2.102', N'A2.103', N'A2.104', N'A2.105', N'A2.106', N'A2.107');
UPDATE dbo.PhongHoc SET suc_chua = 40  WHERE code_phong IN (N'A2.108', N'A2.109', N'A2.110', N'A2.111', N'A2.112', N'A2.113', N'A2.114');
UPDATE dbo.PhongHoc SET suc_chua = 50  WHERE code_phong LIKE N'E2.%' OR code_phong LIKE N'E3.%';
UPDATE dbo.PhongHoc SET suc_chua = 40  WHERE code_phong LIKE N'F2.%' OR code_phong LIKE N'F3.%';
UPDATE dbo.PhongHoc SET suc_chua = 60  WHERE code_phong IN (N'C1.101', N'C1.102', N'C1.201', N'C1.202');
UPDATE dbo.PhongHoc SET suc_chua = 40  WHERE code_phong IN (N'H2.101', N'H2.102', N'H2.103', N'H2.104');
UPDATE dbo.PhongHoc SET suc_chua = 60  WHERE code_phong LIKE N'A3.%' OR code_phong LIKE N'A4.%' OR code_phong LIKE N'H1.%' OR code_phong LIKE N'I3.%' OR code_phong LIKE N'I4.%';
UPDATE dbo.PhongHoc SET suc_chua = 50  WHERE code_phong LIKE N'D1.%' OR code_phong IN (N'C1.103', N'C1.104', N'C1.203', N'C1.204');

-- 4) Bổ sung sân thể dục ngoài trời (không thuộc tòa nhà nào).
IF NOT EXISTS (SELECT 1 FROM dbo.ToaNha WHERE code_toa_nha = 'SV')
BEGIN
    SET IDENTITY_INSERT dbo.ToaNha ON;
    INSERT INTO dbo.ToaNha (ma_toa_nha, code_toa_nha, ten_toa_nha) VALUES (22, 'SV', N'Khu sân thể dục');
    SET IDENTITY_INSERT dbo.ToaNha OFF;
END

IF NOT EXISTS (SELECT 1 FROM dbo.PhongHoc WHERE code_phong = N'SV.01')
BEGIN
    SET IDENTITY_INSERT dbo.PhongHoc ON;
    INSERT INTO dbo.PhongHoc (ma_phong, code_phong, ma_toa_nha, loai_phong, suc_chua, trang_thai_phong, hien_thi_dang_ky_muon)
    VALUES (212, N'SV.01', (SELECT ma_toa_nha FROM dbo.ToaNha WHERE code_toa_nha = 'SV'), 'SAN_THE_DUC_NGOAI_TROI', 200, 'AVAILABLE', 1);
    SET IDENTITY_INSERT dbo.PhongHoc OFF;
END

-- 5) Khung giờ phải phủ trọn ca lý thuyết 3 tiết lẫn ca thực hành 5 tiết.
UPDATE dbo.Phong_KhungTrong SET gio_ket_thuc = '12:15:00'
WHERE gio_bat_dau = '07:00:00' AND gio_ket_thuc = '11:25:00';

UPDATE dbo.Phong_KhungTrong SET gio_ket_thuc = '18:15:00'
WHERE gio_bat_dau = '13:00:00' AND gio_ket_thuc = '17:25:00';

UPDATE dbo.Phong_KhungTrong SET gio_bat_dau = '18:20:00'
WHERE gio_bat_dau = '18:30:00' AND gio_ket_thuc = '21:00:00';

IF NOT EXISTS (SELECT 1 FROM dbo.Phong_KhungTrong k
               JOIN dbo.PhongHoc p ON p.ma_phong = k.ma_phong
               WHERE p.code_phong = N'SV.01')
BEGIN
    INSERT INTO dbo.Phong_KhungTrong (ma_phong, gio_bat_dau, gio_ket_thuc)
    SELECT ma_phong, '07:00:00', '11:30:00' FROM dbo.PhongHoc WHERE code_phong = N'SV.01'
    UNION ALL
    SELECT ma_phong, '13:00:00', '17:30:00' FROM dbo.PhongHoc WHERE code_phong = N'SV.01';
END

-- 6) Chuyển các thiết bị cũ sang mã loại phòng mới.
UPDATE dbo.ThietBi SET loai_phong = 'PHONG_MAY_TINH'
WHERE loai_phong = 'ROOM_PRACTICE' AND ten_thiet_bi IN (N'Máy tính bàn (Workstation)', N'Chuột & Bàn phím');

UPDATE dbo.ThietBi SET loai_phong = 'LAB_DIEN_DIEN_TU'
WHERE loai_phong = 'ROOM_PRACTICE' AND ten_thiet_bi IN (N'Bộ thực hành vi điều khiển PLC', N'Máy hàn điện tử chuyên dụng');

UPDATE dbo.ThietBi SET loai_phong = 'PHONG_DO_AN_KIEN_TRUC'
WHERE loai_phong = 'ROOM_PRACTICE' AND ten_thiet_bi IN (N'Máy in 3D công nghiệp', N'Bàn vẽ kỹ thuật chuyên dụng');

UPDATE dbo.ThietBi SET loai_phong = 'LAB_HOA_SINH' WHERE loai_phong = 'ROOM_LAB';
UPDATE dbo.ThietBi SET loai_phong = 'SAN_THE_DUC_NGOAI_TROI' WHERE loai_phong = 'ROOM_SPORT';

-- 7) Danh mục thiết bị chuẩn theo từng loại phòng.
DECLARE @dm TABLE (loai_phong varchar(50) NOT NULL, ten_thiet_bi nvarchar(100) NOT NULL);

INSERT INTO @dm (loai_phong, ten_thiet_bi) VALUES
('ROOM_THEORY', N'Máy chiếu Projector'),
('ROOM_THEORY', N'Màn chiếu'),
('ROOM_THEORY', N'Bảng viết'),
('ROOM_THEORY', N'Hệ thống Loa/Micro'),
('ROOM_THEORY', N'Điều hòa'),
('ROOM_THEORY', N'Bàn ghế sinh viên'),
('ROOM_THEORY', N'Bàn giáo viên'),

('PHONG_MAY_TINH', N'Máy tính để bàn'),
('PHONG_MAY_TINH', N'Màn hình'),
('PHONG_MAY_TINH', N'Bàn phím & chuột'),
('PHONG_MAY_TINH', N'Máy chiếu Projector'),
('PHONG_MAY_TINH', N'Điều hòa'),
('PHONG_MAY_TINH', N'Bàn máy tính'),
('PHONG_MAY_TINH', N'Ghế xoay'),
('PHONG_MAY_TINH', N'Switch mạng'),

('LAB_HOA_SINH', N'Tủ hút khí độc (Fume Hood)'),
('LAB_HOA_SINH', N'Bồn rửa hóa chất'),
('LAB_HOA_SINH', N'Kính hiển vi quang học'),
('LAB_HOA_SINH', N'Cân phân tích'),
('LAB_HOA_SINH', N'Máy đo pH'),
('LAB_HOA_SINH', N'Bộ chưng cất'),
('LAB_HOA_SINH', N'Tủ an toàn sinh học'),
('LAB_HOA_SINH', N'Hệ thống thông gió'),

('LAB_CNTP', N'Tủ lên men'),
('LAB_CNTP', N'Nồi hấp tiệt trùng'),
('LAB_CNTP', N'Tủ sấy'),
('LAB_CNTP', N'Máy đo độ ẩm'),
('LAB_CNTP', N'Cân phân tích'),
('LAB_CNTP', N'Kính hiển vi quang học'),
('LAB_CNTP', N'Bồn rửa'),
('LAB_CNTP', N'Tủ hút khí độc (Fume Hood)'),

('LAB_DIEN_DIEN_TU', N'Bàn thực hành mạch điện'),
('LAB_DIEN_DIEN_TU', N'Bộ vi điều khiển'),
('LAB_DIEN_DIEN_TU', N'PLC'),
('LAB_DIEN_DIEN_TU', N'Máy hiện sóng Oscilloscope'),
('LAB_DIEN_DIEN_TU', N'Mỏ hàn'),
('LAB_DIEN_DIEN_TU', N'Đồng hồ vạn năng'),
('LAB_DIEN_DIEN_TU', N'Nguồn DC'),
('LAB_DIEN_DIEN_TU', N'Bộ linh kiện điện tử'),

('XUONG_CO_KHI', N'Máy tiện'),
('XUONG_CO_KHI', N'Máy phay'),
('XUONG_CO_KHI', N'Máy CNC'),
('XUONG_CO_KHI', N'Mô hình động cơ ô tô'),
('XUONG_CO_KHI', N'Cầu nâng ô tô'),
('XUONG_CO_KHI', N'Máy khoan'),
('XUONG_CO_KHI', N'Bộ dụng cụ cầm tay'),
('XUONG_CO_KHI', N'Hệ thống khí nén'),

('PHONG_LUYEN_THANH', N'Đàn Piano'),
('PHONG_LUYEN_THANH', N'Micro'),
('PHONG_LUYEN_THANH', N'Loa kiểm âm'),
('PHONG_LUYEN_THANH', N'Gương soi toàn thân'),
('PHONG_LUYEN_THANH', N'Tấm tiêu âm'),
('PHONG_LUYEN_THANH', N'Sàn gỗ chuyên dụng'),
('PHONG_LUYEN_THANH', N'Bộ gõ'),

('PHONG_HOA_TAU', N'Đàn Piano'),
('PHONG_HOA_TAU', N'Bộ gõ'),
('PHONG_HOA_TAU', N'Micro'),
('PHONG_HOA_TAU', N'Loa kiểm âm'),
('PHONG_HOA_TAU', N'Tấm tiêu âm'),
('PHONG_HOA_TAU', N'Sàn gỗ chuyên dụng'),

('PHONG_HOA_VIEN_MY_THUAT', N'Giá vẽ (Easel)'),
('PHONG_HOA_VIEN_MY_THUAT', N'Bàn vẽ A0/A1'),
('PHONG_HOA_VIEN_MY_THUAT', N'Tượng thạch cao mẫu'),
('PHONG_HOA_VIEN_MY_THUAT', N'Đèn chiếu điểm Spotlight'),
('PHONG_HOA_VIEN_MY_THUAT', N'Bồn rửa cọ'),

('PHONG_DO_AN_KIEN_TRUC', N'Bàn đồ án A0'),
('PHONG_DO_AN_KIEN_TRUC', N'Giá vẽ (Easel)'),
('PHONG_DO_AN_KIEN_TRUC', N'Máy in 3D'),
('PHONG_DO_AN_KIEN_TRUC', N'Máy cắt Laser'),
('PHONG_DO_AN_KIEN_TRUC', N'Đèn chiếu điểm Spotlight'),
('PHONG_DO_AN_KIEN_TRUC', N'Tủ mô hình'),

('SAN_THE_DUC_NGOAI_TROI', N'Đèn chiếu sáng sân'),
('SAN_THE_DUC_NGOAI_TROI', N'Cột bóng rổ'),
('SAN_THE_DUC_NGOAI_TROI', N'Khung thành bóng đá'),
('SAN_THE_DUC_NGOAI_TROI', N'Lưới chắn bóng'),
('SAN_THE_DUC_NGOAI_TROI', N'Hệ thống thoát nước sân'),

('PHONG_CHUC_NANG', N'Bàn làm việc'),
('PHONG_CHUC_NANG', N'Máy tính văn phòng'),
('PHONG_CHUC_NANG', N'Máy in'),
('PHONG_CHUC_NANG', N'Điều hòa'),
('PHONG_CHUC_NANG', N'Tủ hồ sơ');

INSERT INTO dbo.ThietBi (loai_phong, ten_thiet_bi)
SELECT dm.loai_phong, dm.ten_thiet_bi
FROM @dm dm
WHERE NOT EXISTS (SELECT 1 FROM dbo.ThietBi t
                  WHERE t.loai_phong = dm.loai_phong AND t.ten_thiet_bi = dm.ten_thiet_bi);

-- Gán thiết bị chuẩn cho mọi phòng; chi tiết sự cố cũ được dọn trước để không mồ côi.
DELETE FROM dbo.ChiTietSuCo;
DELETE FROM dbo.Phong_ThietBi;

INSERT INTO dbo.Phong_ThietBi (ma_phong, ma_thiet_bi, nhan_dien_thiet_bi, trang_thai)
SELECT p.ma_phong, t.ma_thiet_bi, t.ten_thiet_bi, 'GOOD'
FROM dbo.PhongHoc p
JOIN @dm dm ON dm.loai_phong = p.loai_phong
JOIN dbo.ThietBi t ON t.loai_phong = dm.loai_phong AND t.ten_thiet_bi = dm.ten_thiet_bi;

-- 8) Gán nhóm ngành cho học phần để ghép lịch với đúng họ phòng chuyên ngành.
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_CNTT'
WHERE ma_hoc_phan LIKE 'AI%' OR ma_hoc_phan LIKE 'DAT%' OR ma_hoc_phan LIKE 'INT%'
   OR ma_hoc_phan LIKE 'NET%' OR ma_hoc_phan LIKE 'SE%' OR ma_hoc_phan LIKE 'SEC%'
   OR ma_hoc_phan LIKE 'WEB%';

UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_HOA_SINH' WHERE ma_hoc_phan LIKE 'CHE%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_CNTP' WHERE ma_hoc_phan LIKE 'FOD%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_DIEN' WHERE ma_hoc_phan LIKE 'ELE%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_CO_KHI' WHERE ma_hoc_phan LIKE 'MEC%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_AM_NHAC', la_thuc_hanh = 1 WHERE ma_hoc_phan LIKE 'MUS%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_KIEN_TRUC', la_thuc_hanh = 1 WHERE ma_hoc_phan LIKE 'ARC%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_THE_THAO' WHERE ma_hoc_phan LIKE 'PED%';
UPDATE dbo.HocPhan SET nhom_nganh = 'NGANH_DAI_CUONG' WHERE nhom_nganh IS NULL;

-- 9) Bổ sung học phần mẫu cho Công nghệ Thực phẩm và Giáo dục thể chất.
DECLARE @gv nvarchar(50) = (SELECT TOP 1 ma_nguoi_dung FROM dbo.NguoiDung WHERE ma_nguoi_dung = 'GV1006');

IF NOT EXISTS (SELECT 1 FROM dbo.HocPhan WHERE ma_hoc_phan = 'FOD201_01')
    INSERT INTO dbo.HocPhan (ma_hoc_phan, ten_hoc_phan, so_luong_dang_ky, ma_giang_vien, la_thuc_hanh, nhom_nganh)
    VALUES ('FOD201_01', N'Công nghệ chế biến thực phẩm', 35, @gv, 1, 'NGANH_CNTP');

IF NOT EXISTS (SELECT 1 FROM dbo.HocPhan WHERE ma_hoc_phan = 'PED101_01')
    INSERT INTO dbo.HocPhan (ma_hoc_phan, ten_hoc_phan, so_luong_dang_ky, ma_giang_vien, la_thuc_hanh, nhom_nganh)
    VALUES ('PED101_01', N'Giáo dục thể chất', 60, @gv, 1, 'NGANH_THE_THAO');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM dbo.PhongHoc WHERE code_phong = N'SV.01')
BEGIN
    DELETE FROM dbo.Phong_KhungTrong WHERE ma_phong = (SELECT ma_phong FROM dbo.PhongHoc WHERE code_phong = N'SV.01');
    DELETE FROM dbo.Phong_ThietBi WHERE ma_phong = (SELECT ma_phong FROM dbo.PhongHoc WHERE code_phong = N'SV.01');
    DELETE FROM dbo.PhongHoc WHERE code_phong = N'SV.01';
END

IF EXISTS (SELECT 1 FROM dbo.ToaNha WHERE code_toa_nha = 'SV')
    DELETE FROM dbo.ToaNha WHERE code_toa_nha = 'SV';

UPDATE dbo.PhongHoc SET code_phong = N'J1.' + SUBSTRING(code_phong, 4, 50) WHERE code_phong LIKE N'I1.%';
IF EXISTS (SELECT 1 FROM dbo.ToaNha WHERE code_toa_nha = 'I1')
    UPDATE dbo.ToaNha SET code_toa_nha = 'J1', ten_toa_nha = N'Tòa nhà J1' WHERE code_toa_nha = 'I1';");

            migrationBuilder.DropColumn(
                name: "nhom_nganh",
                table: "HocPhan");
        }
    }
}
