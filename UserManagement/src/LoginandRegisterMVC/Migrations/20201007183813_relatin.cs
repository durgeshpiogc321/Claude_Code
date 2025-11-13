using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginandRegisterMVC.Migrations;

/// <inheritdoc />
public partial class relatin : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "usertests");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "usertests",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                desc = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_usertests", x => x.UserId);
                table.ForeignKey(
                    name: "FK_usertests_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_usertests_UserId",
            table: "usertests",
            column: "UserId");
    }
}
