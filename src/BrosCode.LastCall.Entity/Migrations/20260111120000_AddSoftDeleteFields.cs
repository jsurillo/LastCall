using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrosCode.LastCall.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                schema: "App",
                table: "Drinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "App",
                table: "Drinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedDate",
                schema: "App",
                table: "Drinks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "App",
                table: "Drinks");
        }
    }
}
