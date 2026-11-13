using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OWLS.Data;
using OWLS.Models;

namespace OWLS.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        public TransactionsController(ApplicationDbContext db, UserManager<ApplicationUser> users) { _db = db; _users = users; }

        // ADMIN: list all, search, filter by status
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search, string status)
        {
            var q = _db.Transactions.Include(t => t.Book).Include(t => t.User).AsQueryable();
            var today = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(t => t.Book.Title.Contains(search) || t.User.FullName.Contains(search) || t.User.IdNumber.Contains(search));
            q = status switch
            {
                "Pending" => q.Where(t => t.RequestStatus == "Pending"),
                "Borrowed" => q.Where(t => t.RequestStatus == "Approved" && t.ReturnDate == null && t.DueDate >= today),
                "Overdue" => q.Where(t => t.RequestStatus == "Approved" && t.ReturnDate == null && t.DueDate < today),
                "Returned" => q.Where(t => t.RequestStatus == "Approved" && t.ReturnDate != null),
                "Declined" => q.Where(t => t.RequestStatus == "Declined"),
                _ => q
            };
            ViewBag.Search = search; ViewBag.Status = status;
            ViewBag.PendingCount = await _db.Transactions.CountAsync(t => t.RequestStatus == "Pending");
            return View(await q.OrderByDescending(t => t.RequestStatus == "Pending").ThenByDescending(t => t.Id).ToListAsync());
        }

        // BORROWER: own history
        public async Task<IActionResult> My()
        {
            var uid = _users.GetUserId(User);
            var list = await _db.Transactions.Include(t => t.Book)
                .Where(t => t.UserId == uid).OrderByDescending(t => t.BorrowDate).ToListAsync();
            return View(list);
        }

        // BORROWER: request to borrow from catalog
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBorrow(int bookId)
        {
            if (User.IsInRole("Admin")) return Forbid();
            var uid = _users.GetUserId(User);
            var book = await _db.Books.FindAsync(bookId);
            if (book == null) return NotFound();
            if (book.AvailableCopies < 1)
            {
                TempData["Error"] = "No copies available for this book.";
                return RedirectToAction("Index", "Books");
            }
            bool duplicate = await _db.Transactions.AnyAsync(t => t.BookId == bookId && t.UserId == uid
                && (t.RequestStatus == "Pending" || (t.RequestStatus == "Approved" && t.ReturnDate == null)));
            if (duplicate)
            {
                TempData["Error"] = "You already have a pending or active borrow for this book.";
                return RedirectToAction("Index", "Books");
            }

            _db.Transactions.Add(new BorrowTransaction
            {
                BookId = bookId,
                UserId = uid,
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today,
                RequestStatus = "Pending"
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Borrow request submitted and marked as Pending.";
            return RedirectToAction("Index", "Books");
        }

        // CREATE (checkout)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? bookId)
        {
            await LoadDropdowns(bookId);
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int bookId, string userId, int days = LibrarySettings.DefaultLoanDays)
        {
            var book = await _db.Books.FindAsync(bookId);
            if (book == null || string.IsNullOrEmpty(userId)) ModelState.AddModelError("", "Select a book and a borrower.");
            else if (book.AvailableCopies < 1) ModelState.AddModelError("", "No copies available for this book.");
            if (days < 1 || days > 60) ModelState.AddModelError("", "Loan period must be 1-60 days.");

            if (!ModelState.IsValid) { await LoadDropdowns(bookId); return View(); }

            _db.Transactions.Add(new BorrowTransaction
            {
                BookId = bookId,
                UserId = userId,
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(days),
                RequestStatus = "Approved"
            });
            book.AvailableCopies--;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Book checked out.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, int days = LibrarySettings.DefaultLoanDays)
        {
            var t = await _db.Transactions.Include(x => x.Book).FirstOrDefaultAsync(x => x.Id == id);
            if (t == null || t.RequestStatus != "Pending") return NotFound();
            if (t.Book.AvailableCopies < 1)
            {
                TempData["Error"] = "Cannot approve: no copies available.";
                return RedirectToAction(nameof(Index));
            }
            if (days < 1 || days > 60) days = LibrarySettings.DefaultLoanDays;
            t.RequestStatus = "Approved";
            t.BorrowDate = DateTime.Today;
            t.DueDate = DateTime.Today.AddDays(days);
            t.Book.AvailableCopies--;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Borrow request accepted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Decline(int id)
        {
            var t = await _db.Transactions.FindAsync(id);
            if (t == null || t.RequestStatus != "Pending") return NotFound();
            t.RequestStatus = "Declined";
            await _db.SaveChangesAsync();
            TempData["Success"] = "Borrow request declined.";
            return RedirectToAction(nameof(Index));
        }

        // RETURN (computes penalty)
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Return(int id)
        {
            var t = await _db.Transactions.Include(x => x.Book).FirstOrDefaultAsync(x => x.Id == id);
            if (t == null || t.ReturnDate != null || t.RequestStatus != "Approved") return NotFound();
            t.ReturnDate = DateTime.Today;
            int late = Math.Max(0, (t.ReturnDate.Value - t.DueDate.Date).Days);
            t.Penalty = late * LibrarySettings.PenaltyPerDay;
            t.Book.AvailableCopies++;
            await _db.SaveChangesAsync();
            TempData["Success"] = t.Penalty > 0 ? $"Returned. Overdue penalty: ₱{t.Penalty:N2}" : "Returned on time.";
            return RedirectToAction(nameof(Index));
        }

        // UPDATE (adjust due date)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var t = await _db.Transactions.Include(x => x.Book).Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
            return t == null ? NotFound() : View(t);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, DateTime dueDate)
        {
            var t = await _db.Transactions.FindAsync(id);
            if (t == null || t.RequestStatus != "Approved") return NotFound();
            if (dueDate.Date < t.BorrowDate.Date)
            {
                TempData["Error"] = "Due date cannot be before the borrow date.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            t.DueDate = dueDate.Date;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Transaction updated.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Transactions.Include(x => x.Book).FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            if (t.RequestStatus == "Approved" && t.ReturnDate == null) t.Book.AvailableCopies++;   // restore copy if active loan
            _db.Transactions.Remove(t);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Transaction deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(int? bookId)
        {
            var books = await _db.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToListAsync();
            var borrowers = (await _users.GetUsersInRoleAsync(DbSeeder.BorrowerRole)).OrderBy(u => u.FullName);
            ViewBag.Books = new SelectList(books.Select(b => new { b.Id, Label = $"{b.Title} ({b.AvailableCopies} avail.)" }), "Id", "Label", bookId);
            ViewBag.Users = new SelectList(borrowers.Select(u => new { u.Id, Label = $"{u.FullName} - {u.IdNumber}" }), "Id", "Label");
        }
    }
}