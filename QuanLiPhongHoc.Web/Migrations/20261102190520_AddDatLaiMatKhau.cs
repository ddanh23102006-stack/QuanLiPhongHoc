using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDatLaiMatKhau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatLaiMatKhau",
                columns: table => new
                {
                    ma_dat_lai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_nguoi_dung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    het_han = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ngay_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatLaiMatKhau", x => x.ma_dat_lai);
                });

            migrationBuilder.CreateIndex(
                name: "idx_datlaipassword_nguoidung",
                table: "DatLaiMatKhau",
                column: "ma_nguoi_dung");

            migrationBuilder.CreateIndex(
                name: "idx_datlaipassword_token",
                table: "DatLaiMatKhau",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatLaiMatKhau");
        }
    }
}
