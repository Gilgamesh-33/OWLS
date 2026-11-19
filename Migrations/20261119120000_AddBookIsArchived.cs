using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OWLS.Migrations
{
    /// <inheritdoc />
    public partial class AddBookIsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded so the migration is safe on databases where the column was already
            // added by another branch's migration (e.g. AddBookArchiveStatus).
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Books]', N'IsArchived') IS NULL
BEGIN
    ALTER TABLE [dbo].[Books] ADD [IsArchived] bit NOT NULL DEFAULT (0);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Books]', N'IsArchived') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Books] DROP COLUMN [IsArchived];
END");
        }
    }
}
