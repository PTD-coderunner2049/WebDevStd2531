using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDevStd2531.Migrations
{
    /// <inheritdoc />
    public partial class orderAnaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");
        }
    }
}
