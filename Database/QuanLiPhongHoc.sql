-- SSMS bật sẵn hai tùy chọn này, còn sqlcmd thì mặc định QUOTED_IDENTIFIER OFF và sẽ
-- báo lỗi Msg 1934 ở dòng CREATE UNIQUE INDEX ... WHERE bên dưới. Giữ hai dòng đầu
-- để script chạy được trọn vẹn bằng cả SSMS lẫn sqlcmd.
--
-- File này phải lưu ở UTF-8 CÓ BOM. sqlcmd chỉ nhận diện UTF-8 nhờ BOM; mất BOM là nó
-- đọc theo codepage ANSI và mọi dấu tiếng Việt trong N'...' sẽ thành "Ná»¯", "Ã".


CREATE DATABASE QuanLiPhongHoc;
GO

USE QuanLiPhongHoc;
GO

CREATE TABLE NguoiDung (
    ma_nguoi_dung VARCHAR(50) PRIMARY KEY,
    ho_ten NVARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    mat_khau VARCHAR(255) NOT NULL,
    bi_khoa BIT DEFAULT 0,
    ngay_tao DATETIME DEFAULT GETDATE(),
    so_dien_thoai NVARCHAR(20) NULL,
    ngay_sinh DATETIME NULL,
    gioi_tinh NVARCHAR(20) NULL,
    noi_sinh NVARCHAR(200) NULL
);

CREATE TABLE VaiTro (
    ma_vai_tro INT IDENTITY(1,1) PRIMARY KEY,
    code_vai_tro VARCHAR(50) UNIQUE NOT NULL,
    ten_vai_tro NVARCHAR(100)
);

CREATE TABLE NguoiDung_VaiTro (
    ma_nguoi_dung VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung) ON DELETE CASCADE,
    ma_vai_tro INT REFERENCES VaiTro(ma_vai_tro) ON DELETE CASCADE,
    PRIMARY KEY (ma_nguoi_dung, ma_vai_tro)
);

CREATE TABLE ToaNha (
    ma_toa_nha INT IDENTITY(1,1) PRIMARY KEY,
    code_toa_nha NVARCHAR(20) UNIQUE NOT NULL,
    ten_toa_nha NVARCHAR(100),
    dia_chi NVARCHAR(255),
    so_tang INT,
    -- ACTIVE = đang hoạt động, MAINTENANCE = đang bảo trì, INACTIVE = ngừng hoạt động.
    trang_thai_hoat_dong NVARCHAR(30) NOT NULL DEFAULT N'ACTIVE'
);

CREATE TABLE PhongHoc (
    ma_phong INT IDENTITY(1,1) PRIMARY KEY,
    code_phong NVARCHAR(50) UNIQUE NOT NULL,
    ma_toa_nha INT REFERENCES ToaNha(ma_toa_nha) ON DELETE CASCADE,
    loai_phong NVARCHAR(50) NOT NULL,
    suc_chua INT NOT NULL,
    trang_thai_phong NVARCHAR(30) DEFAULT N'Đang hoạt động',
    -- 1 = phòng hiện trên trang đăng ký mượn của giảng viên/sinh viên; 0 = ẩn hoàn toàn.
    hien_thi_dang_ky_muon BIT NOT NULL DEFAULT 1
);

CREATE TABLE ThietBi (
    ma_thiet_bi INT IDENTITY(1,1) PRIMARY KEY,
    loai_phong NVARCHAR(50),
    ten_thiet_bi NVARCHAR(100) NOT NULL,
    ma_code_thiet_bi NVARCHAR(30)
);

CREATE UNIQUE INDEX idx_thietbi_ma_code ON ThietBi(ma_code_thiet_bi)
    WHERE ma_code_thiet_bi IS NOT NULL;

CREATE TABLE Phong_ThietBi (
    ma_phong_thiet_bi INT IDENTITY(1,1) PRIMARY KEY,
    ma_phong INT REFERENCES PhongHoc(ma_phong) ON DELETE CASCADE,
    ma_thiet_bi INT REFERENCES ThietBi(ma_thiet_bi) ON DELETE CASCADE,
    nhan_dien_thiet_bi NVARCHAR(50),
    trang_thai NVARCHAR(30) DEFAULT N'Bình thường'
);

-- Khung giờ trống nhân viên cơ sở vật chất khai cho từng phòng (không bó theo ca).
-- Phòng chưa khai khung nào thì trang đăng ký mượn dùng 3 khung giờ mặc định.
CREATE TABLE Phong_KhungTrong (
    ma_khung_trong INT IDENTITY(1,1) PRIMARY KEY,
    ma_phong INT NOT NULL REFERENCES PhongHoc(ma_phong) ON DELETE CASCADE,
    gio_bat_dau TIME NOT NULL,
    gio_ket_thuc TIME NOT NULL,
    CONSTRAINT UQ_Phong_KhungTrong UNIQUE (ma_phong, gio_bat_dau)
);

CREATE TABLE HocPhan (
    ma_hoc_phan VARCHAR(50) PRIMARY KEY,
    ten_hoc_phan NVARCHAR(150) NOT NULL,
    so_luong_dang_ky INT NOT NULL,
    ma_giang_vien VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    la_thuc_hanh BIT DEFAULT 0,
    nhom_nganh VARCHAR(50) NULL
);

CREATE TABLE LichHoc (
    ma_lich_hoc INT IDENTITY(1,1) PRIMARY KEY,
    ma_hoc_phan VARCHAR(50) REFERENCES HocPhan(ma_hoc_phan) ON DELETE CASCADE,
    ma_phong INT REFERENCES PhongHoc(ma_phong) ON DELETE CASCADE,
    ngay_hoc DATE NOT NULL,
    ca_hoc NVARCHAR(30) NOT NULL,
    trang_thai_hoat_dong BIT DEFAULT 1,
    -- Điều phối thủ công khi giảng viên vắng: người dạy thay và ghi chú của phòng đào tạo.
    ma_giang_vien_day_thay VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung) NULL,
    ghi_chu_dieu_phoi NVARCHAR(500)
);

CREATE INDEX idx_lichhoc_giangvien_thay ON LichHoc(ma_giang_vien_day_thay);

CREATE TABLE DatPhong (
    ma_dat_phong INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_yeu_cau VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    ma_phong INT REFERENCES PhongHoc(ma_phong) ON DELETE CASCADE,
    ngay_dat DATE NOT NULL,
    gio_bat_dau TIME NOT NULL,
    gio_ket_thuc TIME NOT NULL,
    muc_dich NVARCHAR(MAX),
    so_luong_nguoi_tham_gia INT,
    trang_thai NVARCHAR(30) DEFAULT N'Chờ duyệt',
    nguoi_duyet VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    -- Thời điểm CSVC duyệt/từ chối và cờ người gửi đã xem kết quả trong mục Phiếu đã gửi.
    thoi_diem_xu_ly DATETIME2 NULL,
    da_xem_ket_qua BIT NOT NULL DEFAULT 0
);

CREATE TABLE SuCo (
    ma_su_co INT IDENTITY(1,1) PRIMARY KEY,
    ma_phong INT REFERENCES PhongHoc(ma_phong) ON DELETE CASCADE,
    nguoi_bao_cao VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    loai_su_co NVARCHAR(50),
    mo_ta NVARCHAR(MAX),
    muc_do_nghiem_trong NVARCHAR(20),
    trang_thai NVARCHAR(30) DEFAULT N'Mới tạo',
    ngay_tao DATETIME DEFAULT GETDATE(),
    nguoi_xu_ly VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    ngay_xu_ly DATETIME2 NULL,
    danh_gia NVARCHAR(MAX),
    ket_qua_xu_ly NVARCHAR(30),
    phong_dieu_chuyen INT NULL REFERENCES PhongHoc(ma_phong),
    thong_bao_phan_hoi NVARCHAR(500),
    da_xem_phan_hoi BIT NOT NULL DEFAULT 0,
    doi_tuong_bao_cao NVARCHAR(20),
    ho_ten_nguoi_bao_cao NVARCHAR(100),
    so_dien_thoai_lien_he NVARCHAR(20),
    noi_dung_gop_y NVARCHAR(1000),
    anh_dinh_kem NVARCHAR(255)
);

CREATE TABLE ChiTietSuCo (
    ma_chi_tiet_su_co INT IDENTITY(1,1) PRIMARY KEY,
    ma_su_co INT REFERENCES SuCo(ma_su_co),
    ma_phong_thiet_bi INT REFERENCES Phong_ThietBi(ma_phong_thiet_bi) ON DELETE SET NULL,
    ghi_chu_loi NVARCHAR(255)
);

