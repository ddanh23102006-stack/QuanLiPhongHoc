using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuanLiPhongHoc.Web.Data;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations;

[DbContext(typeof(QuanLiPhongHocContext))]
[Migration("20261107180000_LoaiBoPhongSeedGs25")]
public partial class LoaiBoPhongSeedGs25 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DECLARE @maPhong INT;
SELECT @maPhong = ma_phong FROM dbo.PhongHoc WHERE code_phong = N'GS.25';

IF @maPhong IS NOT NULL
BEGIN
    -- Giữ lại dấu vết lịch sai nhưng ngừng hiển thị và cho phép xếp lại các học phần đó.
    UPDATE dbo.LichHoc
    SET trang_thai_hoat_dong = 0, ma_phong = NULL
    WHERE ma_phong = @maPhong;

    UPDATE dbo.DatPhong SET ma_phong = NULL WHERE ma_phong = @maPhong;
    UPDATE dbo.SuCo
    SET ma_phong = NULL,
        phong_dieu_chuyen = CASE WHEN phong_dieu_chuyen = @maPhong THEN NULL ELSE phong_dieu_chuyen END
    WHERE ma_phong = @maPhong OR phong_dieu_chuyen = @maPhong;
    UPDATE dbo.YeuCauSuaChua SET ma_phong = NULL WHERE ma_phong = @maPhong;

    DELETE FROM dbo.Phong_CaTrong WHERE ma_phong = @maPhong;
    DELETE FROM dbo.Phong_KhungTrong WHERE ma_phong = @maPhong;
    DELETE FROM dbo.Phong_ThietBi WHERE ma_phong = @maPhong;
    DELETE FROM dbo.PhongHoc WHERE ma_phong = @maPhong;
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.PhongHoc WHERE code_phong = N'GS.25')
   AND EXISTS (SELECT 1 FROM dbo.ToaNha WHERE ma_toa_nha = 21)
BEGIN
    SET IDENTITY_INSERT dbo.PhongHoc ON;
    INSERT INTO dbo.PhongHoc
        (ma_phong, code_phong, ma_toa_nha, loai_phong, suc_chua, trang_thai_phong, hien_thi_dang_ky_muon)
    VALUES
        (211, N'GS.25', 21, 'ROOM_THEORY', 150, 'UNAVAILABLE', 0);
    SET IDENTITY_INSERT dbo.PhongHoc OFF;
END
");
    }
}
