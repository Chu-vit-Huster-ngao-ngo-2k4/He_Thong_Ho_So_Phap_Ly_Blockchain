using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace prj1.Migrations
{
    /// <inheritdoc />
    public partial class AddFileHashToLegalProfileFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "LegalProfileFiles",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "LegalProfileFiles");
        }
    }
}