-- Phiếu yêu cầu xử lý do CSVC lập khi cử nhân viên đến sửa thiết bị.
CREATE TABLE YeuCauSuaChua (
    ma_yeu_cau INT IDENTITY(1,1) PRIMARY KEY,
    ma_phong_thiet_bi INT NULL REFERENCES Phong_ThietBi(ma_phong_thiet_bi) ON DELETE SET NULL,
    ma_phong INT NULL REFERENCES PhongHoc(ma_phong),
    nguoi_yeu_cau VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    nhan_vien_xu_ly NVARCHAR(100),
    noi_dung NVARCHAR(MAX) NOT NULL,
    ngay_hen DATETIME2 NULL,
    trang_thai NVARCHAR(30) NOT NULL DEFAULT N'MOI',
    ngay_tao DATETIME2 NOT NULL DEFAULT GETDATE(),
    ngay_hoan_thanh DATETIME2 NULL,
    ket_qua NVARCHAR(MAX)
);

CREATE INDEX idx_yeucausuachua_thietbi ON YeuCauSuaChua(ma_phong_thiet_bi);
CREATE INDEX idx_yeucausuachua_trangthai ON YeuCauSuaChua(trang_thai);

CREATE TABLE DonXinNghi (
    ma_don INT IDENTITY(1,1) PRIMARY KEY,
    ma_giang_vien VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    ma_hoc_phan VARCHAR(50) REFERENCES HocPhan(ma_hoc_phan),
    ly_do NVARCHAR(MAX) NOT NULL,
    phuong_an_giai_quyet NVARCHAR(50) NOT NULL,
    ma_giang_vien_day_thay VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung) NULL,
    trang_thai NVARCHAR(30) DEFAULT N'Chờ duyệt',
    ngay_tao DATETIME DEFAULT GETDATE(),
    nguoi_duyet VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung) NULL
);

CREATE TABLE NhatKyHeThong (
    ma_nhat_ky INT IDENTITY(1,1) PRIMARY KEY,
    thoi_gian DATETIME DEFAULT GETDATE(),
    ma_nguoi_thuc_hien VARCHAR(50) REFERENCES NguoiDung(ma_nguoi_dung),
    loai_hanh_dong NVARCHAR(50) NOT NULL,
    doi_tuong_tac_dong NVARCHAR(100),
    du_lieu_cu NVARCHAR(MAX),
    du_lieu_moi NVARCHAR(MAX),
    dia_chi_ip VARCHAR(45)
);

CREATE TABLE ThongBao (
    ma_thong_bao INT IDENTITY(1,1) PRIMARY KEY,
    tieu_de NVARCHAR(255) NOT NULL,
    noi_dung NVARCHAR(MAX) NOT NULL,
    anh_dai_dien NVARCHAR(255) NULL,
    doi_tuong VARCHAR(50) NOT NULL DEFAULT 'ALL',
    la_moi BIT NOT NULL DEFAULT 0,
    noi_bat BIT NOT NULL DEFAULT 0,
    trang_thai NVARCHAR(30) NOT NULL DEFAULT 'PUBLISHED',
    ngay_dang DATETIME NOT NULL DEFAULT GETDATE(),
    nguoi_dang VARCHAR(50) NULL
);

CREATE UNIQUE INDEX idx_unique_phong_lichhoc
ON LichHoc(ma_phong, ngay_hoc, ca_hoc)
WHERE trang_thai_hoat_dong = 1;

CREATE INDEX idx_lichhoc_giangvien ON LichHoc(ngay_hoc, ca_hoc);
CREATE INDEX idx_nhatky_thoigian ON NhatKyHeThong(thoi_gian);
CREATE INDEX idx_phongthietbi_phong ON Phong_ThietBi(ma_phong);
CREATE INDEX idx_thongbao_hien_thi ON ThongBao(trang_thai, ngay_dang);

--------------------------------------------------------------------------------
-- 1. BẢNG VAI TRÒ (VaiTro)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT VaiTro ON;
INSERT INTO VaiTro (ma_vai_tro, code_vai_tro, ten_vai_tro) VALUES
(1, 'ROLE_STUDENT', N'Sinh viên'),
(2, 'ROLE_LECTURER', N'Giảng viên'),
(3, 'ROLE_FACILITY', N'Nhân viên cơ sở vật chất'),
(4, 'ROLE_TRAINING', N'Nhân viên đào tạo'),
(5, 'ROLE_ADMIN', N'Quản trị hệ thống'),
(6, 'ROLE_HEAD_DEPT', N'Trưởng khoa'),
(7, 'ROLE_DEPUTY_HEAD', N'Phó trưởng khoa'),
(8, 'ROLE_LAB_ASSISTANT', N'Trợ lý phòng lab'),
(9, 'ROLE_TA', N'Trợ giảng'),
(10, 'ROLE_STUDENT_UNION', N'Cán bộ đoàn hội'),
(11, 'ROLE_SECURITY', N'Nhân viên an ninh'),
(12, 'ROLE_IT_SUPPORT', N'Hỗ trợ kỹ thuật IT'),
(13, 'ROLE_LIBRARY_STAFF', N'Nhân viên thư viện'),
(14, 'ROLE_FINANCE_STAFF', N'Nhân viên tài chính'),
(15, 'ROLE_ADMISSION_STAFF', N'Nhân viên tuyển sinh'),
(16, 'ROLE_RESEARCHER', N'Nghiên cứu viên'),
(17, 'ROLE_GUEST_LECTURER', N'Giảng viên thỉnh giảng'),
(18, 'ROLE_INSPECTOR', N'Thanh tra giáo dục'),
(19, 'ROLE_ALUMNI', N'Cựu sinh viên'),
(20, 'ROLE_PARTNER', N'Đơn vị đối tác');
SET IDENTITY_INSERT VaiTro OFF;

--------------------------------------------------------------------------------
-- 2. BẢNG NGƯỜI DÙNG (NguoiDung)
--------------------------------------------------------------------------------
-- so_dien_thoai / ngay_sinh / gioi_tinh / noi_sinh là hồ sơ mẫu của từng tài khoản,
-- giữ đúng thứ tự và giá trị như Data/HoSoSeedData.cs để CSDL dựng bằng script
-- hoặc bằng app cho ra cùng một dữ liệu.
-- Mật khẩu mẫu ghi dạng thô trong script; lần khởi động đầu tiên DbSeeder tự băm lại
-- bằng PBKDF2-SHA256 nên CSDL thực tế không lưu mật khẩu đọc được.
INSERT INTO NguoiDung (ma_nguoi_dung, ho_ten, email, mat_khau, bi_khoa, so_dien_thoai, ngay_sinh, gioi_tinh, noi_sinh) VALUES
('SV202601', N'Nguyễn Văn Sinh', 'sv202601@school.edu.vn', '123456', 0, '0912345601', '2004-03-15', N'Nam', N'Bình Dương'),
('SV202602', N'Trần Thị Mai', 'sv202602@school.edu.vn', '123456', 0, '0912345602', '2004-07-22', N'Nữ', N'Tây Ninh'),
('SV202603', N'Lê Hoàng Long', 'sv202603@school.edu.vn', '123456', 0, '0912345603', '2003-11-05', N'Nam', N'TP. Hồ Chí Minh'),
('SV202604', N'Phạm Thị Hương', 'sv202604@school.edu.vn', '123456', 0, '0912345604', '2004-01-18', N'Nữ', N'Đồng Nai'),
('SV202605', N'Hoàng Văn Nam', 'sv202605@school.edu.vn', '123456', 0, '0912345605', '2004-09-30', N'Nam', N'Long An'),
('SV202606', N'Vũ Thị Lan', 'sv202606@school.edu.vn', '123456', 0, '0912345606', '2003-05-12', N'Nữ', N'An Giang'),
('SV202607', N'Đỗ Minh Quân', 'sv202607@school.edu.vn', '123456', 0, '0912345607', '2004-12-08', N'Nam', N'Kiên Giang'),
('SV202608', N'Bùi Thị Thanh', 'sv202608@school.edu.vn', '123456', 0, '0912345608', '2004-02-25', N'Nữ', N'Vĩnh Long'),
('SV202609', N'Đặng Văn Hùng', 'sv202609@school.edu.vn', '123456', 0, '0912345609', '2003-08-14', N'Nam', N'Cà Mau'),
('SV202610', N'Ngô Thị Thu', 'sv202610@school.edu.vn', '123456', 0, '0912345610', '2004-06-03', N'Nữ', N'Bình Phước'),
('GV1001', N'PGS.TS. Nguyễn Văn A', 'nguyenvana@school.edu.vn', '123456', 0, '0912345611', '1975-04-10', N'Nam', N'Bình Định'),
('GV1002', N'TS. Trần Thị B', 'tranthib@school.edu.vn', '123456', 0, '0912345612', '1980-10-28', N'Nữ', N'TP. Hồ Chí Minh'),
('GV1003', N'ThS. Lê Văn C', 'levanc@school.edu.vn', '123456', 0, '0912345613', '1983-01-16', N'Nam', N'Nghệ An'),
('GV1004', N'ThS. Phạm Thị D', 'phamthid@school.edu.vn', '123456', 0, '0912345614', '1985-09-07', N'Nữ', N'Thanh Hóa'),
('GV1005', N'TS. Trần Thị Giảng', 'tranthigiang@school.edu.vn', '123456', 0, '0912345615', '1978-03-24', N'Nữ', N'Thừa Thiên Huế'),
('CSVC501', N'Nguyễn Văn Cơ Sở', 'coso1@school.edu.vn', '123456', 0, '0912345616', '1990-05-19', N'Nam', N'Bình Dương'),
('CSVC502', N'Lê Văn Vật Chất', 'coso2@school.edu.vn', '123456', 0, '0912345617', '1992-11-30', N'Nam', N'Đồng Nai'),
('ST501', N'Phạm Văn Đào Tạo', 'daotao1@school.edu.vn', '123456', 0, '0912345618', '1988-07-11', N'Nam', N'Tây Ninh'),
('ST502', N'Vũ Thị Đào Tạo', 'daotao2@school.edu.vn', '123456', 0, '0912345619', '1991-02-06', N'Nữ', N'Long An'),
('AD01', N'Quản Trị Viên Hệ Thống', 'admin.sys@school.edu.vn', '123456', 0, '0912345620', '1986-12-02', N'Nam', N'TP. Hồ Chí Minh');

