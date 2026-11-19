using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class UnicodeMaPhongToaNha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CSDL khởi tạo từ Database/QuanLiPhongHoc.sql dùng VARCHAR (Latin1) nên mã phòng
            // gõ tiếng Việt bị thay ký tự có dấu bằng '?'. Chuyển sang NVARCHAR để lưu đúng.
            // Ràng buộc UNIQUE của script phải bỏ tạm rồi tạo lại quanh lệnh ALTER COLUMN.
            migrationBuilder.Sql(DoiSangNvarchar("PhongHoc", "code_phong", 50));
            migrationBuilder.Sql(DoiSangNvarchar("ToaNha", "code_toa_nha", 20));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DoiSangVarchar("PhongHoc", "code_phong", 50));
            migrationBuilder.Sql(DoiSangVarchar("ToaNha", "code_toa_nha", 20));
        }

        private static string DoiSangNvarchar(string bang, string cot, int doDai) => $"""
            IF EXISTS (SELECT 1 FROM sys.columns
                       WHERE object_id = OBJECT_ID(N'dbo.{bang}') AND name = N'{cot}'
                         AND TYPE_NAME(user_type_id) <> N'nvarchar')
            BEGIN
                DECLARE @uq sysname;
                SELECT @uq = kc.name
                FROM sys.key_constraints kc
                WHERE kc.parent_object_id = OBJECT_ID(N'dbo.{bang}')
                  AND kc.type = N'UQ'
                  AND EXISTS (SELECT 1
                              FROM sys.index_columns ic
                              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                              WHERE ic.object_id = kc.parent_object_id
                                AND ic.index_id = kc.unique_index_id
                                AND c.name = N'{cot}');
                IF @uq IS NOT NULL EXEC(N'ALTER TABLE dbo.{bang} DROP CONSTRAINT [' + @uq + N']');

                ALTER TABLE dbo.{bang} ALTER COLUMN {cot} NVARCHAR({doDai}) NOT NULL;

                IF @uq IS NOT NULL EXEC(N'ALTER TABLE dbo.{bang} ADD CONSTRAINT [' + @uq + N'] UNIQUE ({cot})');
            END
            """;

        private static string DoiSangVarchar(string bang, string cot, int doDai) => $"""
            IF EXISTS (SELECT 1 FROM sys.columns
                       WHERE object_id = OBJECT_ID(N'dbo.{bang}') AND name = N'{cot}'
                         AND TYPE_NAME(user_type_id) = N'nvarchar')
            BEGIN
                DECLARE @uq sysname;
                SELECT @uq = kc.name
                FROM sys.key_constraints kc
                WHERE kc.parent_object_id = OBJECT_ID(N'dbo.{bang}')
                  AND kc.type = N'UQ'
                  AND EXISTS (SELECT 1
                              FROM sys.index_columns ic
                              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                              WHERE ic.object_id = kc.parent_object_id
                                AND ic.index_id = kc.unique_index_id
                                AND c.name = N'{cot}');
                IF @uq IS NOT NULL EXEC(N'ALTER TABLE dbo.{bang} DROP CONSTRAINT [' + @uq + N']');

                ALTER TABLE dbo.{bang} ALTER COLUMN {cot} VARCHAR({doDai}) NOT NULL;

                IF @uq IS NOT NULL EXEC(N'ALTER TABLE dbo.{bang} ADD CONSTRAINT [' + @uq + N'] UNIQUE ({cot})');
            END
            """;
    }
}
