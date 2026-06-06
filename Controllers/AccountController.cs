using JobBoardPlatform.Data;
using JobBoardPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JobBoardPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToRoleHome(HttpContext.Session.GetString("UserRole"));

            return View(new User { Role = "JobSeeker" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            user.Email = user.Email.Trim().ToLowerInvariant();
            user.Role = string.IsNullOrWhiteSpace(user.Role) ? "JobSeeker" : user.Role;

            if (user.Role == "Admin")
                ModelState.AddModelError(nameof(JobBoardPlatform.Models.User.Role), "Admin accounts can only be created from the seeded admin or database.");

            if (_context.Users.Any(u => u.Email == user.Email))
                ModelState.AddModelError(nameof(JobBoardPlatform.Models.User.Email), "An account already exists for this email address.");

            if (!ModelState.IsValid)
                return View(user);

            user.PasswordHash = ComputeSha256Hash(user.PasswordHash);
            user.IsActive = user.Role == "JobSeeker";
            user.LastLoginDate = null;

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = user.Role == "Employer"
                ? "Registration successful. Your employer account must be activated by an admin before login."
                : "Registration successful. You can now log in.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToRoleHome(HttpContext.Session.GetString("UserRole"));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var hashedPassword = ComputeSha256Hash(password);

            var user = _context.Users.FirstOrDefault(u => u.Email == normalizedEmail && u.PasswordHash == hashedPassword);
            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = user.Role == "Employer" && user.LastLoginDate == null
                    ? "Your employer account is waiting for admin activation."
                    : "Your account has been deactivated. Please contact the admin.";
                return View();
            }

            user.LastLoginDate = DateTime.UtcNow;
            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName);

            if (user.Role == "Employer")
                HttpContext.Session.SetInt32("EmployerId", user.Id);

            return RedirectToRoleHome(user.Role);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToRoleHome(string? role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Employer" => RedirectToAction("Home", "Employer"),
                "JobSeeker" => RedirectToAction("Home", "JobSeeker"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        internal static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
