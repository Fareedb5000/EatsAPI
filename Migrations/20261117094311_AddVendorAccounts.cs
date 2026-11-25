using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorRestaurantId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_VendorRestaurantId",
                table: "Users",
                column: "VendorRestaurantId",
                unique: true,
                filter: "[VendorRestaurantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Restaurants_VendorRestaurantId",
                table: "Users",
                column: "VendorRestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Restaurants_VendorRestaurantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_VendorRestaurantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VendorRestaurantId",
                table: "Users");
        }
    }
}
