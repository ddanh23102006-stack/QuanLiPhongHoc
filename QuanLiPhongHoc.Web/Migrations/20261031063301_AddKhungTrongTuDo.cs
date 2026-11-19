using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKhungTrongTuDo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Phiếu mượn chuyển từ "ca" sang giờ mượn tường minh.
            migrationBuilder.AddColumn<TimeOnly>(
                name: "gio_bat_dau",
                table: "DatPhong",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "gio_ket_thuc",
                table: "DatPhong",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.Sql(@"
UPDATE [DatPhong]
SET [gio_bat_dau] = CASE [ca_hoc]
        WHEN 'CA_S2'        THEN '09:40' WHEN 'CA_C1'       THEN '13:00'
        WHEN 'CA_C2'        THEN '15:40' WHEN 'CA_T1'       THEN '18:20'
        WHEN 'CA_TH_CHIEU'  THEN '13:00' ELSE '07:00' END,
    [gio_ket_thuc] = CASE [ca_hoc]
        WHEN 'CA_S2'        THEN '12:10' WHEN 'CA_C1'       THEN '15:30'
        WHEN 'CA_C2'        THEN '18:10' WHEN 'CA_T1'       THEN '20:50'
        WHEN 'CA_TH_CHIEU'  THEN '17:30' ELSE '09:30' END;");

            migrationBuilder.DropColumn(
                name: "ca_hoc",
                table: "DatPhong");

            // 2. Khung giờ trống tự do thay cho bảng khai theo từng ca; giữ lại giờ CSVC đã nhập.
            migrationBuilder.CreateTable(
                name: "Phong_KhungTrong",
                columns: table => new
                {
                    ma_khung_trong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phong = table.Column<int>(type: "int", nullable: false),
                    gio_bat_dau = table.Column<TimeOnly>(type: "time", nullable: false),
                    gio_ket_thuc = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phong_KhungTrong", x => x.ma_khung_trong);
                    table.ForeignKey(
                        name: "FK_Phong_KhungTrong_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
INSERT INTO [Phong_KhungTrong] ([ma_phong], [gio_bat_dau], [gio_ket_thuc])
SELECT [ma_phong], [gio_bat_dau], MAX([gio_ket_thuc])
FROM [Phong_CaTrong]
GROUP BY [ma_phong], [gio_bat_dau];");

            migrationBuilder.CreateIndex(
                name: "IX_Phong_KhungTrong_ma_phong_gio_bat_dau",
                table: "Phong_KhungTrong",
                columns: new[] { "ma_phong", "gio_bat_dau" },
                unique: true);

            migrationBuilder.DropTable(
                name: "Phong_CaTrong");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.Sql(@"
INSERT INTO [Phong_CaTrong] ([ma_phong], [ca_hoc], [gio_bat_dau], [gio_ket_thuc])
SELECT [ma_phong],
       CASE [gio_bat_dau]
           WHEN '09:40' THEN 'CA_S2' WHEN '13:00' THEN 'CA_C1'
           WHEN '15:40' THEN 'CA_C2' WHEN '18:20' THEN 'CA_T1' ELSE 'CA_S1' END,
       [gio_bat_dau], [gio_ket_thuc]
FROM [Phong_KhungTrong];");

            migrationBuilder.DropTable(
                name: "Phong_KhungTrong");

            migrationBuilder.CreateIndex(
                name: "IX_Phong_CaTrong_ma_phong_ca_hoc",
                table: "Phong_CaTrong",
                columns: new[] { "ma_phong", "ca_hoc" },
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "ca_hoc",
                table: "DatPhong",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "CA_S1");

            migrationBuilder.Sql(@"
UPDATE [DatPhong]
SET [ca_hoc] = CASE CAST([gio_bat_dau] AS NVARCHAR(8))
        WHEN '09:40' THEN 'CA_S2' WHEN '13:00' THEN 'CA_C1'
        WHEN '15:40' THEN 'CA_C2' WHEN '18:20' THEN 'CA_T1' ELSE 'CA_S1' END;");

            migrationBuilder.DropColumn(
                name: "gio_bat_dau",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "gio_ket_thuc",
                table: "DatPhong");
        }
    }
}