--------------------------------------------------------------------------------
-- 3. BẢNG PHÂN QUYỀN (NguoiDung_VaiTro)
--------------------------------------------------------------------------------
INSERT INTO NguoiDung_VaiTro (ma_nguoi_dung, ma_vai_tro) VALUES
('SV202601', 1), ('SV202602', 1), ('SV202603', 1), ('SV202604', 1), ('SV202605', 1),
('SV202606', 1), ('SV202607', 1), ('SV202608', 1), ('SV202609', 1), ('SV202610', 1),
('GV1001', 2), ('GV1002', 2), ('GV1003', 2), ('GV1004', 2), ('GV1005', 2),
('CSVC501', 3), ('CSVC502', 3), 
('ST501', 4), ('ST502', 4), 
('AD01', 5);

--------------------------------------------------------------------------------
-- 4. BẢNG TÒA NHÀ (ToaNha)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT ToaNha ON;
INSERT INTO ToaNha (ma_toa_nha, code_toa_nha, ten_toa_nha) VALUES
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
SET IDENTITY_INSERT ToaNha OFF;

--------------------------------------------------------------------------------
-- 5. BẢNG PHÒNG HỌC (PhongHoc)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT PhongHoc ON;
INSERT INTO PhongHoc (ma_phong, code_phong, ma_toa_nha, loai_phong, suc_chua, trang_thai_phong, hien_thi_dang_ky_muon) VALUES
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
SET IDENTITY_INSERT PhongHoc OFF;

--------------------------------------------------------------------------------
-- 6. BẢNG THIẾT BỊ (ThietBi)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT ThietBi ON;
INSERT INTO ThietBi (ma_thiet_bi, loai_phong, ten_thiet_bi) VALUES
(1, 'ROOM_THEORY', N'Máy chiếu Projector'),
(2, 'ROOM_THEORY', N'Hệ thống Loa/Micro'),
(3, 'ROOM_PRACTICE', N'Máy tính bàn (Workstation)'),
(4, 'ROOM_PRACTICE', N'Chuột & Bàn phím'),
(5, 'ROOM_LAB', N'Kính hiển vi quang học'),
(6, 'ROOM_LAB', N'Ống nghiệm và khay đong hóa chất'),
(7, 'ROOM_PRACTICE', N'Bộ thực hành vi điều khiển PLC'),
(8, 'ROOM_SPORT', N'Bảng điểm điện tử thể thao'),
(9, 'ROOM_THEORY', N'Máy điều hòa không khí 24000 BTU'),
(10, 'ROOM_THEORY', N'Bảng thông minh tương tác'),
(11, 'ROOM_LAB', N'Cân điện tử phân tích độ chính xác cao'),
(12, 'ROOM_PRACTICE', N'Máy hàn điện tử chuyên dụng'),
(13, 'ROOM_THEORY', N'Quạt treo tường công nghiệp'),
(14, 'ROOM_LAB', N'Máy đo dao động ký điện tử'),
(15, 'ROOM_THEORY', N'Rèm cửa chống sáng cao cấp'),
(16, 'ROOM_SPORT', N'Hệ thống khung thành bóng rổ di động'),
(17, 'ROOM_PRACTICE', N'Máy in 3D công nghiệp'),
(18, 'ROOM_LAB', N'Tủ hút khí độc phòng thí nghiệm'),
(19, 'ROOM_THEORY', N'Bộ thiết bị camera giám sát phòng học'),
(20, 'ROOM_PRACTICE', N'Bàn vẽ kỹ thuật chuyên dụng');
SET IDENTITY_INSERT ThietBi OFF;

--------------------------------------------------------------------------------
-- 7. BẢNG PHÒNG - THIẾT BỊ (Phong_ThietBi)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT Phong_ThietBi ON;
INSERT INTO Phong_ThietBi (ma_phong_thiet_bi, ma_phong, ma_thiet_bi, nhan_dien_thiet_bi, trang_thai) VALUES
(1, 1, 1, N'Projector A2-301', 'GOOD'),
(2, 3, 3, N'Máy 01', 'GOOD'),
(3, 3, 3, N'Máy 15', 'DAMAGED'),
(4, 3, 4, N'Bàn phím Máy 15', 'DAMAGED'),
(5, 1, 2, N'Loa Mic A2-301', 'GOOD'),
(6, 2, 1, N'Projector A2-302', 'GOOD'),
(7, 2, 9, N'Điều hòa A2-302', 'GOOD'),
(8, 5, 1, N'Projector A3-101', 'GOOD'),
(9, 7, 7, N'PLC Kit 01', 'GOOD'),
(10, 8, 12, N'Máy hàn 01', 'GOOD'),
(11, 9, 5, N'Kính hiển vi 01', 'GOOD'),
(12, 9, 11, N'Cân điện tử 01', 'GOOD'),
(13, 11, 14, N'Dao động ký 01', 'GOOD'),
(14, 12, 10, N'Bảng thông minh D1-101', 'GOOD'),
(15, 13, 17, N'Máy in 3D 01', 'GOOD'),
(16, 14, 2, N'Loa G2-101', 'GOOD'),
(17, 16, 1, N'Projector A2-401', 'GOOD'),
(18, 17, 9, N'Điều hòa A2-402', 'GOOD'),
(19, 18, 3, N'Máy trạm B1-301-01', 'GOOD'),
(20, 19, 18, N'Tủ hút khí độc C1-201', 'GOOD');
SET IDENTITY_INSERT Phong_ThietBi OFF;

-- Thiết bị mặc định cho các phòng còn lại: phòng thí nghiệm gắn kính hiển vi, phòng lý thuyết gắn máy chiếu.
INSERT INTO Phong_ThietBi (ma_phong, ma_thiet_bi, nhan_dien_thiet_bi, trang_thai)
SELECT p.ma_phong, t.ma_thiet_bi, t.ten_thiet_bi + N' - ' + p.code_phong, 'GOOD'
FROM PhongHoc p
CROSS APPLY (
    SELECT TOP 1 ma_thiet_bi, ten_thiet_bi
    FROM ThietBi
    WHERE ten_thiet_bi = CASE WHEN p.loai_phong = 'ROOM_LAB' THEN N'Kính hiển vi quang học' ELSE N'Máy chiếu Projector' END
    ORDER BY ma_thiet_bi
) t
WHERE NOT EXISTS (SELECT 1 FROM Phong_ThietBi x WHERE x.ma_phong = p.ma_phong);

