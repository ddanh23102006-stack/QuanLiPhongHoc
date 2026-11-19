using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class QuanLyToaNhaVaThietBi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dia_chi",
                table: "ToaNha",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "so_tang",
                table: "ToaNha",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trang_thai_hoat_dong",
                table: "ToaNha",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<string>(
                name: "ma_code_thiet_bi",
                table: "ThietBi",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_thietbi_ma_code",
                table: "ThietBi",
                column: "ma_code_thiet_bi",
                unique: true,
                filter: "[ma_code_thiet_bi] IS NOT NULL");

            // Điền dữ liệu mô tả cho các tòa nhà đã có: địa chỉ khuôn viên, số tầng suy ra từ
            // chữ số hàng trăm của mã phòng (A2.203 → tầng 2), mã quản lý cho từng loại thiết bị.
            migrationBuilder.Sql("""
                UPDATE dbo.ToaNha
                SET dia_chi = N'06 Trần Văn Ơn, Phường Phú Hòa, TP. Thủ Dầu Một'
                WHERE dia_chi IS NULL;

                UPDATE t
                SET so_tang = x.so_tang
                FROM dbo.ToaNha t
                CROSS APPLY (
                    SELECT MAX(TRY_CAST(SUBSTRING(p.code_phong, CHARINDEX(N'.', p.code_phong) + 1, 1) AS int)) AS so_tang
                    FROM dbo.PhongHoc p
                    WHERE p.ma_toa_nha = t.ma_toa_nha
                ) x
                WHERE t.so_tang IS NULL AND x.so_tang IS NOT NULL;

                UPDATE dbo.ThietBi
                SET ma_code_thiet_bi = 'TB-' + RIGHT('0000' + CAST(ma_thiet_bi AS varchar(10)), 4)
                WHERE ma_code_thiet_bi IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_thietbi_ma_code",
                table: "ThietBi");

            migrationBuilder.DropColumn(
                name: "dia_chi",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "so_tang",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "trang_thai_hoat_dong",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "ma_code_thiet_bi",
                table: "ThietBi");
        }
    }
}
