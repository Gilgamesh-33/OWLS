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
            new Book { ISBN = "9780140449136", Title = "Noli Me Tangere", Author = "Jose Rizal", Category = "Filipino Literature", YearPublished = 1887, TotalCopies = 20, AvailableCopies = 20, CoverImagePath = "/uploads/Noli_Me_Tangere.jpg" },
            new Book { ISBN = "9789712714351", Title = "El Filibusterismo", Author = "Jose Rizal", Category = "Filipino Literature", YearPublished = 1891, TotalCopies = 20, AvailableCopies = 20, CoverImagePath = "/uploads/el_filibusterismo.webp" },
            new Book { ISBN = "9780743273565", Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Category = "Fiction", YearPublished = 1925, TotalCopies = 20, AvailableCopies = 20, CoverImagePath = "/uploads/The_Great_Gatsby_Cover_1925_Retouched.jpg" },
            new Book { ISBN = "9780132350884", Title = "Clean Code", Author = "Robert C. Martin", Category = "Technology", YearPublished = 2008, TotalCopies = 20, AvailableCopies = 20, CoverImagePath = "/uploads/CleanCode.jpg" },
            new Book { ISBN = "9780061120084", Title = "To Kill a Mockingbird", Author = "Harper Lee", Category = "Fiction", YearPublished = 1960, TotalCopies = 20, AvailableCopies = 20, CoverImagePath = "/uploads/book-cover-To-Kill-a-Mockingbird-many-1961.webp" },
            new Book { ISBN = "9780553380163", Title = "A Brief History of Time", Author = "Stephen Hawking", Category = "Science", YearPublished = 1988, TotalCopies = 20, AvailableCopies = 20, CoverImagePath = "/uploads/A_brief_history_of_time.jpg" },
        };

        // Borrower accounts created on first run so the demo library has a populated user list.
        private static readonly (string FullName, string IdNumber, string Email)[] SeedBorrowers =
        {
            ("Maria Santos", "2026-01001", "maria.santos@dihs.edu.ph"),
            ("Juan Dela Cruz", "2026-01002", "juan.delacruz@dihs.edu.ph"),
            ("Ana Reyes", "2026-01003", "ana.reyes@dihs.edu.ph"),
            ("Jose Bautista", "2026-01004", "jose.bautista@dihs.edu.ph"),
            ("Liza Mendoza", "2026-01005", "liza.mendoza@dihs.edu.ph"),
            ("Carlo Garcia", "2026-01006", "carlo.garcia@dihs.edu.ph"),
            ("Grace Villanueva", "2026-01007", "grace.villanueva@dihs.edu.ph"),
            ("Mark Aquino", "2026-01008", "mark.aquino@dihs.edu.ph"),
            ("Nicole Ramos", "2026-01009", "nicole.ramos@dihs.edu.ph"),
            ("Paolo Castillo", "2026-01010", "paolo.castillo@dihs.edu.ph"),
        };

        private const string BorrowerPassword = "Borrower@123";

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
            await SeedBorrowersAsync(users);
        }

        private static async Task SeedBorrowersAsync(UserManager<ApplicationUser> users)
        {
            foreach (var seed in SeedBorrowers)
            {
                if (await users.FindByEmailAsync(seed.Email) != null) continue;

                var borrower = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    FullName = seed.FullName,
                    IdNumber = seed.IdNumber,
                    EmailConfirmed = true
                };
                var result = await users.CreateAsync(borrower, BorrowerPassword);
                if (result.Succeeded)
                    await users.AddToRoleAsync(borrower, BorrowerRole);
            }
        }

        // Adds missing shared books, fills in missing covers, and tops up the copy count
        // for older installs. Never overwrites a set cover and never touches transactions.
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

                // Bring older installs up to the shared copy count without touching books
                // already at (or above) the target or the number of copies on loan.
                if (book != null && book.TotalCopies < shared.TotalCopies)
                {
                    var extra = shared.TotalCopies - book.TotalCopies;
                    book.TotalCopies += extra;
                    book.AvailableCopies += extra;
                    changed = true;
                }
            }

            if (changed) await db.SaveChangesAsync();
        }
    }
}