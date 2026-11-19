using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OWLS.Models;

namespace OWLS.Data
{
    public static class DbSeeder
    {
        public const string AdminRole = "Admin";
        public const string BorrowerRole = "Borrower";

        // ------------------------------------------------------------------
        // SHARED BOOK LIST  (this is what every teammate gets after cloning)
        //
        // Every teammate has their OWN local database, so a book added through the
        // website only exists on the laptop that added it. Books listed here are
        // created (or have their cover fixed) automatically every time the app starts.
        //
        // To share a NEW book with the whole group:
        //   1. Add the book + cover in the website.
        //   2. Add one line below with the same ISBN and the cover path
        //      (e.g. "/uploads/covers/xxxxxxxx.jpg" - the file is inside wwwroot/uploads/covers).
        //   3. git add / commit / push BOTH this file and the image.
        // ------------------------------------------------------------------
        private static readonly Book[] SharedBooks =
        {
            new Book { ISBN = "9780140449136", Title = "Noli Me Tangere", Author = "Jose Rizal", Category = "Filipino Literature", YearPublished = 1887, TotalCopies = 5, AvailableCopies = 5, CoverImagePath = "/uploads/Noli_Me_Tangere.jpg" },
            new Book { ISBN = "9789712714351", Title = "El Filibusterismo", Author = "Jose Rizal", Category = "Filipino Literature", YearPublished = 1891, TotalCopies = 4, AvailableCopies = 4, CoverImagePath = "/uploads/el_filibusterismo.webp" },
            new Book { ISBN = "9780743273565", Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Category = "Fiction", YearPublished = 1925, TotalCopies = 3, AvailableCopies = 3, CoverImagePath = "/uploads/The_Great_Gatsby_Cover_1925_Retouched.jpg" },
            new Book { ISBN = "9780132350884", Title = "Clean Code", Author = "Robert C. Martin", Category = "Technology", YearPublished = 2008, TotalCopies = 2, AvailableCopies = 2, CoverImagePath = "/uploads/CleanCode.jpg" },
            new Book { ISBN = "9780061120084", Title = "To Kill a Mockingbird", Author = "Harper Lee", Category = "Fiction", YearPublished = 1960, TotalCopies = 3, AvailableCopies = 3, CoverImagePath = "/uploads/book-cover-To-Kill-a-Mockingbird-many-1961.webp" },
            new Book { ISBN = "9780553380163", Title = "A Brief History of Time", Author = "Stephen Hawking", Category = "Science", YearPublished = 1988, TotalCopies = 2, AvailableCopies = 2, CoverImagePath = "/uploads/A_brief_history_of_time.jpg" },
        };

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
                    UserName = "admin@dihs.edu.ph",
                    Email = "admin@dihs.edu.ph",
                    FullName = "Head Librarian",
                    IdNumber = "ADMIN-001",
                    EmailConfirmed = true
                };
                await users.CreateAsync(admin, "Admin@123");
                await users.AddToRoleAsync(admin, AdminRole);
            }

            await SyncSharedBooksAsync(db);
        }

        // Adds missing shared books and fills in missing covers. Never overwrites
        // a cover that is already set, and never touches copies / transactions.
        private static async Task SyncSharedBooksAsync(ApplicationDbContext db)
        {
            var existing = await db.Books.ToListAsync();
            var changed = false;

            foreach (var shared in SharedBooks)
            {
                var book = existing.FirstOrDefault(b => b.ISBN == shared.ISBN);
                if (book == null)
                {
                    db.Books.Add(new Book
                    {
                        ISBN = shared.ISBN,
                        Title = shared.Title,
                        Author = shared.Author,
                        Category = shared.Category,
                        YearPublished = shared.YearPublished,
                        TotalCopies = shared.TotalCopies,
                        AvailableCopies = shared.AvailableCopies,
                        CoverImagePath = shared.CoverImagePath
                    });
                    changed = true;
                }
                else if (string.IsNullOrWhiteSpace(book.CoverImagePath) && !string.IsNullOrWhiteSpace(shared.CoverImagePath))
                {
                    book.CoverImagePath = shared.CoverImagePath;
                    changed = true;
                }
            }

            if (changed) await db.SaveChangesAsync();
        }
    }
}