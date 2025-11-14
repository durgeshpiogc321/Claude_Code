using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LoginandRegisterMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleName);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleName", "CreatedAt", "Description", "IsSystemRole", "UpdatedAt" },
                values: new object[,]
                {
                    { "Admin", new DateTime(2025, 11, 13, 16, 1, 56, 775, DateTimeKind.Utc).AddTicks(5985), "System administrator with full access", true, new DateTime(2025, 11, 13, 16, 1, 56, 775, DateTimeKind.Utc).AddTicks(5985) },
                    { "User", new DateTime(2025, 11, 13, 16, 1, 56, 775, DateTimeKind.Utc).AddTicks(5989), "Standard user with limited access", true, new DateTime(2025, 11, 13, 16, 1, 56, 775, DateTimeKind.Utc).AddTicks(5990) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
