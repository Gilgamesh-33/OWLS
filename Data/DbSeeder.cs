using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OWLS.Models;

namespace OWLS.Data
{
    public static class DbSeeder
    {
        public const string AdminRole = "Admin";
        public const string BorrowerRole = "Borrower";

        public static async Task SeedAsync(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();   // creates OWLS_DB on first run

            var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var r in new[] { AdminRole, BorrowerRole })
                if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            if (await users.FindByEmailAsync("admin@dihs.edu.ph") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@dihs.edu.ph", Email = "admin@dihs.edu.ph",
                    FullName = "Head Librarian", IdNumber = "ADMIN-001", EmailConfirmed = true
                };
                await users.CreateAsync(admin, "Admin@123");
                await users.AddToRoleAsync(admin, AdminRole);
            }

            if (!db.Books.Any())
            {
                db.Books.AddRange(
                    new Book { ISBN = "9780140449136", Title = "Noli Me Tangere", Author = "Jose Rizal", Category = "Filipino Literature", YearPublished = 1887, TotalCopies = 5, AvailableCopies = 5 },
                    new Book { ISBN = "9789712714351", Title = "El Filibusterismo", Author = "Jose Rizal", Category = "Filipino Literature", YearPublished = 1891, TotalCopies = 4, AvailableCopies = 4 },
                    new Book { ISBN = "9780743273565", Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Category = "Fiction", YearPublished = 1925, TotalCopies = 3, AvailableCopies = 3 },
                    new Book { ISBN = "9780132350884", Title = "Clean Code", Author = "Robert C. Martin", Category = "Technology", YearPublished = 2008, TotalCopies = 2, AvailableCopies = 2 },
                    new Book { ISBN = "9780061120084", Title = "To Kill a Mockingbird", Author = "Harper Lee", Category = "Fiction", YearPublished = 1960, TotalCopies = 3, AvailableCopies = 3 },
                    new Book { ISBN = "9780553380163", Title = "A Brief History of Time", Author = "Stephen Hawking", Category = "Science", YearPublished = 1988, TotalCopies = 2, AvailableCopies = 2 });
                await db.SaveChangesAsync();
            }
        }
    }
}
