using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsActiveToDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Specialities");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Specialities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Specialities");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Doctors");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Specialities",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
    }
}