--------------------------------------------------------------------------------
-- 8. BẢNG HỌC PHẦN (HocPhan)
--------------------------------------------------------------------------------
INSERT INTO HocPhan (ma_hoc_phan, ten_hoc_phan, so_luong_dang_ky, ma_giang_vien, la_thuc_hanh) VALUES
('INT101_01', N'Lập trình căn bản', 45, 'GV1001', 1),
('MAT202_02', N'Toán rời rạc', 75, 'GV1002', 0),
('ENG301_01', N'Tiếng Anh chuyên ngành IT', 60, 'GV1003', 0),
('PHY101_01', N'Vật lý đại cương 1', 90, 'GV1004', 0),
('DAT201_01', N'Cơ sở dữ liệu nâng cao', 50, 'GV1001', 1),
('NET302_01', N'Mạng máy tính', 40, 'GV1005', 1),
('AI401_01', N'Nhập môn Trí tuệ nhân tạo', 55, 'GV1001', 1),
('SEC301_01', N'An toàn thông tin mạng', 45, 'GV1005', 1),
('SE402_01', N'Kiểm thử phần mềm', 50, 'GV1003', 1),
('WEB201_01', N'Lập trình Web nâng cao', 65, 'GV1001', 1),
('ECO101_01', N'Kinh tế học đại cương', 100, 'GV1002', 0),
('MGT201_01', N'Quản trị học', 85, 'GV1004', 0),
('CHE101_01', N'Hóa đại cương', 40, 'GV1002', 1),
('MEC101_01', N'Cơ kỹ thuật', 35, 'GV1004', 1),
('ELE201_01', N'Kỹ thuật điện tử', 45, 'GV1005', 1),
('ARC101_01', N'Nguyên lý thiết kế kiến trúc', 30, 'GV1003', 0),
('MUS101_01', N'Thưởng thức âm nhạc', 50, 'GV1002', 0),
('LAW101_01', N'Pháp luật đại cương', 110, 'GV1004', 0),
('BUS202_01', N'Quản trị dự án phần mềm', 45, 'GV1001', 0),
('FIN101_01', N'Tài chính doanh nghiệp', 70, 'GV1002', 0);

--------------------------------------------------------------------------------
-- 8.1. CHUẨN HÓA CƠ SỞ VẬT CHẤT THEO NGÀNH
-- Sơ đồ khuôn viên ghi tòa I1 (danh mục cũ dùng J1). Phòng được phân lại theo
-- chức năng đào tạo để xếp lịch tự động đúng chuyên ngành.
--------------------------------------------------------------------------------
UPDATE ToaNha SET code_toa_nha = 'I1', ten_toa_nha = N'Tòa nhà I1' WHERE code_toa_nha = 'J1';
UPDATE PhongHoc SET code_phong = N'I1.' + SUBSTRING(code_phong, 4, 50) WHERE code_phong LIKE N'J1.%';

-- Lý thuyết 3 tiết
UPDATE PhongHoc SET loai_phong = 'ROOM_THEORY'
WHERE code_phong IN (N'A1.101', N'A1.102', N'C1.103', N'C1.104', N'C1.203', N'C1.204')
   OR code_phong LIKE N'A3.%' OR code_phong LIKE N'A4.%' OR code_phong LIKE N'D1.%'
   OR code_phong LIKE N'H1.%' OR code_phong LIKE N'I1.%'
   OR code_phong LIKE N'I3.%' OR code_phong LIKE N'I4.%';

-- Máy tính – CNTT & Phần mềm (thực hành 5 tiết)
UPDATE PhongHoc SET loai_phong = 'PHONG_MAY_TINH' WHERE code_phong LIKE N'E1.%';

-- Thí nghiệm Hóa – Sinh và Công nghệ Thực phẩm
UPDATE PhongHoc SET loai_phong = 'LAB_HOA_SINH'
WHERE code_phong IN (N'A2.101', N'A2.102', N'A2.103', N'A2.104', N'A2.105', N'A2.106', N'A2.107');
UPDATE PhongHoc SET loai_phong = 'LAB_CNTP'
WHERE code_phong IN (N'A2.108', N'A2.109', N'A2.110', N'A2.111', N'A2.112', N'A2.113', N'A2.114');

-- Điện – Điện tử – Tự động hóa và Xưởng Cơ khí – Ô tô
UPDATE PhongHoc SET loai_phong = 'LAB_DIEN_DIEN_TU' WHERE code_phong LIKE N'E2.%' OR code_phong LIKE N'E3.%';
UPDATE PhongHoc SET loai_phong = 'XUONG_CO_KHI' WHERE code_phong LIKE N'F2.%' OR code_phong LIKE N'F3.%';

-- Âm nhạc, Mỹ thuật, Kiến trúc
UPDATE PhongHoc SET loai_phong = 'PHONG_LUYEN_THANH' WHERE code_phong IN (N'C1.101', N'C1.102');
UPDATE PhongHoc SET loai_phong = 'PHONG_HOA_TAU' WHERE code_phong IN (N'C1.201', N'C1.202');
UPDATE PhongHoc SET loai_phong = 'PHONG_HOA_VIEN_MY_THUAT' WHERE code_phong IN (N'H2.101', N'H2.102');
UPDATE PhongHoc SET loai_phong = 'PHONG_DO_AN_KIEN_TRUC' WHERE code_phong IN (N'H2.103', N'H2.104');

-- Hành chính, khoa, phòng chức năng: không xếp lịch và không cho đăng ký mượn
UPDATE PhongHoc SET loai_phong = 'PHONG_CHUC_NANG', hien_thi_dang_ky_muon = 0
WHERE code_phong IN (N'A2.203', N'A2.204', N'K2.201')
   OR code_phong LIKE N'B1.%' OR code_phong LIKE N'F1.%'
   OR code_phong LIKE N'K1.%' OR code_phong LIKE N'K23.%';
UPDATE PhongHoc SET hien_thi_dang_ky_muon = 1 WHERE loai_phong <> 'PHONG_CHUC_NANG';

-- Sức chứa theo quy mô lớp
UPDATE PhongHoc SET suc_chua = 150 WHERE code_phong IN (N'A1.101', N'A1.102');
UPDATE PhongHoc SET suc_chua = 70  WHERE code_phong LIKE N'E1.%';
UPDATE PhongHoc SET suc_chua = 45  WHERE code_phong IN (N'A2.101', N'A2.102', N'A2.103', N'A2.104', N'A2.105', N'A2.106', N'A2.107');
UPDATE PhongHoc SET suc_chua = 40  WHERE code_phong IN (N'A2.108', N'A2.109', N'A2.110', N'A2.111', N'A2.112', N'A2.113', N'A2.114');
UPDATE PhongHoc SET suc_chua = 50  WHERE code_phong LIKE N'E2.%' OR code_phong LIKE N'E3.%';
UPDATE PhongHoc SET suc_chua = 40  WHERE code_phong LIKE N'F2.%' OR code_phong LIKE N'F3.%';
UPDATE PhongHoc SET suc_chua = 60  WHERE code_phong IN (N'C1.101', N'C1.102', N'C1.201', N'C1.202');
UPDATE PhongHoc SET suc_chua = 40  WHERE code_phong IN (N'H2.101', N'H2.102', N'H2.103', N'H2.104');
UPDATE PhongHoc SET suc_chua = 60  WHERE code_phong LIKE N'A3.%' OR code_phong LIKE N'A4.%' OR code_phong LIKE N'H1.%' OR code_phong LIKE N'I3.%' OR code_phong LIKE N'I4.%';
UPDATE PhongHoc SET suc_chua = 50  WHERE code_phong LIKE N'D1.%' OR code_phong IN (N'C1.103', N'C1.104', N'C1.203', N'C1.204');

-- Sân thể dục ngoài trời
SET IDENTITY_INSERT ToaNha ON;
INSERT INTO ToaNha (ma_toa_nha, code_toa_nha, ten_toa_nha) VALUES (22, 'SV', N'Khu sân thể dục');
SET IDENTITY_INSERT ToaNha OFF;

SET IDENTITY_INSERT PhongHoc ON;
INSERT INTO PhongHoc (ma_phong, code_phong, ma_toa_nha, loai_phong, suc_chua, trang_thai_phong, hien_thi_dang_ky_muon)
VALUES (212, N'SV.01', 22, 'SAN_THE_DUC_NGOAI_TROI', 200, 'AVAILABLE', 1);
SET IDENTITY_INSERT PhongHoc OFF;

