using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDevStd2531.Migrations
{
    /// <inheritdoc />
    public partial class addGrandCategory : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. CREATE the GrandCategory table (The principal table)
            migrationBuilder.CreateTable(
                name: "GrandCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrandCategory", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GrandCategory",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Default Grand Category" }
            );

            migrationBuilder.AddColumn<int>(
                name: "GrandCategoryId",
                table: "Categories",
                type: "int",
                nullable: false, // Because your C# model says 'required'
                defaultValue: 1); // <--- CRITICAL: Must be 1 to match the seeded data

            migrationBuilder.CreateIndex(
                name: "IX_Categories_GrandCategoryId",
                table: "Categories",
                column: "GrandCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_GrandCategory_GrandCategoryId",
                table: "Categories",
                column: "GrandCategoryId",
                principalTable: "GrandCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_GrandCategory_GrandCategoryId",
                table: "Categories");

            migrationBuilder.DropTable(
                name: "GrandCategory");

            migrationBuilder.DropIndex(
                name: "IX_Categories_GrandCategoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "GrandCategoryId",
                table: "Categories");
        }
    }
}
