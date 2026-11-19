using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class DieuPhoiThuCongLichHoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ghi_chu_dieu_phoi",
                table: "LichHoc",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ma_giang_vien_day_thay",
                table: "LichHoc",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_lichhoc_giangvien_thay",
                table: "LichHoc",
                column: "ma_giang_vien_day_thay");

            migrationBuilder.AddForeignKey(
                name: "FK_LichHoc_NguoiDung_ma_giang_vien_day_thay",
                table: "LichHoc",
                column: "ma_giang_vien_day_thay",
                principalTable: "NguoiDung",
                principalColumn: "ma_nguoi_dung",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LichHoc_NguoiDung_ma_giang_vien_day_thay",
                table: "LichHoc");

            migrationBuilder.DropIndex(
                name: "idx_lichhoc_giangvien_thay",
                table: "LichHoc");

            migrationBuilder.DropColumn(
                name: "ghi_chu_dieu_phoi",
                table: "LichHoc");

            migrationBuilder.DropColumn(
                name: "ma_giang_vien_day_thay",
                table: "LichHoc");
        }
    }
}
