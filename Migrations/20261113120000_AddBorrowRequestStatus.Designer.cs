using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OWLS.Data;

#nullable disable

namespace OWLS.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261113120000_AddBorrowRequestStatus")]
    partial class AddBorrowRequestStatus
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Snapshot is maintained in ApplicationDbContextModelSnapshot.
        }
    }
}