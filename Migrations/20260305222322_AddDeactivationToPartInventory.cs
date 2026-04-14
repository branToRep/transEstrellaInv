using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transEstrellaInv.Migrations
{
    /// <inheritdoc />
    public partial class AddDeactivationToPartInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "PartInventories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PartInventories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "PartInventories",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "PartInventories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PartInventories");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "PartInventories");
        }
    }
}
