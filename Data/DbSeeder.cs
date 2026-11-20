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
            // ---- 20 additional titles across 14 new genres (no cover file needed: the catalog draws one) ----
            new Book { ISBN = "9780547928227", Title = "The Hobbit", Author = "J.R.R. Tolkien", Category = "Fantasy", YearPublished = 1937, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780590353427", Title = "Harry Potter and the Sorcerer's Stone", Author = "J.K. Rowling", Category = "Fantasy", YearPublished = 1997, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780441013593", Title = "Dune", Author = "Frank Herbert", Category = "Science Fiction", YearPublished = 1965, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780812550702", Title = "Ender's Game", Author = "Orson Scott Card", Category = "Science Fiction", YearPublished = 1985, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780451524935", Title = "1984", Author = "George Orwell", Category = "Science Fiction", YearPublished = 1949, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780062073488", Title = "And Then There Were None", Author = "Agatha Christie", Category = "Mystery", YearPublished = 1939, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780062693662", Title = "Murder on the Orient Express", Author = "Agatha Christie", Category = "Mystery", YearPublished = 1934, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780486282114", Title = "Frankenstein", Author = "Mary Shelley", Category = "Horror", YearPublished = 1818, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780141439518", Title = "Pride and Prejudice", Author = "Jane Austen", Category = "Romance", YearPublished = 1813, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780375842207", Title = "The Book Thief", Author = "Markus Zusak", Category = "Historical Fiction", YearPublished = 2005, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780553296983", Title = "The Diary of a Young Girl", Author = "Anne Frank", Category = "Biography", YearPublished = 1947, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780316322409", Title = "I Am Malala", Author = "Malala Yousafzai", Category = "Biography", YearPublished = 2013, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780735211292", Title = "Atomic Habits", Author = "James Clear", Category = "Self-Help", YearPublished = 2018, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780857197689", Title = "The Psychology of Money", Author = "Morgan Housel", Category = "Business", YearPublished = 2020, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780807014295", Title = "Man's Search for Meaning", Author = "Viktor E. Frankl", Category = "Philosophy", YearPublished = 1946, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780394404288", Title = "The Prophet", Author = "Kahlil Gibran", Category = "Poetry", YearPublished = 1923, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780439023528", Title = "The Hunger Games", Author = "Suzanne Collins", Category = "Young Adult", YearPublished = 2008, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780544336261", Title = "The Giver", Author = "Lois Lowry", Category = "Young Adult", YearPublished = 1993, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780062316097", Title = "Sapiens", Author = "Yuval Noah Harari", Category = "History", YearPublished = 2011, TotalCopies = 20, AvailableCopies = 20 },
            new Book { ISBN = "9780143127536", Title = "How Not to Be Wrong", Author = "Jordan Ellenberg", Category = "Mathematics", YearPublished = 2014, TotalCopies = 20, AvailableCopies = 20 },
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

            await SyncSharedBooksAsync(db, sp.GetRequiredService<IWebHostEnvironment>());
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
        private static async Task SyncSharedBooksAsync(ApplicationDbContext db, IWebHostEnvironment env)
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

                // The shared list has no cover for this book, but an older run saved a path to an image file that
                // is not on this machine. Clear it so the catalog shows the generated cover instead of a missing picture.
                if (book != null && string.IsNullOrWhiteSpace(shared.CoverImagePath)
                    && book.CoverImagePath == "/uploads/covers/" + book.ISBN + ".jpg")
                {
                    var file = Path.Combine(env.WebRootPath, "uploads", "covers", book.ISBN + ".jpg");
                    if (!System.IO.File.Exists(file)) { book.CoverImagePath = null; changed = true; }
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