using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace keynote_asp.Migrations
{
    /// <inheritdoc />
    public partial class keynoteUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalFrames",
                table: "Keynotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalFrames",
                table: "Keynotes");
        }
    }
}
