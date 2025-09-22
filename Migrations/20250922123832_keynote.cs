using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace keynote_asp.Migrations
{
    /// <inheritdoc />
    public partial class keynote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransitionType",
                table: "Keynotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransitionType",
                table: "Keynotes");
        }
    }
}
