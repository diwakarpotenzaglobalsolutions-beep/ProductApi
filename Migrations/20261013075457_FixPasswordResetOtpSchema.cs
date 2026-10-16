using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMvcApp.Migrations
{
    /// <inheritdoc />
    public partial class FixPasswordResetOtpSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordResetOtpHash",
                table: "users",
                newName: "ResetOtpHash");

            migrationBuilder.RenameColumn(
                name: "PasswordResetOtpExpiry",
                table: "users",
                newName: "ResetOtpExpiry");

            migrationBuilder.AddColumn<bool>(
                name: "IsOtpVerified",
                table: "users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OtpFailedAttempts",
                table: "users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE users SET OtpFailedAttempts = MAX(0, CAST(COALESCE(PasswordResetOtpAttempts, '0') AS INTEGER));");
            migrationBuilder.DropColumn(
                name: "PasswordResetOtpAttempts",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOtpVerified",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "ResetOtpHash",
                table: "users",
                newName: "PasswordResetOtpHash");

            migrationBuilder.RenameColumn(
                name: "ResetOtpExpiry",
                table: "users",
                newName: "PasswordResetOtpExpiry");

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetOtpAttempts",
                table: "users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE users SET PasswordResetOtpAttempts = CAST(OtpFailedAttempts AS TEXT);");
            migrationBuilder.DropColumn(
                name: "OtpFailedAttempts",
                table: "users");
        }
    }
}
