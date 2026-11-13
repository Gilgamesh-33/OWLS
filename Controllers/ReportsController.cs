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

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, string range = "monthly")
        {
            var today = DateTime.Today;
            DateTime f;
            DateTime t;

            switch ((range ?? "custom").ToLowerInvariant())
            {
                case "daily":
                    f = today; t = today; break;
                case "weekly":
                    int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    f = today.AddDays(-diff); t = today; break;
                case "monthly":
                    f = new DateTime(today.Year, today.Month, 1); t = today; break;
                case "yearly":
                    f = new DateTime(today.Year, 1, 1); t = today; break;
                default:
                    f = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
                    t = (to ?? today).Date;
                    range = "custom"; break;
            }
            if (t < f) (f, t) = (t, f);

            var approvedInRange = await _db.Transactions
                .Include(x => x.Book)
                .Where(x => x.RequestStatus == "Approved" && x.BorrowDate.Date >= f && x.BorrowDate.Date <= t)
                .ToListAsync();

            var returnsInRange = await _db.Transactions
                .Where(x => x.RequestStatus == "Approved" && x.ReturnDate != null && x.ReturnDate.Value.Date >= f && x.ReturnDate.Value.Date <= t)
                .ToListAsync();

            var totalBorrowed = approvedInRange.Count;
            var totalReturned = returnsInRange.Count;
            var activeLoans = await _db.Transactions.CountAsync(x => x.RequestStatus == "Approved" && x.ReturnDate == null);
            var overdueCount = await _db.Transactions.CountAsync(x => x.RequestStatus == "Approved" && x.ReturnDate == null && x.DueDate < today);
            var pendingRequests = await _db.Transactions.CountAsync(x => x.RequestStatus == "Pending");
            var declinedRequests = await _db.Transactions.CountAsync(x => x.RequestStatus == "Declined");
            var approvedCount = await _db.Transactions.CountAsync(x => x.RequestStatus == "Approved");
            var returnedAll = await _db.Transactions.CountAsync(x => x.RequestStatus == "Approved" && x.ReturnDate != null);
            var onTimeActive = Math.Max(0, activeLoans - overdueCount);

            var totalDays = (t - f).TotalDays;
            List<ChartPoint> trend;
            if (totalDays <= 31)
            {
                trend = Enumerable.Range(0, (int)totalDays + 1)
                    .Select(i => f.AddDays(i))
                    .Select(d => new ChartPoint { Label = d.ToString("MMM dd"), Value = approvedInRange.Count(x => x.BorrowDate.Date == d.Date) })
                    .ToList();
            }
            else if (totalDays <= 366)
            {
                trend = approvedInRange
                    .GroupBy(x => new DateTime(x.BorrowDate.Year, x.BorrowDate.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartPoint { Label = g.Key.ToString("MMM yyyy"), Value = g.Count() })
                    .ToList();
                var cursor = new DateTime(f.Year, f.Month, 1);
                var end = new DateTime(t.Year, t.Month, 1);
                var map = trend.ToDictionary(x => x.Label, x => x.Value);
                var filled = new List<ChartPoint>();
                while (cursor <= end)
                {
                    var label = cursor.ToString("MMM yyyy");
                    filled.Add(new ChartPoint { Label = label, Value = map.TryGetValue(label, out var v) ? v : 0 });
                    cursor = cursor.AddMonths(1);
                }
                trend = filled;
            }
            else
            {
                trend = approvedInRange
                    .GroupBy(x => x.BorrowDate.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartPoint { Label = g.Key.ToString(), Value = g.Count() })
                    .ToList();
            }

            // Monthly returns across the selected period (by month buckets)
            var monthlyReturns = new List<ChartPoint>();
            {
                var cursor = new DateTime(f.Year, f.Month, 1);
                var end = new DateTime(t.Year, t.Month, 1);
                while (cursor <= end)
                {
                    var month = cursor;
                    var next = month.AddMonths(1);
                    monthlyReturns.Add(new ChartPoint
                    {
                        Label = month.ToString("MMM yyyy"),
                        Value = returnsInRange.Count(x => x.ReturnDate.HasValue && x.ReturnDate.Value.Date >= month && x.ReturnDate.Value.Date < next)
                    });
                    cursor = next;
                }
                if (monthlyReturns.Count == 1 && totalDays <= 31)
                {
                    monthlyReturns = Enumerable.Range(0, (int)totalDays + 1)
                        .Select(i => f.AddDays(i))
                        .Select(d => new ChartPoint { Label = d.ToString("MMM dd"), Value = returnsInRange.Count(x => x.ReturnDate.HasValue && x.ReturnDate.Value.Date == d.Date) })
                        .ToList();
                }
            }

            var books = await _db.Books.AsNoTracking().ToListAsync();
            var inventoryByCategory = books
                .GroupBy(b => b.Category ?? "Uncategorized")
                .Select(g => new CategoryPoint
                {
                    Category = g.Key,
                    TotalCopies = g.Sum(b => b.TotalCopies),
                    AvailableCopies = g.Sum(b => b.AvailableCopies),
                    BorrowedCopies = Math.Max(0, g.Sum(b => b.TotalCopies - b.AvailableCopies))
                })
                .OrderByDescending(x => x.TotalCopies).ToList();

            var totalCopies = books.Sum(b => b.TotalCopies);
            var availableCopies = books.Sum(b => b.AvailableCopies);
            var borrowedCopiesNow = Math.Max(0, totalCopies - availableCopies);

            var vm = new ReportViewModel
            {
                From = f,
                To = t,
                Range = range.ToLowerInvariant(),
                TotalBorrowed = totalBorrowed,
                TotalReturned = totalReturned,
                ActiveLoans = activeLoans,
                OverdueCount = overdueCount,
                ApprovedCount = approvedCount,
                ReturnedAllTime = returnedAll,
                PendingRequests = pendingRequests,
                DeclinedRequests = declinedRequests,
                TotalPenalties = returnsInRange.Sum(x => x.Penalty),
                TotalTitles = books.Count,
                TotalCopies = totalCopies,
                AvailableCopies = availableCopies,
                BorrowedCopiesNow = borrowedCopiesNow,
                TopBooks = approvedInRange.GroupBy(x => x.Book?.Title ?? "Unknown")
                    .Select(g => new TopBook { Title = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(10).ToList(),
                OverdueLog = await _db.Transactions.Include(x => x.Book).Include(x => x.User)
                    .Where(x => x.RequestStatus == "Approved" && x.ReturnDate == null && x.DueDate < today)
                    .OrderBy(x => x.DueDate).ToListAsync(),
                BorrowTrend = trend,
                MonthlyReturns = monthlyReturns,
                InventoryByCategory = inventoryByCategory,
                AvailabilityBreakdown = new List<ChartPoint>
                {
                    new ChartPoint { Label = "Available", Value = availableCopies },
                    new ChartPoint { Label = "Borrowed", Value = borrowedCopiesNow }
                },
                StatusBreakdown = new List<ChartPoint>
                {
                    new ChartPoint { Label = "Pending", Value = pendingRequests },
                    new ChartPoint { Label = "Approved (Active)", Value = onTimeActive },
                    new ChartPoint { Label = "Returned", Value = returnedAll },
                    new ChartPoint { Label = "Overdue", Value = overdueCount },
                    new ChartPoint { Label = "Declined", Value = declinedRequests }
                }
            };
            return View(vm);
        }
    }
}