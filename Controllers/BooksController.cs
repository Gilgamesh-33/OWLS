using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OWLS.Data;
using OWLS.Models;

namespace OWLS.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _users;
        public BooksController(ApplicationDbContext db, IWebHostEnvironment env, UserManager<ApplicationUser> users) { _db = db; _env = env; _users = users; }

        private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };

        private void ValidateCover(IFormFile file)
        {
            if (file == null || file.Length == 0) return;
            if (!AllowedExt.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
                ModelState.AddModelError("", "Cover must be a .jpg, .png or .webp image.");
            else if (file.Length > 2 * 1024 * 1024)
                ModelState.AddModelError("", "Cover image must be 2 MB or smaller.");
        }

        private async Task<string> SaveCoverAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "covers");
            Directory.CreateDirectory(folder);
            var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
            using var fs = new FileStream(Path.Combine(folder, name), FileMode.Create);
            await file.CopyToAsync(fs);
            return "/uploads/covers/" + name;
        }

        private void DeleteCover(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var full = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }

        // READ + SEARCH + FILTER + SORT
        public async Task<IActionResult> Index(string search, string category, string sort, bool availableOnly = false)
        {
            var q = _db.Books.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(b => b.Title.Contains(search) || b.Author.Contains(search) || b.ISBN.Contains(search) || b.Category.Contains(search));
            if (!string.IsNullOrWhiteSpace(category)) q = q.Where(b => b.Category == category);
            if (availableOnly) q = q.Where(b => b.AvailableCopies > 0);

            q = sort switch
            {
                "title_desc" => q.OrderByDescending(b => b.Title),
                "author" => q.OrderBy(b => b.Author),
                "year" => q.OrderByDescending(b => b.YearPublished),
                _ => q.OrderBy(b => b.Title)
            };

            ViewBag.Categories = await _db.Books.Select(b => b.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Search = search; ViewBag.Category = category; ViewBag.Sort = sort; ViewBag.AvailableOnly = availableOnly;
            if (!User.IsInRole("Admin"))
            {
                var uid = _users.GetUserId(User);
                ViewBag.MyPendingBookIds = await _db.Transactions.Where(t => t.UserId == uid && t.RequestStatus == "Pending").Select(t => t.BookId).ToListAsync();
                ViewBag.MyBorrowedBookIds = await _db.Transactions.Where(t => t.UserId == uid && t.RequestStatus == "Approved" && t.ReturnDate == null).Select(t => t.BookId).ToListAsync();
            }
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var book = await _db.Books.FindAsync(id);
            return book == null ? NotFound() : View(book);
        }

        // CREATE
        [Authorize(Roles = "Admin")] public IActionResult Create() => View(new Book { TotalCopies = 1, YearPublished = DateTime.Now.Year });

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Book book, IFormFile coverFile)
        {
            if (await _db.Books.AnyAsync(b => b.ISBN == book.ISBN))
                ModelState.AddModelError("ISBN", "A book with this ISBN already exists.");
            ValidateCover(coverFile);
            if (!ModelState.IsValid) return View(book);

            book.AvailableCopies = book.TotalCopies;
            book.CoverImagePath = coverFile is { Length: > 0 } ? await SaveCoverAsync(coverFile) : null;
            _db.Add(book);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Book added.";
            return RedirectToAction(nameof(Index));
        }

        // UPDATE
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var book = await _db.Books.FindAsync(id);
            return book == null ? NotFound() : View(book);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Book book, IFormFile coverFile, bool removeCover)
        {
            if (id != book.Id) return NotFound();
            var existing = await _db.Books.FindAsync(id);
            if (existing == null) return NotFound();
            book.CoverImagePath = existing.CoverImagePath;   // keeps the preview if validation fails

            if (await _db.Books.AnyAsync(b => b.ISBN == book.ISBN && b.Id != id))
                ModelState.AddModelError("ISBN", "A book with this ISBN already exists.");
            ValidateCover(coverFile);
            if (!ModelState.IsValid) return View(book);

            int out_ = existing.TotalCopies - existing.AvailableCopies;
            if (book.TotalCopies < out_)
            {
                ModelState.AddModelError("TotalCopies", $"{out_} copies are currently borrowed; total cannot be lower.");
                return View(book);
            }

            existing.ISBN = book.ISBN; existing.Title = book.Title; existing.Author = book.Author;
            existing.Category = book.Category; existing.YearPublished = book.YearPublished;
            existing.TotalCopies = book.TotalCopies; existing.AvailableCopies = book.TotalCopies - out_;

            if (coverFile is { Length: > 0 })
            {
                DeleteCover(existing.CoverImagePath);
                existing.CoverImagePath = await SaveCoverAsync(coverFile);
            }
            else if (removeCover)
            {
                DeleteCover(existing.CoverImagePath);
                existing.CoverImagePath = null;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Book updated.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var book = await _db.Books.FindAsync(id);
            return book == null ? NotFound() : View(book);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (await _db.Transactions.AnyAsync(t => t.BookId == id))
            {
                TempData["Error"] = "Cannot delete: this book has transaction history.";
                return RedirectToAction(nameof(Index));
            }
            var book = await _db.Books.FindAsync(id);
            DeleteCover(book.CoverImagePath);
            if (book != null) { _db.Books.Remove(book); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Book deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}