-- Loại sự cố demo đổi theo loại phòng mới.
UPDATE s SET s.loai_su_co = CASE p.loai_phong
        WHEN 'ROOM_THEORY' THEN 'SU_CO_DIEU_HOA_DEN'
        WHEN 'PHONG_MAY_TINH' THEN 'SU_CO_MAY_TINH'
        WHEN 'LAB_HOA_SINH' THEN 'SU_CO_TB_THI_NGHIEM'
        WHEN 'LAB_CNTP' THEN 'SU_CO_TB_THI_NGHIEM'
        WHEN 'LAB_DIEN_DIEN_TU' THEN 'SU_CO_NGUON_DIEN'
        WHEN 'XUONG_CO_KHI' THEN 'SU_CO_MAY_CONG_CU'
        WHEN 'PHONG_LUYEN_THANH' THEN 'SU_CO_NHAC_CU'
        WHEN 'PHONG_HOA_TAU' THEN 'SU_CO_NHAC_CU'
        WHEN 'PHONG_HOA_VIEN_MY_THUAT' THEN 'SU_CO_GIA_VE'
        WHEN 'PHONG_DO_AN_KIEN_TRUC' THEN 'SU_CO_MAY_IN_3D_LASER'
        WHEN 'SAN_THE_DUC_NGOAI_TROI' THEN 'SU_CO_SAN_NGAP'
        ELSE 'SU_CO_KHAC' END
FROM SuCo s JOIN PhongHoc p ON p.ma_phong = s.ma_phong;

-- Bỏ các dòng gán thiết bị mặc định cũ (nhãn do CROSS APPLY sinh có dạng 'Tên - Mã phòng').
DELETE FROM Phong_ThietBi WHERE nhan_dien_thiet_bi LIKE N'% - %';

-- Danh mục thiết bị chuẩn theo loại phòng.
DECLARE @dm TABLE (loai_phong VARCHAR(50) NOT NULL, ten_thiet_bi NVARCHAR(100) NOT NULL);
INSERT INTO @dm (loai_phong, ten_thiet_bi) VALUES
('ROOM_THEORY', N'Máy chiếu Projector'), ('ROOM_THEORY', N'Màn chiếu'), ('ROOM_THEORY', N'Bảng viết'),
('ROOM_THEORY', N'Hệ thống Loa/Micro'), ('ROOM_THEORY', N'Điều hòa'), ('ROOM_THEORY', N'Bàn ghế sinh viên'),
('ROOM_THEORY', N'Bàn giáo viên'),
('PHONG_MAY_TINH', N'Máy tính để bàn'), ('PHONG_MAY_TINH', N'Màn hình'), ('PHONG_MAY_TINH', N'Bàn phím & chuột'),
('PHONG_MAY_TINH', N'Máy chiếu Projector'), ('PHONG_MAY_TINH', N'Điều hòa'), ('PHONG_MAY_TINH', N'Bàn máy tính'),
('PHONG_MAY_TINH', N'Ghế xoay'), ('PHONG_MAY_TINH', N'Switch mạng'),
('LAB_HOA_SINH', N'Tủ hút khí độc (Fume Hood)'), ('LAB_HOA_SINH', N'Bồn rửa hóa chất'),
('LAB_HOA_SINH', N'Kính hiển vi quang học'), ('LAB_HOA_SINH', N'Cân phân tích'), ('LAB_HOA_SINH', N'Máy đo pH'),
('LAB_HOA_SINH', N'Bộ chưng cất'), ('LAB_HOA_SINH', N'Tủ an toàn sinh học'), ('LAB_HOA_SINH', N'Hệ thống thông gió'),
('LAB_CNTP', N'Tủ lên men'), ('LAB_CNTP', N'Nồi hấp tiệt trùng'), ('LAB_CNTP', N'Tủ sấy'),
('LAB_CNTP', N'Máy đo độ ẩm'), ('LAB_CNTP', N'Cân phân tích'), ('LAB_CNTP', N'Kính hiển vi quang học'),
('LAB_CNTP', N'Bồn rửa'), ('LAB_CNTP', N'Tủ hút khí độc (Fume Hood)'),
('LAB_DIEN_DIEN_TU', N'Bàn thực hành mạch điện'), ('LAB_DIEN_DIEN_TU', N'Bộ vi điều khiển'),
('LAB_DIEN_DIEN_TU', N'PLC'), ('LAB_DIEN_DIEN_TU', N'Máy hiện sóng Oscilloscope'), ('LAB_DIEN_DIEN_TU', N'Mỏ hàn'),
('LAB_DIEN_DIEN_TU', N'Đồng hồ vạn năng'), ('LAB_DIEN_DIEN_TU', N'Nguồn DC'), ('LAB_DIEN_DIEN_TU', N'Bộ linh kiện điện tử'),
('XUONG_CO_KHI', N'Máy tiện'), ('XUONG_CO_KHI', N'Máy phay'), ('XUONG_CO_KHI', N'Máy CNC'),
('XUONG_CO_KHI', N'Mô hình động cơ ô tô'), ('XUONG_CO_KHI', N'Cầu nâng ô tô'), ('XUONG_CO_KHI', N'Máy khoan'),
('XUONG_CO_KHI', N'Bộ dụng cụ cầm tay'), ('XUONG_CO_KHI', N'Hệ thống khí nén'),
('PHONG_LUYEN_THANH', N'Đàn Piano'), ('PHONG_LUYEN_THANH', N'Micro'), ('PHONG_LUYEN_THANH', N'Loa kiểm âm'),
('PHONG_LUYEN_THANH', N'Gương soi toàn thân'), ('PHONG_LUYEN_THANH', N'Tấm tiêu âm'),
('PHONG_LUYEN_THANH', N'Sàn gỗ chuyên dụng'), ('PHONG_LUYEN_THANH', N'Bộ gõ'),
('PHONG_HOA_TAU', N'Đàn Piano'), ('PHONG_HOA_TAU', N'Bộ gõ'), ('PHONG_HOA_TAU', N'Micro'),
('PHONG_HOA_TAU', N'Loa kiểm âm'), ('PHONG_HOA_TAU', N'Tấm tiêu âm'), ('PHONG_HOA_TAU', N'Sàn gỗ chuyên dụng'),
('PHONG_HOA_VIEN_MY_THUAT', N'Giá vẽ (Easel)'), ('PHONG_HOA_VIEN_MY_THUAT', N'Bàn vẽ A0/A1'),
('PHONG_HOA_VIEN_MY_THUAT', N'Tượng thạch cao mẫu'), ('PHONG_HOA_VIEN_MY_THUAT', N'Đèn chiếu điểm Spotlight'),
('PHONG_HOA_VIEN_MY_THUAT', N'Bồn rửa cọ'),
('PHONG_DO_AN_KIEN_TRUC', N'Bàn đồ án A0'), ('PHONG_DO_AN_KIEN_TRUC', N'Giá vẽ (Easel)'),
('PHONG_DO_AN_KIEN_TRUC', N'Máy in 3D'), ('PHONG_DO_AN_KIEN_TRUC', N'Máy cắt Laser'),
('PHONG_DO_AN_KIEN_TRUC', N'Đèn chiếu điểm Spotlight'), ('PHONG_DO_AN_KIEN_TRUC', N'Tủ mô hình'),
('SAN_THE_DUC_NGOAI_TROI', N'Đèn chiếu sáng sân'), ('SAN_THE_DUC_NGOAI_TROI', N'Cột bóng rổ'),
('SAN_THE_DUC_NGOAI_TROI', N'Khung thành bóng đá'), ('SAN_THE_DUC_NGOAI_TROI', N'Lưới chắn bóng'),
('SAN_THE_DUC_NGOAI_TROI', N'Hệ thống thoát nước sân'),
('PHONG_CHUC_NANG', N'Bàn làm việc'), ('PHONG_CHUC_NANG', N'Máy tính văn phòng'), ('PHONG_CHUC_NANG', N'Máy in'),
('PHONG_CHUC_NANG', N'Điều hòa'), ('PHONG_CHUC_NANG', N'Tủ hồ sơ');

INSERT INTO ThietBi (loai_phong, ten_thiet_bi)
SELECT dm.loai_phong, dm.ten_thiet_bi
FROM @dm dm
WHERE NOT EXISTS (SELECT 1 FROM ThietBi t WHERE t.loai_phong = dm.loai_phong AND t.ten_thiet_bi = dm.ten_thiet_bi);

INSERT INTO Phong_ThietBi (ma_phong, ma_thiet_bi, nhan_dien_thiet_bi, trang_thai)
SELECT p.ma_phong, t.ma_thiet_bi, t.ten_thiet_bi, 'GOOD'
FROM PhongHoc p
JOIN @dm dm ON dm.loai_phong = p.loai_phong
JOIN ThietBi t ON t.loai_phong = dm.loai_phong AND t.ten_thiet_bi = dm.ten_thiet_bi
WHERE NOT EXISTS (SELECT 1 FROM Phong_ThietBi x WHERE x.ma_phong = p.ma_phong AND x.ma_thiet_bi = t.ma_thiet_bi);

