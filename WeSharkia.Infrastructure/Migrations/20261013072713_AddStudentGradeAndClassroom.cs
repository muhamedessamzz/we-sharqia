using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeSharkia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentGradeAndClassroom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classroom",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Grade",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classroom",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Users");
        }
    }
}
