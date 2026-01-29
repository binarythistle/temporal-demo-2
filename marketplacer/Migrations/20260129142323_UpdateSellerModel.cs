using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace marketplacer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSellerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerIndustry",
                table: "Sellers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPhone",
                table: "Sellers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerIndustry",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "SellerPhone",
                table: "Sellers");
        }
    }
}
