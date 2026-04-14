using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transEstrellaInv.Migrations
{
    /// <inheritdoc />
    public partial class MovePurchaseFieldsToPartInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartBoughtBy",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PartOrderedBy",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Seller",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "ReceivedDate",
                table: "PartInventories",
                newName: "PurchaseDate");

            migrationBuilder.AlterColumn<string>(
                name: "Seller",
                table: "PartInventories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartBoughtBy",
                table: "PartInventories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartOrderedBy",
                table: "PartInventories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartBoughtBy",
                table: "PartInventories");

            migrationBuilder.DropColumn(
                name: "PartOrderedBy",
                table: "PartInventories");

            migrationBuilder.RenameColumn(
                name: "PurchaseDate",
                table: "PartInventories",
                newName: "ReceivedDate");

            migrationBuilder.AddColumn<string>(
                name: "PartBoughtBy",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartOrderedBy",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seller",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Seller",
                table: "PartInventories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
