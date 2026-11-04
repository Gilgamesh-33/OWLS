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
            ViewBag.Search = search;
            return View(await q.OrderBy(u => u.FullName).ToListAsync());
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