-- Nhóm ngành cho học phần
UPDATE HocPhan SET nhom_nganh = 'NGANH_CNTT'
WHERE ma_hoc_phan LIKE 'AI%' OR ma_hoc_phan LIKE 'DAT%' OR ma_hoc_phan LIKE 'INT%'
   OR ma_hoc_phan LIKE 'NET%' OR ma_hoc_phan LIKE 'SE%' OR ma_hoc_phan LIKE 'SEC%' OR ma_hoc_phan LIKE 'WEB%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_HOA_SINH' WHERE ma_hoc_phan LIKE 'CHE%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_CNTP' WHERE ma_hoc_phan LIKE 'FOD%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_DIEN' WHERE ma_hoc_phan LIKE 'ELE%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_CO_KHI' WHERE ma_hoc_phan LIKE 'MEC%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_AM_NHAC', la_thuc_hanh = 1 WHERE ma_hoc_phan LIKE 'MUS%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_KIEN_TRUC', la_thuc_hanh = 1 WHERE ma_hoc_phan LIKE 'ARC%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_THE_THAO' WHERE ma_hoc_phan LIKE 'PED%';
UPDATE HocPhan SET nhom_nganh = 'NGANH_DAI_CUONG' WHERE nhom_nganh IS NULL;

INSERT INTO HocPhan (ma_hoc_phan, ten_hoc_phan, so_luong_dang_ky, ma_giang_vien, la_thuc_hanh, nhom_nganh) VALUES
('FOD201_01', N'Công nghệ chế biến thực phẩm', 35, 'GV1002', 1, 'NGANH_CNTP'),
('PED101_01', N'Giáo dục thể chất', 60, 'GV1005', 1, 'NGANH_THE_THAO');

--------------------------------------------------------------------------------
-- 9. BẢNG LỊCH HỌC (LichHoc)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT LichHoc ON;
INSERT INTO LichHoc (ma_lich_hoc, ma_hoc_phan, ma_phong, ngay_hoc, ca_hoc, trang_thai_hoat_dong) VALUES
(1, 'INT101_01', 59, '2026-10-30', 'CA_TH_SANG', 1),
(2, 'MAT202_02', 1, '2026-10-30', 'CA_S1', 1),
(3, 'ENG301_01', 2, '2026-10-30', 'CA_S2', 1),
(4, 'PHY101_01', 19, '2026-10-31', 'CA_S1', 1),
(5, 'DAT201_01', 60, '2026-10-31', 'CA_TH_CHIEU', 1),
(6, 'NET302_01', 61, '2026-11-01', 'CA_TH_SANG', 1),
(7, 'AI401_01', 62, '2026-11-01', 'CA_TH_CHIEU', 1),
(8, 'SEC301_01', 63, '2026-11-02', 'CA_TH_SANG', 1),
(9, 'SE402_01', 64, '2026-11-02', 'CA_TH_CHIEU', 1),
(10, 'WEB201_01', 65, '2026-11-03', 'CA_TH_SANG', 1),
(11, 'ECO101_01', 20, '2026-11-03', 'CA_S2', 1),
(12, 'MGT201_01', 21, '2026-11-04', 'CA_S2', 1),
(13, 'CHE101_01', 3, '2026-11-04', 'CA_TH_SANG', 1),
(14, 'MEC101_01', 100, '2026-11-05', 'CA_TH_CHIEU', 1),
(15, 'ELE201_01', 89, '2026-11-05', 'CA_TH_SANG', 1),
(16, 'ARC101_01', 123, '2026-11-06', 'CA_TH_CHIEU', 1),
(17, 'MUS101_01', 45, '2026-11-06', 'CA_TH_SANG', 1),
(18, 'LAW101_01', 22, '2026-11-07', 'CA_S2', 1),
(19, 'BUS202_01', 23, '2026-11-07', 'CA_S1', 1),
(20, 'FIN101_01', 24, '2026-11-08', 'CA_S1', 1);
SET IDENTITY_INSERT LichHoc OFF;

--------------------------------------------------------------------------------
-- 10. BẢNG ĐẶT PHÒNG (DatPhong)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT DatPhong ON;
-- Phiếu mượn ghi khung giờ tường minh (không còn bó theo ca); mọi khoảng đều nằm trong
-- 3 khung giờ trống mặc định: 07:00-12:15, 13:00-18:15, 18:20-21:00.
INSERT INTO DatPhong (ma_dat_phong, nguoi_yeu_cau, ma_phong, ngay_dat, gio_bat_dau, gio_ket_thuc, muc_dich, so_luong_nguoi_tham_gia, trang_thai, nguoi_duyet) VALUES
(1, 'SV202601', 1, '2026-11-02', '07:00', '09:30', N'Đặt phòng học nhóm ôn tập', 15, 'PENDING', NULL),
(2, 'SV202602', 2, '2026-11-02', '09:40', '11:25', N'Họp câu lạc bộ lập trình', 25, 'APPROVED', 'ST501'),
(3, 'SV202603', 5, '2026-11-03', '07:00', '09:00', N'Thuyết trình đồ án nhóm', 20, 'APPROVED', 'ST501'),
(4, 'SV202604', 3, '2026-11-03', '13:00', '16:30', N'Thực hành lập trình thêm', 30, 'REJECTED', 'ST502'),
(5, 'SV202605', 210, '2026-11-04', '07:00', '11:25', N'Tổ chức hội thảo khoa học sinh viên', 80, 'PENDING', NULL),
(6, 'SV202606', 1, '2026-11-04', '09:00', '11:00', N'Ôn thi cuối kỳ môn Toán', 40, 'PENDING', NULL),
(7, 'SV202607', 2, '2026-11-05', '07:00', '09:30', N'Sinh hoạt chi đoàn', 50, 'APPROVED', 'ST501'),
(8, 'SV202608', 5, '2026-11-05', '09:40', '11:25', N'Luyện tập thuyết trình tiếng Anh', 20, 'PENDING', NULL),
(9, 'SV202609', 45, '2026-11-06', '07:00', '09:00', N'Tập văn nghệ khoa nghệ thuật', 35, 'APPROVED', 'ST502'),
(10, 'SV202610', 1, '2026-11-06', '09:40', '11:25', N'Họp ban chấp hành đoàn trường', 15, 'PENDING', NULL),
(11, 'GV1001', 3, '2026-11-07', '07:00', '11:25', N'Dạy bù môn Lập trình căn bản', 45, 'APPROVED', 'ST501'),
(12, 'GV1002', 1, '2026-11-07', '07:00', '08:30', N'Họp bộ môn Toán', 10, 'APPROVED', 'ST501'),
(13, 'GV1003', 5, '2026-11-08', '09:40', '11:25', N'Chấm đồ án tốt nghiệp', 12, 'PENDING', NULL),
(14, 'GV1004', 209, '2026-11-08', '13:00', '17:25', N'Hội thảo khoa học cấp trường', 90, 'APPROVED', 'ST502'),
(15, 'GV1005', 24, '2026-11-09', '13:00', '16:00', N'Bồi dưỡng đội tuyển Olympic Tin học', 35, 'APPROVED', 'ST501'),
(16, 'SV202601', 2, '2026-11-09', '07:00', '09:30', N'Thảo luận đề tài nghiên cứu', 5, 'PENDING', NULL),
(17, 'SV202603', 3, '2026-11-10', '07:00', '11:25', N'Tự học nhóm thực hành', 10, 'PENDING', NULL),
(18, 'SV202605', 5, '2026-11-10', '09:40', '11:25', N'Học nhóm ngoại ngữ', 15, 'PENDING', NULL),
(19, 'GV1001', 1, '2026-11-11', '18:30', '20:30', N'Tư vấn học tập sinh viên', 20, 'APPROVED', 'ST501'),
(20, 'SV202610', 210, '2026-11-11', '13:00', '15:30', N'Giao lưu sinh viên quốc tế', 60, 'PENDING', NULL);
SET IDENTITY_INSERT DatPhong OFF;

-- Phiếu mẫu đã xử lý: gắn mốc thời gian để trang Phiếu đã gửi hiển thị kết quả,
-- và đánh dấu đã xem sẵn để không hiện nhãn "Mới" cho dữ liệu cũ.
UPDATE DatPhong
SET thoi_diem_xu_ly = DATEADD(HOUR, -2, GETDATE()), da_xem_ket_qua = 1
WHERE trang_thai IN ('APPROVED', 'REJECTED') AND thoi_diem_xu_ly IS NULL;

