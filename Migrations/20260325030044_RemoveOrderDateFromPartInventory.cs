using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transEstrellaInv.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderDateFromPartInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "PartInventories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
            name: "OrderDate",
            table: "PartInventories",
            type: "timestamp with time zone",
            nullable: true);
        }
    }
}
