using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    ma_nguoi_dung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ho_ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mat_khau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    bi_khoa = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ngay_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.ma_nguoi_dung);
                });

            migrationBuilder.CreateTable(
                name: "ThietBi",
                columns: table => new
                {
                    ma_thiet_bi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    loai_phong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ten_thiet_bi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThietBi", x => x.ma_thiet_bi);
                });

            migrationBuilder.CreateTable(
                name: "ToaNha",
                columns: table => new
                {
                    ma_toa_nha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code_toa_nha = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ten_toa_nha = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToaNha", x => x.ma_toa_nha);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    ma_vai_tro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code_vai_tro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ten_vai_tro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.ma_vai_tro);
                });

            migrationBuilder.CreateTable(
                name: "HocPhan",
                columns: table => new
                {
                    ma_hoc_phan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ten_hoc_phan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    so_luong_dang_ky = table.Column<int>(type: "int", nullable: false),
                    ma_giang_vien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    la_thuc_hanh = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocPhan", x => x.ma_hoc_phan);
                    table.ForeignKey(
                        name: "FK_HocPhan_NguoiDung_ma_giang_vien",
                        column: x => x.ma_giang_vien,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHeThong",
                columns: table => new
                {
                    ma_nhat_ky = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    thoi_gian = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ma_nguoi_thuc_hien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    loai_hanh_dong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    doi_tuong_tac_dong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    du_lieu_cu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    du_lieu_moi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dia_chi_ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHeThong", x => x.ma_nhat_ky);
                    table.ForeignKey(
                        name: "FK_NhatKyHeThong_NguoiDung_ma_nguoi_thuc_hien",
                        column: x => x.ma_nguoi_thuc_hien,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhongHoc",
                columns: table => new
                {
                    ma_phong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code_phong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ma_toa_nha = table.Column<int>(type: "int", nullable: true),
                    loai_phong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    suc_chua = table.Column<int>(type: "int", nullable: false),
                    trang_thai_phong = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "AVAILABLE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongHoc", x => x.ma_phong);
                    table.ForeignKey(
                        name: "FK_PhongHoc_ToaNha_ma_toa_nha",
                        column: x => x.ma_toa_nha,
                        principalTable: "ToaNha",
                        principalColumn: "ma_toa_nha",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung_VaiTro",
                columns: table => new
                {
                    ma_nguoi_dung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ma_vai_tro = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung_VaiTro", x => new { x.ma_nguoi_dung, x.ma_vai_tro });
                    table.ForeignKey(
                        name: "FK_NguoiDung_VaiTro_NguoiDung_ma_nguoi_dung",
                        column: x => x.ma_nguoi_dung,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NguoiDung_VaiTro_VaiTro_ma_vai_tro",
                        column: x => x.ma_vai_tro,
                        principalTable: "VaiTro",
                        principalColumn: "ma_vai_tro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonXinNghi",
                columns: table => new
                {
                    ma_don = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_giang_vien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ma_hoc_phan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ly_do = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phuong_an_giai_quyet = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ma_giang_vien_day_thay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    ngay_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    nguoi_duyet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonXinNghi", x => x.ma_don);
                    table.ForeignKey(
                        name: "FK_DonXinNghi_HocPhan_ma_hoc_phan",
                        column: x => x.ma_hoc_phan,
                        principalTable: "HocPhan",
                        principalColumn: "ma_hoc_phan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonXinNghi_NguoiDung_ma_giang_vien",
                        column: x => x.ma_giang_vien,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonXinNghi_NguoiDung_ma_giang_vien_day_thay",
                        column: x => x.ma_giang_vien_day_thay,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonXinNghi_NguoiDung_nguoi_duyet",
                        column: x => x.nguoi_duyet,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatPhong",
                columns: table => new
                {
                    ma_dat_phong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nguoi_yeu_cau = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ma_phong = table.Column<int>(type: "int", nullable: true),
                    ngay_dat = table.Column<DateOnly>(type: "date", nullable: false),
                    ca_hoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    muc_dich = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    so_luong_nguoi_tham_gia = table.Column<int>(type: "int", nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    nguoi_duyet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatPhong", x => x.ma_dat_phong);
                    table.ForeignKey(
                        name: "FK_DatPhong_NguoiDung_nguoi_duyet",
                        column: x => x.nguoi_duyet,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatPhong_NguoiDung_nguoi_yeu_cau",
                        column: x => x.nguoi_yeu_cau,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatPhong_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichHoc",
                columns: table => new
                {
                    ma_lich_hoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_hoc_phan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ma_phong = table.Column<int>(type: "int", nullable: true),
                    ngay_hoc = table.Column<DateOnly>(type: "date", nullable: false),
                    ca_hoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    trang_thai_hoat_dong = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHoc", x => x.ma_lich_hoc);
                    table.ForeignKey(
                        name: "FK_LichHoc_HocPhan_ma_hoc_phan",
                        column: x => x.ma_hoc_phan,
                        principalTable: "HocPhan",
                        principalColumn: "ma_hoc_phan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichHoc_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Phong_ThietBi",
                columns: table => new
                {
                    ma_phong_thiet_bi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phong = table.Column<int>(type: "int", nullable: true),
                    ma_thiet_bi = table.Column<int>(type: "int", nullable: true),
                    nhan_dien_thiet_bi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "GOOD")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phong_ThietBi", x => x.ma_phong_thiet_bi);
                    table.ForeignKey(
                        name: "FK_Phong_ThietBi_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Phong_ThietBi_ThietBi_ma_thiet_bi",
                        column: x => x.ma_thiet_bi,
                        principalTable: "ThietBi",
                        principalColumn: "ma_thiet_bi",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuCo",
                columns: table => new
                {
                    ma_su_co = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_phong = table.Column<int>(type: "int", nullable: true),
                    nguoi_bao_cao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    loai_su_co = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    mo_ta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    muc_do_nghiem_trong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "NEW"),
                    ngay_tao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuCo", x => x.ma_su_co);
                    table.ForeignKey(
                        name: "FK_SuCo_NguoiDung_nguoi_bao_cao",
                        column: x => x.nguoi_bao_cao,
                        principalTable: "NguoiDung",
                        principalColumn: "ma_nguoi_dung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuCo_PhongHoc_ma_phong",
                        column: x => x.ma_phong,
                        principalTable: "PhongHoc",
                        principalColumn: "ma_phong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietSuCo",
                columns: table => new
                {
                    ma_chi_tiet_su_co = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ma_su_co = table.Column<int>(type: "int", nullable: true),
                    ma_phong_thiet_bi = table.Column<int>(type: "int", nullable: true),
                    ghi_chu_loi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietSuCo", x => x.ma_chi_tiet_su_co);
                    table.ForeignKey(
                        name: "FK_ChiTietSuCo_Phong_ThietBi_ma_phong_thiet_bi",
                        column: x => x.ma_phong_thiet_bi,
                        principalTable: "Phong_ThietBi",
                        principalColumn: "ma_phong_thiet_bi",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChiTietSuCo_SuCo_ma_su_co",
                        column: x => x.ma_su_co,
                        principalTable: "SuCo",
                        principalColumn: "ma_su_co");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietSuCo_ma_phong_thiet_bi",
                table: "ChiTietSuCo",
                column: "ma_phong_thiet_bi");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietSuCo_ma_su_co",
                table: "ChiTietSuCo",
                column: "ma_su_co");

            migrationBuilder.CreateIndex(
                name: "IX_DatPhong_ma_phong",
                table: "DatPhong",
                column: "ma_phong");

            migrationBuilder.CreateIndex(
                name: "IX_DatPhong_nguoi_duyet",
                table: "DatPhong",
                column: "nguoi_duyet");

            migrationBuilder.CreateIndex(
                name: "IX_DatPhong_nguoi_yeu_cau",
                table: "DatPhong",
                column: "nguoi_yeu_cau");

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghi_ma_giang_vien",
                table: "DonXinNghi",
                column: "ma_giang_vien");

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghi_ma_giang_vien_day_thay",
                table: "DonXinNghi",
                column: "ma_giang_vien_day_thay");

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghi_ma_hoc_phan",
                table: "DonXinNghi",
                column: "ma_hoc_phan");

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghi_nguoi_duyet",
                table: "DonXinNghi",
                column: "nguoi_duyet");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhan_ma_giang_vien",
                table: "HocPhan",
                column: "ma_giang_vien");

            migrationBuilder.CreateIndex(
                name: "idx_lichhoc_giangvien",
                table: "LichHoc",
                columns: new[] { "ngay_hoc", "ca_hoc" });

            migrationBuilder.CreateIndex(
                name: "idx_unique_phong_lichhoc",
                table: "LichHoc",
                columns: new[] { "ma_phong", "ngay_hoc", "ca_hoc" },
                unique: true,
                filter: "[trang_thai_hoat_dong] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_LichHoc_ma_hoc_phan",
                table: "LichHoc",
                column: "ma_hoc_phan");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_email",
                table: "NguoiDung",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_VaiTro_ma_vai_tro",
                table: "NguoiDung_VaiTro",
                column: "ma_vai_tro");

            migrationBuilder.CreateIndex(
                name: "idx_nhatky_thoigian",
                table: "NhatKyHeThong",
                column: "thoi_gian");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHeThong_ma_nguoi_thuc_hien",
                table: "NhatKyHeThong",
                column: "ma_nguoi_thuc_hien");

            migrationBuilder.CreateIndex(
                name: "idx_phongthietbi_phong",
                table: "Phong_ThietBi",
                column: "ma_phong");

            migrationBuilder.CreateIndex(
                name: "IX_Phong_ThietBi_ma_thiet_bi",
                table: "Phong_ThietBi",
                column: "ma_thiet_bi");

            migrationBuilder.CreateIndex(
                name: "IX_PhongHoc_code_phong",
                table: "PhongHoc",
                column: "code_phong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhongHoc_ma_toa_nha",
                table: "PhongHoc",
                column: "ma_toa_nha");

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_ma_phong",
                table: "SuCo",
                column: "ma_phong");

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_nguoi_bao_cao",
                table: "SuCo",
                column: "nguoi_bao_cao");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_code_toa_nha",
                table: "ToaNha",
                column: "code_toa_nha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_code_vai_tro",
                table: "VaiTro",
                column: "code_vai_tro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietSuCo");

            migrationBuilder.DropTable(
                name: "DatPhong");

            migrationBuilder.DropTable(
                name: "DonXinNghi");

            migrationBuilder.DropTable(
                name: "LichHoc");

            migrationBuilder.DropTable(
                name: "NguoiDung_VaiTro");

            migrationBuilder.DropTable(
                name: "NhatKyHeThong");

            migrationBuilder.DropTable(
                name: "Phong_ThietBi");

            migrationBuilder.DropTable(
                name: "SuCo");

            migrationBuilder.DropTable(
                name: "HocPhan");

            migrationBuilder.DropTable(
                name: "VaiTro");

            migrationBuilder.DropTable(
                name: "ThietBi");

            migrationBuilder.DropTable(
                name: "PhongHoc");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "ToaNha");
        }
    }
}
