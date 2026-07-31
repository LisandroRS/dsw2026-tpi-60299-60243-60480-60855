using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Availability_Status_And_SoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Availabilities_DoctorId_StartDateTime",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Availabilities");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Availabilities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Availabilities",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_DoctorId_StartDateTime",
                table: "Availabilities",
                columns: new[] { "DoctorId", "StartDateTime" },
                unique: true,
                filter: "[Deleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Availabilities_DoctorId_StartDateTime",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Availabilities");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Availabilities",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_DoctorId_StartDateTime",
                table: "Availabilities",
                columns: new[] { "DoctorId", "StartDateTime" });
        }
    }
}
