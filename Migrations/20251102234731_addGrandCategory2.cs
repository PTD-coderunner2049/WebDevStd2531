using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDevStd2531.Migrations
{
    /// <inheritdoc />
    public partial class addGrandCategory2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_GrandCategory_GrandCategoryId",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GrandCategory",
                table: "GrandCategory");

            migrationBuilder.RenameTable(
                name: "GrandCategory",
                newName: "GrandCategories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrandCategories",
                table: "GrandCategories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_GrandCategories_GrandCategoryId",
                table: "Categories",
                column: "GrandCategoryId",
                principalTable: "GrandCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_GrandCategories_GrandCategoryId",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GrandCategories",
                table: "GrandCategories");

            migrationBuilder.RenameTable(
                name: "GrandCategories",
                newName: "GrandCategory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrandCategory",
                table: "GrandCategory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_GrandCategory_GrandCategoryId",
                table: "Categories",
                column: "GrandCategoryId",
                principalTable: "GrandCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
