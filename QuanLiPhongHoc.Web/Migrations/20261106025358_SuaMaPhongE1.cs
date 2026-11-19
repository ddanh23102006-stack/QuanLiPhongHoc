using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiPhongHoc.Web.Migrations
{
    /// <inheritdoc />
    public partial class SuaMaPhongE1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration trước sinh mã sai dạng E1.2001..E1.4010 do đệm thừa một số 0.
            // Dồn về đúng sơ đồ: E1.201..E1.210, E1.301..E1.310, E1.401..E1.410.
            migrationBuilder.Sql(@"
UPDATE dbo.PhongHoc
SET code_phong = LEFT(code_phong, 4) + SUBSTRING(code_phong, 6, 2)
WHERE LEN(code_phong) = 7
  AND code_phong LIKE 'E1.[234]0[0-9][0-9]';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
