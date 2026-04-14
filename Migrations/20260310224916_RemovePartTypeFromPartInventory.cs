using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transEstrellaInv.Migrations
{
    /// <inheritdoc />
    public partial class RemovePartTypeFromPartInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartType",
                table: "PartInventories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartType",
                table: "PartInventories",
                type: "character varying(30)",
                nullable: true);
        }
    }
}
