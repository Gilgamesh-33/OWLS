using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OWLS.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestStatus",
                table: "Transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "Transactions");
        }
    }
}