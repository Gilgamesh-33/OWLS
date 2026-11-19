using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OWLS.Data;
using OWLS.Models;

namespace OWLS.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public SettingsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        { _db = db; _users = users; }

        public async Task<IActionResult> Index()
        {
            var user = await _users.GetUserAsync(User);
            var isAdmin = User.IsInRole(DbSeeder.AdminRole);

            var vm = new SettingsViewModel
            {
                FullName = user?.FullName ?? "",
                Email = user?.Email ?? "",
                IdNumber = user?.IdNumber ?? "",
                IsAdmin = isAdmin,
                Role = isAdmin ? "Librarian" : "Borrower",
                LoanDays = LibrarySettings.DefaultLoanDays,
                PenaltyPerDay = LibrarySettings.PenaltyPerDay
            };

            if (isAdmin)
            {
                vm.TotalTitles = await _db.Books.CountAsync();
                vm.TotalTransactions = await _db.Transactions.CountAsync();
                vm.TotalBorrowers = (await _users.GetUsersInRoleAsync(DbSeeder.BorrowerRole)).Count;
            }
            return View(vm);
        }
    }
}