using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OWLS.Data;
using OWLS.Models;

namespace OWLS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly ApplicationDbContext _db;
        public UsersController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ApplicationDbContext db)
        { _users = users; _signIn = signIn; _db = db; }

        public async Task<IActionResult> Index(string search)
        {
            var q = _users.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(u => u.FullName.Contains(search) || u.Email.Contains(search) || u.IdNumber.Contains(search));
            var users = await q.OrderBy(u => u.FullName).ToListAsync();

            var stats = await _db.Transactions
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Pending = g.Count(t => t.RequestStatus == "Pending"),
                    Active = g.Count(t => t.RequestStatus == "Approved" && t.ReturnDate == null),
                    Overdue = g.Count(t => t.RequestStatus == "Approved" && t.ReturnDate == null && t.DueDate < DateTime.Today),
                    Total = g.Count()
                }).ToListAsync();

            ViewBag.Search = search;
            ViewBag.UserStats = stats.ToDictionary(x => x.UserId, x => x);
            ViewBag.AdminIds = (await _users.GetUsersInRoleAsync(DbSeeder.AdminRole)).Select(u => u.Id).ToHashSet();
            return View(users);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var tx = await _db.Transactions.Include(t => t.Book)
                .Where(t => t.UserId == id)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            var pending = tx.Where(t => t.RequestStatus == "Pending").ToList();
            var overdue = tx.Where(t => t.Status == "Overdue").ToList();
            // "Borrowed" is the computed status for approved, unreturned loans that are not yet overdue.
            var active = tx.Where(t => t.Status == "Borrowed").ToList();
            var past = tx.Where(t => t.ReturnDate != null || t.RequestStatus == "Declined")
                .OrderByDescending(t => t.ReturnDate ?? t.BorrowDate).ThenByDescending(t => t.Id).ToList();

            var vm = new BorrowerDetailsViewModel
            {
                User = user,
                IsAdmin = await _users.IsInRoleAsync(user, DbSeeder.AdminRole),
                PendingCount = pending.Count,
                ActiveCount = active.Count,
                OverdueCount = overdue.Count,
                TotalRecords = tx.Count,
                ActiveLoans = active.OrderBy(t => t.DueDate).ToList(),
                Attention = pending.Concat(overdue)
                    .OrderByDescending(t => t.Status == "Overdue").ThenByDescending(t => t.Id).ToList(),
                PastTransactions = past
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(new EditBorrowerViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                IdNumber = user.IdNumber,
                Email = user.Email,
                IsAdmin = await _users.IsInRoleAsync(user, DbSeeder.AdminRole)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditBorrowerViewModel m)
        {
            var user = await _users.FindByIdAsync(m.Id);
            if (user == null) return NotFound();

            m.IsAdmin = await _users.IsInRoleAsync(user, DbSeeder.AdminRole);

            // The seeder looks the admin up by email at startup, so the admin email stays locked.
            if (m.IsAdmin) m.Email = user.Email;

            if (!ModelState.IsValid) return View(m);

            var fullName = m.FullName.Trim();
            var idNumber = m.IdNumber.Trim();
            var email = m.Email.Trim();

            if (!string.Equals(user.IdNumber, idNumber, StringComparison.OrdinalIgnoreCase)
                && await _users.Users.AnyAsync(u => u.Id != user.Id && u.IdNumber == idNumber))
            {
                ModelState.AddModelError(nameof(m.IdNumber), "Another account already uses this School ID.");
                return View(m);
            }

            var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);

            user.FullName = fullName;
            user.IdNumber = idNumber;
            if (emailChanged)
            {
                user.Email = email;
                user.UserName = email;   // login uses the email as the username
            }

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(m);
            }

            if (emailChanged) await _users.UpdateSecurityStampAsync(user);

            // If an admin edited their own account, re-issue their cookie so the name in the navbar updates.
            if (user.Id == _users.GetUserId(User)) await _signIn.RefreshSignInAsync(user);

            TempData["Success"] = "Details updated for " + user.FullName + ".";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (await _users.IsInRoleAsync(user, DbSeeder.AdminRole)) { TempData["Error"] = "Admin accounts cannot be deleted here."; return RedirectToAction(nameof(Index)); }
            if (await _db.Transactions.AnyAsync(t => t.UserId == id)) { TempData["Error"] = "Cannot delete: user has transaction history."; return RedirectToAction(nameof(Index)); }
            await _users.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}