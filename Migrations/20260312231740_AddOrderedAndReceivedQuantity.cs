using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transEstrellaInv.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderedAndReceivedQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "OrderedQuantity",
                table: "PartInventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "PartInventories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantity",
                table: "PartInventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "PartInventories");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "PartInventories");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "PartInventories");

            migrationBuilder.AddColumn<string>(
                name: "PartType",
                table: "PartInventories",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }
    }
}