--------------------------------------------------------------------------------
-- 11. BẢNG SỰ CỐ (SuCo)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT SuCo ON;
INSERT INTO SuCo (ma_su_co, ma_phong, nguoi_bao_cao, loai_su_co, mo_ta, muc_do_nghiem_trong, trang_thai, ngay_tao) VALUES
(1, 3, 'GV1001', 'LOAI_THIET_BI', N'Máy tính bàn số 15 không lên nguồn', 'HIGH', 'NEW', GETDATE()),
(2, 1, 'GV1002', 'LOAI_PHONG', N'Máy chiếu không kết nối được tín hiệu VGA/HDMI', 'MEDIUM', 'IN_PROGRESS', GETDATE()),
(3, 4, 'CSVC501', 'LOAI_CO_SO', N'Phòng bảo trì kẹt cửa sổ', 'LOW', 'RESOLVED', GETDATE()),
(4, 5, 'GV1003', 'LOAI_PHONG', N'Hệ thống âm thanh bị hú rè', 'MEDIUM', 'NEW', GETDATE()),
(5, 7, 'GV1005', 'LOAI_THIET_BI', N'Bộ thực hành PLC bị lỗi nguồn', 'HIGH', 'NEW', GETDATE()),
(6, 9, 'GV1002', 'LOAI_THIET_BI', N'Kính hiển vi bị mờ ống kính', 'MEDIUM', 'IN_PROGRESS', GETDATE()),
(7, 11, 'GV1004', 'LOAI_PHONG', N'Dao động ký không lên màn hình hiển thị', 'HIGH', 'NEW', GETDATE()),
(8, 12, 'GV1001', 'LOAI_CO_SO', N'Bóng đèn hỏng một nửa dãy', 'LOW', 'RESOLVED', GETDATE()),
(9, 13, 'GV1003', 'LOAI_THIET_BI', N'Máy in 3D bị kẹt nhựa đùn', 'MEDIUM', 'NEW', GETDATE()),
(10, 15, 'SV202601', 'LOAI_CO_SO', N'Rách lưới khung thành bóng rổ', 'LOW', 'NEW', GETDATE()),
(11, 2, 'GV1002', 'LOAI_PHONG', N'Điều hòa không lạnh, chảy nước', 'MEDIUM', 'IN_PROGRESS', GETDATE()),
(12, 16, 'GV1004', 'LOAI_THIET_BI', N'Hỏng điều khiển máy chiếu', 'LOW', 'RESOLVED', GETDATE()),
(13, 18, 'GV1005', 'LOAI_THIET_BI', N'Bàn phím máy trạm bị liệt phím Space', 'LOW', 'NEW', GETDATE()),
(14, 19, 'GV1002', 'LOAI_CO_SO', N'Tủ hút khí độc phát tiếng ồn lớn', 'HIGH', 'NEW', GETDATE()),
(15, 3, 'GV1001', 'LOAI_PHONG', N'Mất nguồn điện cụm ổ cắm bàn 5', 'HIGH', 'IN_PROGRESS', GETDATE()),
(16, 5, 'GV1003', 'LOAI_CO_SO', N'Hỏng khóa cửa chính phòng học', 'HIGH', 'RESOLVED', GETDATE()),
(17, 7, 'GV1005', 'LOAI_PHONG', N'Cháy cầu chì bảng điện thực hành', 'HIGH', 'NEW', GETDATE()),
(18, 9, 'GV1002', 'LOAI_CO_SO', N'Vòi nước bồn rửa hóa chất bị rò rỉ', 'MEDIUM', 'NEW', GETDATE()),
(19, 12, 'GV1001', 'LOAI_THIET_BI', N'Mic không dây bị hết pin liên tục', 'LOW', 'RESOLVED', GETDATE()),
(20, 14, 'GV1003', 'LOAI_PHONG', N'Quạt trần phát ra tiếng động lạ', 'LOW', 'NEW', GETDATE());
SET IDENTITY_INSERT SuCo OFF;

--------------------------------------------------------------------------------
-- 12. BẢNG CHI TIẾT SỰ CỐ (ChiTietSuCo)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT ChiTietSuCo ON;
INSERT INTO ChiTietSuCo (ma_chi_tiet_su_co, ma_su_co, ma_phong_thiet_bi, ghi_chu_loi) VALUES
(1, 1, 2, N'Hỏng nguồn phần cứng máy tính số 15'),
(2, 2, 1, N'Lỏng cáp tín hiệu projector A2-301'),
(3, 3, NULL, N'Bôi trơn bản lề cửa phòng B1-205'),
(4, 4, 5, N'Cháy loa tép micro A3-101'),
(5, 5, 9, N'Hỏng nguồn adapter PLC Kit 01'),
(6, 6, 11, N'Cần vệ sinh thấu kính kính hiển vi 01'),
(7, 7, 13, N'Lỗi bo mạch nguồn dao động ký 01'),
(8, 8, NULL, N'Thay thế 4 bóng đèn tuýp LED D1-101'),
(9, 9, 15, N'Tắc đầu phun nhựa máy in 3D'),
(10, 10, NULL, N'Thay lưới khung thành sân F1'),
(11, 11, 7, N'Thiếu gas điều hòa A2-302'),
(12, 12, 17, N'Hết pin remote máy chiếu A2-401'),
(13, 13, 4, N'Đứt mạch bàn phím máy trạm B1-301'),
(14, 14, 20, N'Hỏng quạt hút khí độc tủ C1-201'),
(15, 15, NULL, N'Sửa chữa aptomat tổng bàn 5 phòng B1-204'),
(16, 16, NULL, N'Thay ổ khóa nắm tròn phòng A3-101'),
(17, 17, NULL, N'Thay cầu chì bảo vệ tủ điện B2-201'),
(18, 18, NULL, N'Thay gioăng cao su vòi nước C1-101'),
(19, 19, 5, N'Sạc đầy pin và thay pin micro D1-101'),
(20, 20, NULL, N'Bảo dưỡng tra dầu quạt trần G2-101');
SET IDENTITY_INSERT ChiTietSuCo OFF;

--------------------------------------------------------------------------------
-- 13. BẢNG ĐƠN XIN NGHỈ (DonXinNghi)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT DonXinNghi ON;
INSERT INTO DonXinNghi (ma_don, ma_giang_vien, ma_hoc_phan, ly_do, phuong_an_giai_quyet, ma_giang_vien_day_thay, trang_thai, nguoi_duyet) VALUES
(1, 'GV1001', 'INT101_01', N'Giảng viên đi công tác đột xuất', 'MAKEUP_CLASS', NULL, 'PENDING', NULL),
(2, 'GV1002', 'MAT202_02', N'Giảng viên bị ốm đau phải nghỉ điều trị', 'SUBSTITUTE_TEACHER', 'GV1003', 'APPROVED', 'ST501'),
(3, 'GV1003', 'ENG301_01', N'Giảng viên tham gia hội thảo khoa học quốc tế', 'MAKEUP_CLASS', NULL, 'APPROVED', 'ST501'),
(4, 'GV1004', 'PHY101_01', N'Giảng viên giải quyết việc gia đình', 'SUBSTITUTE_TEACHER', 'GV1002', 'REJECTED', 'ST502'),
(5, 'GV1005', 'NET302_01', N'Giảng viên tập huấn chuyên môn chuyên ngành', 'MAKEUP_CLASS', NULL, 'PENDING', NULL),
(6, 'GV1001', 'DAT201_01', N'Giảng viên chấm thi tại hội đồng khác', 'MAKEUP_CLASS', NULL, 'APPROVED', 'ST501'),
(7, 'GV1002', 'ECO101_01', N'Giảng viên tham gia nghiệm thu đề tài', 'SUBSTITUTE_TEACHER', 'GV1004', 'PENDING', NULL),
(8, 'GV1003', 'SE402_01', N'Giảng viên đi công tác địa phương', 'MAKEUP_CLASS', NULL, 'PENDING', NULL),
(9, 'GV1004', 'MGT201_01', N'Giảng viên bận lịch họp hội đồng khoa', 'SUBSTITUTE_TEACHER', 'GV1002', 'APPROVED', 'ST502'),
(10, 'GV1005', 'SEC301_01', N'Giảng viên tham gia đoàn kiểm tra an toàn', 'MAKEUP_CLASS', NULL, 'PENDING', NULL),
(11, 'GV1001', 'AI401_01', N'Giảng viên hướng dẫn sinh viên thi học thuật', 'MAKEUP_CLASS', NULL, 'APPROVED', 'ST501'),
(12, 'GV1002', 'CHE101_01', N'Giảng viên đi học lớp bồi dưỡng chính trị', 'SUBSTITUTE_TEACHER', 'GV1004', 'PENDING', NULL),
(13, 'GV1003', 'ARC101_01', N'Giảng viên đi khảo sát thực tế công trình', 'MAKEUP_CLASS', NULL, 'APPROVED', 'ST501'),
(14, 'GV1004', 'LAW101_01', N'Giảng viên tham gia tư vấn pháp luật', 'SUBSTITUTE_TEACHER', 'GV1002', 'REJECTED', 'ST502'),
(15, 'GV1005', 'ELE201_01', N'Giảng viên bảo trì thiết bị phòng lab cá nhân', 'MAKEUP_CLASS', NULL, 'PENDING', NULL),
(16, 'GV1001', 'WEB201_01', N'Giảng viên tham gia hội thảo công nghệ', 'MAKEUP_CLASS', NULL, 'APPROVED', 'ST501'),
(17, 'GV1002', 'MUS101_01', N'Giảng viên chấm thi văn nghệ cấp thành phố', 'SUBSTITUTE_TEACHER', 'GV1003', 'PENDING', NULL),
(18, 'GV1003', 'BUS202_01', N'Giảng viên gặp sự cố sức khỏe đột xuất', 'MAKEUP_CLASS', NULL, 'APPROVED', 'ST502'),
(19, 'GV1004', 'FIN101_01', N'Giảng viên đi bảo vệ đề tài cấp nhà nước', 'SUBSTITUTE_TEACHER', 'GV1002', 'PENDING', NULL),
(20, 'GV1005', 'MEC101_01', N'Giảng viên kiểm định máy móc xưởng thực hành', 'MAKEUP_CLASS', NULL, 'PENDING', NULL);
SET IDENTITY_INSERT DonXinNghi OFF;

