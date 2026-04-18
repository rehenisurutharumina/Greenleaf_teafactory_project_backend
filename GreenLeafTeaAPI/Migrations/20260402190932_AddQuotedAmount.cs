using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenLeafTeaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotedAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "QuotedAmount",
                table: "QuoteRequests",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuotedAmount",
                table: "QuoteRequests");
        }
    }
}
