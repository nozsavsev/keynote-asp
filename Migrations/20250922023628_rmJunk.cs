using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace keynote_asp.Migrations
{
    /// <inheritdoc />
    public partial class rmJunk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Keynotes",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Keynotes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Keynotes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Keynotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeynoteUrl",
                table: "Keynotes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MobileKeynoteUrl",
                table: "Keynotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentorNotesUrl",
                table: "Keynotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Keynotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Keynotes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Keynotes");

            migrationBuilder.DropColumn(
                name: "KeynoteUrl",
                table: "Keynotes");

            migrationBuilder.DropColumn(
                name: "MobileKeynoteUrl",
                table: "Keynotes");

            migrationBuilder.DropColumn(
                name: "PresentorNotesUrl",
                table: "Keynotes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Keynotes");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Keynotes",
                newName: "name");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Keynotes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    permissionId = table.Column<long>(type: "bigint", nullable: false),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_permissionId",
                        column: x => x.permissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_key",
                table: "Permissions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_permissionId",
                table: "UserPermissions",
                column: "permissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_userId_permissionId",
                table: "UserPermissions",
                columns: new[] { "userId", "permissionId" },
                unique: true);
        }
    }
}
