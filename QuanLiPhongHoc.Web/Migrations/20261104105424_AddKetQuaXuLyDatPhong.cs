using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKetQuaXuLyDatPhong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "da_xem_ket_qua",
                table: "DatPhong",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "thoi_diem_xu_ly",
                table: "DatPhong",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "da_xem_ket_qua",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "thoi_diem_xu_ly",
                table: "DatPhong");
        }
    }
}
