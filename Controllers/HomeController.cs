using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OWLS.Data;

namespace OWLS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Books");

            ViewBag.Titles = await _db.Books.CountAsync();
            ViewBag.Available = await _db.Books.SumAsync(b => (int?)b.AvailableCopies) ?? 0;
            ViewBag.Active = await _db.Transactions.CountAsync(t => t.ReturnDate == null);
            return View();
        }

        public IActionResult Error() => View();
    }
}