--------------------------------------------------------------------------------
-- 14. BẢNG NHẬT KÝ HỆ THỐNG (NhatKyHeThong)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT NhatKyHeThong ON;
INSERT INTO NhatKyHeThong (ma_nhat_ky, thoi_gian, ma_nguoi_thuc_hien, loai_hanh_dong, doi_tuong_tac_dong, du_lieu_cu, du_lieu_moi, dia_chi_ip) VALUES
(1, GETDATE(), 'AD01', 'ACTION_UPDATE', 'NguoiDung', N'Trạng thái cũ: Hoạt động', N'Trạng thái mới: Bị khóa', '192.168.1.50'),
(2, GETDATE(), 'AD01', 'ACTION_CREATE', 'VaiTro', NULL, N'Thêm vai trò: Trưởng khoa', '192.168.1.50'),
(3, GETDATE(), 'ST501', 'ACTION_UPDATE', 'DatPhong', N'Trạng thái: Chờ duyệt', N'Trạng thái: Đã duyệt', '192.168.1.88'),
(4, GETDATE(), 'CSVC501', 'ACTION_UPDATE', 'PhongHoc', N'Trạng thái: Đang hoạt động', N'Trạng thái: Đang bảo trì', '192.168.1.65'),
(5, GETDATE(), 'AD01', 'ACTION_DELETE', 'NguoiDung', N'Xóa tài khoản test cũ', NULL, '192.168.1.50'),
(6, GETDATE(), 'ST502', 'ACTION_UPDATE', 'DonXinNghi', N'Trạng thái: Chờ duyệt', N'Trạng thái: Đã duyệt', '192.168.1.89'),
(7, GETDATE(), 'CSVC502', 'ACTION_CREATE', 'ThietBi', NULL, N'Thêm thiết bị mới: Máy in 3D', '192.168.1.66'),
(8, GETDATE(), 'AD01', 'ACTION_UPDATE', N'Phân quyền người dùng', N'Vai trò cũ: Sinh viên', N'Vai trò mới: Giảng viên', '192.168.1.50'),
(9, GETDATE(), 'ST501', 'ACTION_CREATE', 'LichHoc', NULL, N'Phân bổ lịch học mới INT101_01', '192.168.1.88'),
(10, GETDATE(), 'GV1001', 'ACTION_CREATE', 'SuCo', NULL, N'Báo cáo sự cố máy tính bàn 15', '192.168.1.101'),
(11, GETDATE(), 'CSVC501', 'ACTION_UPDATE', 'Phong_ThietBi', N'Trạng thái: Hỏng', N'Trạng thái: Bình thường', '192.168.1.65'),
(12, GETDATE(), 'AD01', 'ACTION_LOGIN', 'NguoiDung', NULL, N'Admin đăng nhập thành công', '192.168.1.50'),
(13, GETDATE(), 'ST501', 'ACTION_UPDATE', 'HocPhan', N'Số lượng: 40', N'Số lượng: 45', '192.168.1.88'),
(14, GETDATE(), 'CSVC502', 'ACTION_DELETE', 'Phong_ThietBi', N'Thanh lý thiết bị hỏng', NULL, '192.168.1.66'),
(15, GETDATE(), 'AD01', 'ACTION_UPDATE', 'ToaNha', N'Tên cũ: A2', N'Tên mới: Tòa nhà Lý thuyết A2', '192.168.1.50'),
(16, GETDATE(), 'ST502', 'ACTION_CREATE', 'DatPhong', NULL, N'Tạo đặt phòng hội thảo mới', '192.168.1.89'),
(17, GETDATE(), 'GV1002', 'ACTION_CREATE', 'DonXinNghi', NULL, N'Tạo đơn xin nghỉ ốm', '192.168.1.102'),
(18, GETDATE(), 'AD01', 'ACTION_RESET', 'NguoiDung', N'Reset mật khẩu người dùng', N'Mật khẩu mặc định 123456', '192.168.1.50'),
(19, GETDATE(), 'CSVC501', 'ACTION_UPDATE', 'PhongHoc', N'Trạng thái: Đang bảo trì', N'Trạng thái: Đang hoạt động', '192.168.1.65'),
(20, GETDATE(), 'AD01', 'ACTION_LOGOUT', 'NguoiDung', NULL, N'Admin đăng xuất hệ thống', '192.168.1.50');
SET IDENTITY_INSERT NhatKyHeThong OFF;

--------------------------------------------------------------------------------
-- 15. BẢNG KHUNG GIỜ TRỐNG CỦA PHÒNG (Phong_KhungTrong)
-- Nhân viên cơ sở vật chất khai khung giờ phòng trống, không bó theo ca học.
-- Ba khung chuẩn phủ trọn ca lý thuyết 3 tiết (S1, S2, C1, C2, T1)
-- và ca thực hành 5 tiết (TH1 07:00-11:30, TH2 13:00-17:30).
-- CSVC vẫn tự do sửa lại giờ khi mở form Sửa phòng.
--------------------------------------------------------------------------------
INSERT INTO Phong_KhungTrong (ma_phong, gio_bat_dau, gio_ket_thuc)
SELECT p.ma_phong, k.tu, k.den
FROM PhongHoc p
CROSS JOIN (VALUES
    (CAST('07:00' AS time), CAST('12:15' AS time)),
    (CAST('13:00' AS time), CAST('18:15' AS time)),
    (CAST('18:20' AS time), CAST('21:00' AS time))
) k(tu, den);

-- Sân ngoài trời không mở ca tối.
DELETE k FROM Phong_KhungTrong k
JOIN PhongHoc p ON p.ma_phong = k.ma_phong
WHERE p.code_phong = N'SV.01' AND k.gio_bat_dau = CAST('18:20' AS time);

--------------------------------------------------------------------------------
-- 16. THÔNG TIN QUẢN LÝ TÒA NHÀ VÀ MÃ THIẾT BỊ
-- Địa chỉ khuôn viên, số tầng suy ra từ chữ số hàng trăm của mã phòng,
-- mã quản lý cho từng loại thiết bị (CSVC sửa lại trên giao diện khi cần).
--------------------------------------------------------------------------------
UPDATE ToaNha
SET dia_chi = N'06 Trần Văn Ơn, Phường Phú Hòa, TP. Thủ Dầu Một'
WHERE dia_chi IS NULL;

UPDATE t
SET so_tang = x.so_tang
FROM ToaNha t
CROSS APPLY (
    SELECT MAX(TRY_CAST(SUBSTRING(p.code_phong, CHARINDEX(N'.', p.code_phong) + 1, 1) AS int)) AS so_tang
    FROM PhongHoc p
    WHERE p.ma_toa_nha = t.ma_toa_nha
) x
WHERE t.so_tang IS NULL AND x.so_tang IS NOT NULL;

UPDATE ThietBi
SET ma_code_thiet_bi = 'TB-' + RIGHT('0000' + CAST(ma_thiet_bi AS varchar(10)), 4)
WHERE ma_code_thiet_bi IS NULL;
GO

SELECT * FROM NguoiDung;
select * from ThongBao
SELECT DB_NAME() AS TenDatabase;
SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;