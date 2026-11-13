using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OWLS.Data;
using OWLS.Models;

namespace OWLS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(UserManager<ApplicationUser> u, SignInManager<ApplicationUser> s)
        { _users = u; _signIn = s; }

        // Already signed in? Send them home instead of showing the login page.
        // NoStore stops the browser's Back button from showing a cached login page.
        [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Books");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel m, string returnUrl = null)
        {
            if (!ModelState.IsValid) return View(m);
            var result = await _signIn.PasswordSignInAsync(m.Email, m.Password, m.RememberMe, false);
            if (result.Succeeded)
            {
                if (Url.IsLocalUrl(returnUrl) && returnUrl != "/" && !returnUrl.StartsWith("/Home", StringComparison.OrdinalIgnoreCase))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Books");
            }
            ModelState.AddModelError("", "Invalid email or password.");
            return View(m);
        }

        [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Books");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel m)
        {
            if (!ModelState.IsValid) return View(m);
            var user = new ApplicationUser { UserName = m.Email, Email = m.Email, FullName = m.FullName, IdNumber = m.IdNumber, EmailConfirmed = true };
            var result = await _users.CreateAsync(user, m.Password);
            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(user, DbSeeder.BorrowerRole);
                await _signIn.SignInAsync(user, false);
                return RedirectToAction("Index", "Books");
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() { await _signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }

        public IActionResult AccessDenied() => View();
    }
}