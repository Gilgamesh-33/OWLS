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
        private readonly ApplicationDbContext _db;
        public UsersController(UserManager<ApplicationUser> users, ApplicationDbContext db) { _users = users; _db = db; }

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

            ViewBag.IsAdmin = await _users.IsInRoleAsync(user, DbSeeder.AdminRole);
            ViewBag.Pending = tx.Count(t => t.RequestStatus == "Pending");
            ViewBag.Active = tx.Where(t => t.RequestStatus == "Approved" && t.ReturnDate == null).ToList();
            ViewBag.Overdue = tx.Where(t => t.Status == "Overdue").ToList();
            ViewBag.History = tx;
            return View(user);
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
