using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class QuyTrinhXuLySuCoVaYeuCauSuaChua : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "da_xem_phan_hoi",
                table: "SuCo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "danh_gia",
                table: "SuCo",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ket_qua_xu_ly",
                table: "SuCo",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ngay_xu_ly",
                table: "SuCo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nguoi_xu_ly",
                table: "SuCo",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "phong_dieu_chuyen",
                table: "SuCo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thong_bao_phan_hoi",
                table: "SuCo",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "YeuCauSuaChua",
                columns: table => new
                {
                    ma_yeu_cau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phong_thiet_bi = table.Column<int>(type: "int", nullable: true),
                    ma_phong = table.Column<int>(type: "int", nullable: true),
                    nguoi_yeu_cau = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    nhan_vien_xu_ly = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    noi_dung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ngay_hen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "MOI"),
                    ngay_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ngay_hoan_thanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ket_qua = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauSuaChua", x => x.ma_yeu_cau);
                    table.ForeignKey(
                        name: "FK_YeuCauSuaChua_NguoiDung_nguoi_yeu_cau",
                        column: x => x.nguoi_yeu_cau,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauSuaChua_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong");
                    table.ForeignKey(
                        name: "FK_YeuCauSuaChua_Phong_ThietBi_ma_phong_thiet_bi",
                        column: x => x.ma_phong_thiet_bi,
                        principalTable: "Phong_ThietBi",
                        principalColumn: "ma_phong_thiet_bi",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_nguoi_xu_ly",
                table: "SuCo",
                column: "nguoi_xu_ly");

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_phong_dieu_chuyen",
                table: "SuCo",
                column: "phong_dieu_chuyen");

            migrationBuilder.CreateIndex(
                name: "idx_yeucausuachua_thietbi",
                table: "YeuCauSuaChua",
                column: "ma_phong_thiet_bi");

            migrationBuilder.CreateIndex(
                name: "idx_yeucausuachua_trangthai",
                table: "YeuCauSuaChua",
                column: "trang_thai");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauSuaChua_ma_phong",
                table: "YeuCauSuaChua",
                column: "ma_phong");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauSuaChua_nguoi_yeu_cau",
                table: "YeuCauSuaChua",
                column: "nguoi_yeu_cau");

            migrationBuilder.AddForeignKey(
                name: "FK_SuCo_NguoiDung_nguoi_xu_ly",
                table: "SuCo",
                column: "nguoi_xu_ly",
                principalTable: "NguoiDung",
                principalColumn: "ma_nguoi_dung",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SuCo_PhongHoc_phong_dieu_chuyen",
                table: "SuCo",
                column: "phong_dieu_chuyen",
                principalTable: "PhongHoc",
                principalColumn: "ma_phong");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuCo_NguoiDung_nguoi_xu_ly",
                table: "SuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_SuCo_PhongHoc_phong_dieu_chuyen",
                table: "SuCo");

            migrationBuilder.DropTable(
                name: "YeuCauSuaChua");

            migrationBuilder.DropIndex(
                name: "IX_SuCo_nguoi_xu_ly",
                table: "SuCo");

            migrationBuilder.DropIndex(
                name: "IX_SuCo_phong_dieu_chuyen",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "da_xem_phan_hoi",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "danh_gia",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "ket_qua_xu_ly",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "ngay_xu_ly",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "nguoi_xu_ly",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "phong_dieu_chuyen",
                table: "SuCo");

            migrationBuilder.DropColumn(
                name: "thong_bao_phan_hoi",
                table: "SuCo");
        }
    }
}
