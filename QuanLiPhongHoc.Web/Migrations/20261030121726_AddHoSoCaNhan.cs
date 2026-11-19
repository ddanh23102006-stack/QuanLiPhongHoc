using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddHoSoCaNhan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gioi_tinh",
                table: "NguoiDung",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ngay_sinh",
                table: "NguoiDung",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "noi_sinh",
                table: "NguoiDung",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "so_dien_thoai",
                table: "NguoiDung",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gioi_tinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "ngay_sinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "noi_sinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "so_dien_thoai",
                table: "NguoiDung");
        }
    }
}
