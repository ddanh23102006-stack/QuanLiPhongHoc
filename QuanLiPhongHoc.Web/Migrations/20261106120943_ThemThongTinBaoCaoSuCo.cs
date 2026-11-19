using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class ThemThongTinBaoCaoSuCo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "anh_dinh_kem",
                table: "SuCo",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "doi_tuong_bao_cao",
                table: "SuCo",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ho_ten_nguoi_bao_cao",
                table: "SuCo",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "noi_dung_gop_y",
                table: "SuCo",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "so_dien_thoai_lien_he",
                table: "SuCo",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "anh_dinh_kem",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "doi_tuong_bao_cao",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "ho_ten_nguoi_bao_cao",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "noi_dung_gop_y",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "so_dien_thoai_lien_he",
                table: "SuCo");
        }
    }
}
