using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddThongBao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    ma_thong_bao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tieu_de = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    noi_dung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    anh_dai_dien = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    doi_tuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ALL"),
                    la_moi = table.Column<bool>(type: "bit", nullable: false),
                    noi_bat = table.Column<bool>(type: "bit", nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PUBLISHED"),
                    ngay_dang = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    nguoi_dang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.ma_thong_bao);
                });

            migrationBuilder.CreateIndex(
                name: "idx_thongbao_hien_thi",
                table: "ThongBao",
                columns: new[] { "trang_thai", "ngay_dang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThongBao");
        }
    }
}
