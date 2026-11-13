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

            var range = _db.Transactions
                .Include(x => x.Book)
                .Where(x => x.RequestStatus == "Approved" && x.BorrowDate.Date >= f && x.BorrowDate.Date <= t);

            var rangeList = await range.ToListAsync();

            var totalBorrowed = rangeList.Count;
            var totalReturned = rangeList.Count(x => x.ReturnDate != null);
            var activeLoans = await _db.Transactions.CountAsync(x => x.RequestStatus == "Approved" && x.ReturnDate == null);
            var overdueCount = await _db.Transactions.CountAsync(x => x.RequestStatus == "Approved" && x.ReturnDate == null && x.DueDate < today);
            var pendingRequests = await _db.Transactions.CountAsync(x => x.RequestStatus == "Pending");
            var declinedRequests = await _db.Transactions.CountAsync(x => x.RequestStatus == "Declined");
            var onTimeActive = Math.Max(0, activeLoans - overdueCount);

            var totalDays = (t - f).TotalDays;
            List<ChartPoint> trend;
            if (totalDays <= 31)
            {
                trend = Enumerable.Range(0, (int)totalDays + 1)
                    .Select(i => f.AddDays(i))
                    .Select(d => new ChartPoint
                    {
                        Label = d.ToString("MMM dd"),
                        Value = rangeList.Count(x => x.BorrowDate.Date == d.Date)
                    }).ToList();
            }
            else
            {
                trend = rangeList
                    .GroupBy(x => new DateTime(x.BorrowDate.Year, x.BorrowDate.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartPoint { Label = g.Key.ToString("MMM yyyy"), Value = g.Count() })
                    .ToList();
            }

            var vm = new ReportViewModel
            {
                From = f,
                To = t,
                TotalBorrowed = totalBorrowed,
                TotalReturned = totalReturned,
                ActiveLoans = activeLoans,
                OverdueCount = overdueCount,
                PendingRequests = pendingRequests,
                DeclinedRequests = declinedRequests,
                TotalPenalties = rangeList.Sum(x => x.Penalty),
                TopBooks = rangeList.GroupBy(x => x.Book?.Title ?? "Unknown")
                    .Select(g => new TopBook { Title = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(10).ToList(),
                OverdueLog = await _db.Transactions.Include(x => x.Book).Include(x => x.User)
                    .Where(x => x.RequestStatus == "Approved" && x.ReturnDate == null && x.DueDate < today)
                    .OrderBy(x => x.DueDate).ToListAsync(),
                BorrowTrend = trend,
                StatusBreakdown = new List<ChartPoint>
                {
                    new ChartPoint { Label = "On-time Loans", Value = onTimeActive },
                    new ChartPoint { Label = "Overdue Loans", Value = overdueCount },
                    new ChartPoint { Label = "Returned (Period)", Value = totalReturned },
                    new ChartPoint { Label = "Pending Requests", Value = pendingRequests },
                    new ChartPoint { Label = "Declined Requests", Value = declinedRequests }
                }
            };
            return View(vm);
        }
    }
}