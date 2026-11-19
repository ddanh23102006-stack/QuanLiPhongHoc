using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPhongCaTrong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Phong_CaTrong",
                columns: table => new
                {
                    ma_ca_trong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phong = table.Column<int>(type: "int", nullable: false),
                    ca_hoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    gio_bat_dau = table.Column<TimeOnly>(type: "time", nullable: false),
                    gio_ket_thuc = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phong_CaTrong", x => x.ma_ca_trong);
                    table.ForeignKey(
                        name: "FK_Phong_CaTrong_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Phong_CaTrong_ma_phong_ca_hoc",
                table: "Phong_CaTrong",
                columns: new[] { "ma_phong", "ca_hoc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Phong_CaTrong");
        }
    }
}
