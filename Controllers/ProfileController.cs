using JobBoardPlatform.Data;
using JobBoardPlatform.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobBoardPlatform.Controllers
{
    public class ProfileController : AuthenticatedController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment env) : base()
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User updatedUser, IFormFile? profileImage)
        {
            ModelState.Remove(nameof(JobBoardPlatform.Models.User.PasswordHash));
            ModelState.Remove(nameof(JobBoardPlatform.Models.User.Role));
            ModelState.Remove(nameof(JobBoardPlatform.Models.User.Applications));
            ModelState.Remove(nameof(JobBoardPlatform.Models.User.JobPosts));

            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(updatedUser.FullName))
                ModelState.AddModelError(nameof(JobBoardPlatform.Models.User.FullName), "Full name is required.");

            if (profileImage != null && profileImage.Length > 0)
            {
                var extension = Path.GetExtension(profileImage.FileName);
                if (!AllowedImageExtensions.Contains(extension))
                    ModelState.AddModelError("profileImage", "Profile picture must be JPG, PNG, GIF, or WEBP.");
            }

            if (!ModelState.IsValid)
                return View(user);

            user.FullName = updatedUser.FullName.Trim();

            if (profileImage != null && profileImage.Length > 0)
            {
                var directory = Path.Combine(_env.WebRootPath, "uploads", "profile_pics");
                Directory.CreateDirectory(directory);

                var fileName = $"user_{user.Id}_{Guid.NewGuid():N}{Path.GetExtension(profileImage.FileName)}";
                var path = Path.Combine(directory, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                    profileImage.CopyTo(stream);

                user.ProfilePicture = "/uploads/profile_pics/" + fileName;
            }

            _context.SaveChanges();
            HttpContext.Session.SetString("UserName", user.FullName);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Edit));
        }
    }
}
