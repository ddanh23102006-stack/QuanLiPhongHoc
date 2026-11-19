using Microsoft.EntityFrameworkCore;
using QuanLiPhongHoc.Web.Models;
using QuanLiPhongHoc.Web.Models.Domain;
using QuanLiPhongHoc.Web.Services;

namespace QuanLiPhongHoc.Web.Data;

/// <summary>
/// Khởi tạo dữ liệu mẫu khớp với kịch bản Database/QuanLiPhongHoc.sql.
/// Vai trò và tài khoản nhân viên cơ sở vật chất luôn được đảm bảo;
/// phần dữ liệu demo còn lại chỉ seed một lần khi CSDL còn trống.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<QuanLiPhongHocContext>();

        // Tự động fix lỗi nếu DB đã được tạo từ SQL script nhưng chưa có lịch sử migration.
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
                BEGIN
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32) NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );
                END;

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261026175539_InitialCreate')
                   AND EXISTS (SELECT * FROM sys.tables WHERE name = 'NguoiDung')
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261026175539_InitialCreate', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261030042401_AddThongBao')
                   AND EXISTS (SELECT * FROM sys.tables WHERE name = 'ThongBao')
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261030042401_AddThongBao', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261030121726_AddHoSoCaNhan')
                   AND COL_LENGTH('dbo.NguoiDung', 'so_dien_thoai') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261030121726_AddHoSoCaNhan', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261031041615_AddPhongCaTrong')
                   AND EXISTS (SELECT * FROM sys.tables WHERE name = 'Phong_CaTrong')
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261031041615_AddPhongCaTrong', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261031063301_AddKhungTrongTuDo')
                   AND EXISTS (SELECT * FROM sys.tables WHERE name = 'Phong_KhungTrong')
                   AND COL_LENGTH('dbo.DatPhong', 'ca_hoc') IS NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261031063301_AddKhungTrongTuDo', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261104103548_AddHienThiDangKyMuon')
                   AND COL_LENGTH('dbo.PhongHoc', 'hien_thi_dang_ky_muon') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261104103548_AddHienThiDangKyMuon', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261104105424_AddKetQuaXuLyDatPhong')
                   AND COL_LENGTH('dbo.DatPhong', 'thoi_diem_xu_ly') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261104105424_AddKetQuaXuLyDatPhong', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261106032127_ChuanHoaCoSoVatChatTheoNganh')
                   AND COL_LENGTH('dbo.HocPhan', 'nhom_nganh') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261106032127_ChuanHoaCoSoVatChatTheoNganh', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261106042341_QuanLyToaNhaVaThietBi')
                   AND COL_LENGTH('dbo.ToaNha', 'dia_chi') IS NOT NULL
                   AND COL_LENGTH('dbo.ThietBi', 'ma_code_thiet_bi') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261106042341_QuanLyToaNhaVaThietBi', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261106051926_QuyTrinhXuLySuCoVaYeuCauSuaChua')
                   AND OBJECT_ID(N'dbo.YeuCauSuaChua') IS NOT NULL
                   AND COL_LENGTH('dbo.SuCo', 'danh_gia') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261106051926_QuyTrinhXuLySuCoVaYeuCauSuaChua', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261106120943_ThemThongTinBaoCaoSuCo')
                   AND COL_LENGTH('dbo.SuCo', 'anh_dinh_kem') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261106120943_ThemThongTinBaoCaoSuCo', '9.0.0');
                END

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261107113514_DieuPhoiThuCongLichHoc')
                   AND COL_LENGTH('dbo.LichHoc', 'ghi_chu_dieu_phoi') IS NOT NULL
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20261107113514_DieuPhoiThuCongLichHoc', '9.0.0');
                END
            ");
        }
        catch { /* Bỏ qua lỗi nếu không có quyền truy cập sys.tables hoặc bảng history đã tồn tại */ }

        await db.Database.MigrateAsync();

        await EnsureReferenceDataAsync(db);
        await NangCapMatKhauCuAsync(db);

        var coSan = await db.ThongBaos.Select(x => x.TieuDe).ToListAsync();
        var conThieu = ThongBaoSeedData.Build("ST501")
            .Where(x => !coSan.Contains(x.TieuDe))
            .ToList();
        if (conThieu.Count > 0)
        {
            db.ThongBaos.AddRange(conThieu);
            await db.SaveChangesAsync();
        }

        // Dữ liệu demo chỉ nạp một lần (nhận biết qua tài khoản mẫu SV202601).
        if (await db.NguoiDungs.AnyAsync(x => x.MaNguoiDung == "SV202601"))
        {
            await NapHoSoMauAsync(db);
            return;
        }

        var vaiTroId = await db.VaiTros.ToDictionaryAsync(x => x.CodeVaiTro, x => x.MaVaiTro);

        var nguoiDungs = new List<NguoiDung>
        {
            new() { MaNguoiDung = "SV202601", HoTen = "Nguyễn Văn Sinh", Email = "sv202601@school.edu.vn", MatKhau = MatKhauHasher.Bam("123456") },
            new() { MaNguoiDung = "GV1005", HoTen = "TS. Trần Thị Giảng", Email = "tranthigiang@school.edu.vn", MatKhau = MatKhauHasher.Bam("123456") },
            new() { MaNguoiDung = "GV1006", HoTen = "ThS. Lê Minh Khoa", Email = "leminhkhoa@school.edu.vn", MatKhau = MatKhauHasher.Bam("123456") },
            new() { MaNguoiDung = "ST501", HoTen = "Phạm Văn Đào Tạo", Email = "daotao@school.edu.vn", MatKhau = MatKhauHasher.Bam("123456") },
            new() { MaNguoiDung = "AD01", HoTen = "Quản Trị Viên Hệ Thống", Email = "admin.sys@school.edu.vn", MatKhau = MatKhauHasher.Bam("123456") }
        };
        db.NguoiDungs.AddRange(nguoiDungs);
        await db.SaveChangesAsync();

        db.NguoiDungVaiTros.AddRange(
            new NguoiDungVaiTro { MaNguoiDung = "SV202601", MaVaiTro = vaiTroId[AppRoles.Student] },
            new NguoiDungVaiTro { MaNguoiDung = "GV1005", MaVaiTro = vaiTroId[AppRoles.Lecturer] },
            new NguoiDungVaiTro { MaNguoiDung = "GV1006", MaVaiTro = vaiTroId[AppRoles.Lecturer] },
            new NguoiDungVaiTro { MaNguoiDung = "ST501", MaVaiTro = vaiTroId[AppRoles.Training] },
            new NguoiDungVaiTro { MaNguoiDung = "AD01", MaVaiTro = vaiTroId[AppRoles.Admin] });
        await db.SaveChangesAsync();

        // Danh mục tòa nhà/phòng học và thiết bị chuẩn do migration CSVC nạp sẵn.
        var tatCaThietBi = await db.ThietBis.ToListAsync();
        var pIt = (await db.PhongHocs.FirstAsync(x => x.CodePhong == "E1.201")).MaPhong;
        var pTheory = (await db.PhongHocs.FirstAsync(x => x.CodePhong == "A1.101")).MaPhong;
        var tbWorkstation = tatCaThietBi.First(x => x.TenThietBi == "Máy tính để bàn").MaThietBi;
        var tbKeyboard = tatCaThietBi.First(x => x.TenThietBi == "Bàn phím & chuột").MaThietBi;

        var phongThietBis = new List<PhongThietBi>
        {
            new() { MaPhong = pIt, MaThietBi = tbWorkstation, NhanDienThietBi = "Máy 01", TrangThai = "GOOD" },
            new() { MaPhong = pIt, MaThietBi = tbWorkstation, NhanDienThietBi = "Máy 15", TrangThai = "DAMAGED" },
            new() { MaPhong = pIt, MaThietBi = tbKeyboard, NhanDienThietBi = "Bàn phím Máy 15", TrangThai = "DAMAGED" }
        };
        db.PhongThietBis.AddRange(phongThietBis);
        await db.SaveChangesAsync();

        var hocPhans = new List<HocPhan>
        {
            new() { MaHocPhan = "INT101_01", TenHocPhan = "Lập trình căn bản", SoLuongDangKy = 45, MaGiangVien = "GV1005", LaThucHanh = true, NhomNganh = Catalogs.NganhCntt },
            new() { MaHocPhan = "MAT202_02", TenHocPhan = "Toán rời rạc", SoLuongDangKy = 75, MaGiangVien = "GV1005", LaThucHanh = false, NhomNganh = Catalogs.NganhDaiCuong }
        };
        db.HocPhans.AddRange(hocPhans);
        await db.SaveChangesAsync();

        db.LichHocs.AddRange(
            new LichHoc { MaHocPhan = "INT101_01", MaPhong = pIt, NgayHoc = new DateOnly(2026, 6, 10), CaHoc = "CA_TH_SANG", TrangThaiHoatDong = true },
            new LichHoc { MaHocPhan = "MAT202_02", MaPhong = pTheory, NgayHoc = new DateOnly(2026, 6, 10), CaHoc = "CA_S1", TrangThaiHoatDong = true });

        db.DatPhongs.Add(new DatPhong
        {
            NguoiYeuCau = "SV202601",
            MaPhong = pTheory,
            NgayDat = new DateOnly(2026, 6, 15),
            GioBatDau = new TimeOnly(7, 0),
            GioKetThuc = new TimeOnly(9, 45),
            MucDich = "Đặt phòng học nhóm ôn tập",
            SoLuongNguoiThamGia = 15,
            TrangThai = "PENDING"
        });

        var suCo = new SuCo
        {
            MaPhong = pIt,
            NguoiBaoCao = "GV1005",
            LoaiSuCo = "SU_CO_MAY_TINH",
            MoTa = "Máy tính bàn số 15 không lên nguồn",
            MucDoNghiemTrong = "HIGH",
            TrangThai = "NEW"
        };
        db.SuCos.Add(suCo);
        await db.SaveChangesAsync();

        var may15 = phongThietBis.First(x => x.NhanDienThietBi == "Máy 15");
        db.ChiTietSuCos.Add(new ChiTietSuCo
        {
            MaSuCo = suCo.MaSuCo,
            MaPhongThietBi = may15.MaPhongThietBi,
            GhiChuLoi = "Hỏng nguồn phần cứng máy tính số 15"
        });

        db.DonXinNghis.Add(new DonXinNghi
        {
            MaGiangVien = "GV1005",
            MaHocPhan = "INT101_01",
            LyDo = "Giảng viên đi công tác đột xuất",
            PhuongAnGiaiQuyet = "MAKEUP_CLASS",
            TrangThai = "PENDING"
        });

        db.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            MaNguoiThucHien = "AD01",
            LoaiHanhDong = "ACTION_UPDATE",
            DoiTuongTacDong = "NguoiDung",
            DuLieuCu = "Trang thai cu: Hoat dong",
            DuLieuMoi = "Trang thai moi: Bi khoa",
            DiaChiIp = "192.168.1.50"
        });

        await db.SaveChangesAsync();

        await NapHoSoMauAsync(db);
    }

    /// <summary>
    /// Nạp hồ sơ cá nhân mẫu lấy nguyên văn từ <see cref="HoSoSeedData"/>, vốn là bản sao của khối
    /// INSERT INTO NguoiDung trong Database/QuanLiPhongHoc.sql. Chỉ điền tài khoản còn trống để
    /// không ghi đè hồ sơ đã chỉnh sửa tay.
    /// </summary>
    private static async Task NapHoSoMauAsync(QuanLiPhongHocContext db)
    {
        var maCoDuLieuMau = HoSoSeedData.DanhSach.Select(x => x.MaNguoiDung).ToArray();
        var conThieu = await db.NguoiDungs
            .Where(x => x.NgaySinh == null && maCoDuLieuMau.Contains(x.MaNguoiDung))
            .ToListAsync();
        if (conThieu.Count == 0) return;

        foreach (var u in conThieu)
        {
            var hoSo = HoSoSeedData.DanhSach.First(x => x.MaNguoiDung == u.MaNguoiDung);
            u.SoDienThoai = hoSo.SoDienThoai;
            u.NgaySinh = hoSo.NgaySinh;
            u.GioiTinh = hoSo.GioiTinh;
            u.NoiSinh = hoSo.NoiSinh;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Băm lại các mật khẩu còn lưu dạng thô (tạo trước khi áp dụng PBKDF2, hoặc từ script SQL)
    /// để CSDL không còn giữ mật khẩu đọc được ngay sau lần khởi động đầu tiên.
    /// </summary>
    private static async Task NangCapMatKhauCuAsync(QuanLiPhongHocContext db)
    {
        var tatCa = await db.NguoiDungs.ToListAsync();
        var canNangCap = tatCa.Where(x => !MatKhauHasher.DaBam(x.MatKhau)).ToList();
        if (canNangCap.Count == 0) return;

        foreach (var u in canNangCap)
            u.MatKhau = MatKhauHasher.Bam(u.MatKhau);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Đảm bảo bảng VaiTro luôn có đủ 5 nhóm quyền và tài khoản mẫu của Nhân viên cơ sở vật chất.
    /// Xử lý cả CSDL tạo từ phiên bản trước, khi CSVC và Đào tạo còn dùng chung vai trò ROLE_STAFF.
    /// </summary>
    private static async Task EnsureReferenceDataAsync(QuanLiPhongHocContext db)
    {
        // Tách vai trò cũ ROLE_STAFF (gộp CSVC + Đào tạo) thành ROLE_TRAINING.
        var legacyStaff = await db.VaiTros.FirstOrDefaultAsync(x => x.CodeVaiTro == "ROLE_STAFF");
        if (legacyStaff is not null)
        {
            legacyStaff.CodeVaiTro = AppRoles.Training;
            legacyStaff.TenVaiTro = "Nhân viên đào tạo";
            await db.SaveChangesAsync();
        }

        var required = new (string Code, string Ten)[]
        {
            (AppRoles.Student, "Sinh viên"),
            (AppRoles.Lecturer, "Giảng viên"),
            (AppRoles.Facility, "Nhân viên cơ sở vật chất"),
            (AppRoles.Training, "Nhân viên đào tạo"),
            (AppRoles.Admin, "Quản trị hệ thống")
        };
        var existingCodes = await db.VaiTros.Select(x => x.CodeVaiTro).ToListAsync();
        db.VaiTros.AddRange(required.Where(r => !existingCodes.Contains(r.Code))
            .Select(r => new VaiTro { CodeVaiTro = r.Code, TenVaiTro = r.Ten }));
        await db.SaveChangesAsync();

        // Tài khoản mẫu của Nhân viên cơ sở vật chất.
        if (!await db.NguoiDungs.AnyAsync(x => x.MaNguoiDung == "CSVC501"))
        {
            var facility = await db.VaiTros.FirstAsync(x => x.CodeVaiTro == AppRoles.Facility);
            db.NguoiDungs.Add(new NguoiDung
            {
                MaNguoiDung = "CSVC501",
                HoTen = "Nguyễn Văn Cơ Sở",
                Email = "coso@school.edu.vn",
                MatKhau = MatKhauHasher.Bam("123456")
            });
            db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = "CSVC501", MaVaiTro = facility.MaVaiTro });
            await db.SaveChangesAsync();
        }
    }
}
