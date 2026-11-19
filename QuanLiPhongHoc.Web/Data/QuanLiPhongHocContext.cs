using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;

namespace QuanLiPhongHoc.Web.Data;

public class QuanLiPhongHocContext : DbContext
{
    public QuanLiPhongHocContext(DbContextOptions<QuanLiPhongHocContext> options) : base(options)
    {
    }

    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<VaiTro> VaiTros => Set<VaiTro>();
    public DbSet<NguoiDungVaiTro> NguoiDungVaiTros => Set<NguoiDungVaiTro>();
    public DbSet<ToaNha> ToaNhas => Set<ToaNha>();
    public DbSet<PhongHoc> PhongHocs => Set<PhongHoc>();
    public DbSet<PhongKhungTrong> PhongKhungTrongs => Set<PhongKhungTrong>();
    public DbSet<ThietBi> ThietBis => Set<ThietBi>();
    public DbSet<PhongThietBi> PhongThietBis => Set<PhongThietBi>();
    public DbSet<HocPhan> HocPhans => Set<HocPhan>();
    public DbSet<LichHoc> LichHocs => Set<LichHoc>();
    public DbSet<DatPhong> DatPhongs => Set<DatPhong>();
    public DbSet<SuCo> SuCos => Set<SuCo>();
    public DbSet<ChiTietSuCo> ChiTietSuCos => Set<ChiTietSuCo>();
    public DbSet<YeuCauSuaChua> YeuCauSuaChuas => Set<YeuCauSuaChua>();
    public DbSet<DonXinNghi> DonXinNghis => Set<DonXinNghi>();
    public DbSet<NhatKyHeThong> NhatKyHeThongs => Set<NhatKyHeThong>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<DatLaiMatKhau> MatKhauDatLais => Set<DatLaiMatKhau>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NguoiDung>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.BiKhoa).HasDefaultValue(false);
            e.Property(x => x.NgayTao).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<VaiTro>(e =>
        {
            e.HasIndex(x => x.CodeVaiTro).IsUnique();
            e.Property(x => x.MaVaiTro).UseIdentityColumn();
        });

        modelBuilder.Entity<NguoiDungVaiTro>(e =>
        {
            e.HasKey(x => new { x.MaNguoiDung, x.MaVaiTro });
            e.HasOne(x => x.NguoiDung).WithMany(x => x.NguoiDungVaiTros)
                .HasForeignKey(x => x.MaNguoiDung).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.VaiTro).WithMany(x => x.NguoiDungVaiTros)
                .HasForeignKey(x => x.MaVaiTro).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToaNha>(e =>
        {
            e.HasIndex(x => x.CodeToaNha).IsUnique();
            e.Property(x => x.MaToaNha).UseIdentityColumn();
            e.Property(x => x.TrangThaiHoatDong).HasDefaultValue("ACTIVE");
        });

        modelBuilder.Entity<PhongHoc>(e =>
        {
            e.HasIndex(x => x.CodePhong).IsUnique();
            e.Property(x => x.MaPhong).UseIdentityColumn();
            e.Property(x => x.TrangThaiPhong).HasDefaultValue("AVAILABLE");
            e.Property(x => x.HienThiDangKyMuon).HasDefaultValue(true);
            e.HasOne(x => x.ToaNha).WithMany(x => x.PhongHocs)
                .HasForeignKey(x => x.MaToaNha).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ThietBi>().Property(x => x.MaThietBi).UseIdentityColumn();
        modelBuilder.Entity<ThietBi>().HasIndex(x => x.MaCodeThietBi)
            .IsUnique()
            .HasFilter("[ma_code_thiet_bi] IS NOT NULL")
            .HasDatabaseName("idx_thietbi_ma_code");

        modelBuilder.Entity<PhongKhungTrong>(e =>
        {
            e.Property(x => x.MaKhungTrong).UseIdentityColumn();
            e.HasIndex(x => new { x.MaPhong, x.GioBatDau }).IsUnique();
            e.HasOne(x => x.PhongHoc).WithMany(x => x.KhungTrongs)
                .HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhongThietBi>(e =>
        {
            e.Property(x => x.MaPhongThietBi).UseIdentityColumn();
            e.Property(x => x.TrangThai).HasDefaultValue("GOOD");
            e.HasOne(x => x.PhongHoc).WithMany(x => x.PhongThietBis)
                .HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ThietBi).WithMany()
                .HasForeignKey(x => x.MaThietBi).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.MaPhong).HasDatabaseName("idx_phongthietbi_phong");
        });

        modelBuilder.Entity<HocPhan>(e =>
        {
            e.HasOne(x => x.GiangVien).WithMany()
                .HasForeignKey(x => x.MaGiangVien).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.LaThucHanh).HasDefaultValue(false);
        });

        modelBuilder.Entity<LichHoc>(e =>
        {
            e.Property(x => x.MaLichHoc).UseIdentityColumn();
            e.Property(x => x.TrangThaiHoatDong).HasDefaultValue(true);
            e.HasIndex(x => x.MaGiangVienDayThay).HasDatabaseName("idx_lichhoc_giangvien_thay");
            e.HasOne(x => x.HocPhan).WithMany(x => x.LichHocs)
                .HasForeignKey(x => x.MaHocPhan).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PhongHoc).WithMany(x => x.LichHocs)
                .HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GiangVienDayThay).WithMany()
                .HasForeignKey(x => x.MaGiangVienDayThay).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.MaPhong, x.NgayHoc, x.CaHoc })
                .IsUnique()
                .HasFilter("[trang_thai_hoat_dong] = 1")
                .HasDatabaseName("idx_unique_phong_lichhoc");
            e.HasIndex(x => new { x.NgayHoc, x.CaHoc }).HasDatabaseName("idx_lichhoc_giangvien");
        });

        modelBuilder.Entity<DatPhong>(e =>
        {
            e.Property(x => x.MaDatPhong).UseIdentityColumn();
            e.Property(x => x.TrangThai).HasDefaultValue("PENDING");
            e.Property(x => x.DaXemKetQua).HasDefaultValue(false);
            e.HasOne(x => x.NguoiYeuCauNav).WithMany()
                .HasForeignKey(x => x.NguoiYeuCau).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiDuyetNav).WithMany()
                .HasForeignKey(x => x.NguoiDuyet).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PhongHoc).WithMany()
                .HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SuCo>(e =>
        {
            e.Property(x => x.MaSuCo).UseIdentityColumn();
            e.Property(x => x.TrangThai).HasDefaultValue("NEW");
            e.Property(x => x.NgayTao).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.DaXemPhanHoi).HasDefaultValue(false);
            e.HasOne(x => x.PhongHoc).WithMany()
                .HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.NguoiBaoCaoNav).WithMany()
                .HasForeignKey(x => x.NguoiBaoCao).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiXuLyNav).WithMany()
                .HasForeignKey(x => x.NguoiXuLy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PhongDieuChuyenNav).WithMany()
                .HasForeignKey(x => x.PhongDieuChuyen).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<YeuCauSuaChua>(e =>
        {
            e.Property(x => x.MaYeuCau).UseIdentityColumn();
            e.Property(x => x.TrangThai).HasDefaultValue("MOI");
            e.Property(x => x.NgayTao).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.PhongThietBi).WithMany()
                .HasForeignKey(x => x.MaPhongThietBi).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PhongHoc).WithMany()
                .HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.NguoiYeuCauNav).WithMany()
                .HasForeignKey(x => x.NguoiYeuCau).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.MaPhongThietBi).HasDatabaseName("idx_yeucausuachua_thietbi");
            e.HasIndex(x => x.TrangThai).HasDatabaseName("idx_yeucausuachua_trangthai");
        });

        modelBuilder.Entity<ChiTietSuCo>(e =>
        {
            e.Property(x => x.MaChiTietSuCo).UseIdentityColumn();
            e.HasOne(x => x.SuCo).WithMany(x => x.ChiTietSuCos)
                .HasForeignKey(x => x.MaSuCo).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PhongThietBi).WithMany()
                .HasForeignKey(x => x.MaPhongThietBi).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DonXinNghi>(e =>
        {
            e.Property(x => x.MaDon).UseIdentityColumn();
            e.Property(x => x.TrangThai).HasDefaultValue("PENDING");
            e.Property(x => x.NgayTao).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.GiangVien).WithMany()
                .HasForeignKey(x => x.MaGiangVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GiangVienDayThay).WithMany()
                .HasForeignKey(x => x.MaGiangVienDayThay).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiDuyetNav).WithMany()
                .HasForeignKey(x => x.NguoiDuyet).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HocPhan).WithMany()
                .HasForeignKey(x => x.MaHocPhan).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NhatKyHeThong>(e =>
        {
            e.Property(x => x.MaNhatKy).UseIdentityColumn();
            e.Property(x => x.ThoiGian).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.NguoiThucHien).WithMany()
                .HasForeignKey(x => x.MaNguoiThucHien).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ThoiGian).HasDatabaseName("idx_nhatky_thoigian");
        });

        modelBuilder.Entity<ThongBao>(e =>
        {
            e.Property(x => x.MaThongBao).UseIdentityColumn();
            e.Property(x => x.DoiTuong).HasDefaultValue(ThongBaoDoiTuong.All);
            e.Property(x => x.TrangThai).HasDefaultValue("PUBLISHED");
            e.Property(x => x.NgayDang).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.TrangThai, x.NgayDang }).HasDatabaseName("idx_thongbao_hien_thi");
        });

        modelBuilder.Entity<DatLaiMatKhau>(e =>
        {
            e.Property(x => x.MaDatLai).UseIdentityColumn();
            e.Property(x => x.NgayTao).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("idx_datlaipassword_token");
            e.HasIndex(x => x.MaNguoiDung).HasDatabaseName("idx_datlaipassword_nguoidung");
        });
    }
}
