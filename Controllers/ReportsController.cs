using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OWLS.Data;
using OWLS.Models;

namespace OWLS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ReportsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var f = (from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var t = (to ?? DateTime.Today).Date;
            var today = DateTime.Today;

            var range = _db.Transactions.Where(x => x.BorrowDate >= f && x.BorrowDate <= t);

            var vm = new ReportViewModel
            {
                From = f, To = t,
                TotalBorrowed = await range.CountAsync(),
                TotalReturned = await range.CountAsync(x => x.ReturnDate != null),
                ActiveLoans = await _db.Transactions.CountAsync(x => x.ReturnDate == null),
                OverdueCount = await _db.Transactions.CountAsync(x => x.ReturnDate == null && x.DueDate < today),
                TotalPenalties = await range.SumAsync(x => (decimal?)x.Penalty) ?? 0m,
                TopBooks = await range.GroupBy(x => x.Book.Title)
                    .Select(g => new TopBook { Title = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(10).ToListAsync(),
                OverdueLog = await _db.Transactions.Include(x => x.Book).Include(x => x.User)
                    .Where(x => x.ReturnDate == null && x.DueDate < today)
                    .OrderBy(x => x.DueDate).ToListAsync()
            };
            return View(vm);
        }
    }
}
