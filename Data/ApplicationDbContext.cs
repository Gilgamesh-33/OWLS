using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OWLS.Models;

namespace OWLS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<BorrowTransaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Book>().HasIndex(x => x.ISBN).IsUnique();
            b.Entity<BorrowTransaction>()
                .HasOne(t => t.Book).WithMany(x => x.Transactions)
                .HasForeignKey(t => t.BookId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<BorrowTransaction>()
                .HasOne(t => t.User).WithMany()
                .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
