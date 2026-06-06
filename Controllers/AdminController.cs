using JobBoardPlatform.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobBoardPlatform.Controllers
{
    public class AdminController : AuthenticatedController
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] ValidJobStatuses = { "Pending", "Approved", "Rejected" };
        private static readonly string[] ValidRoles = { "Admin", "Employer", "JobSeeker" };

        public AdminController(ApplicationDbContext context) : base("Admin")
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalJobs = _context.JobPosts.Count();
            ViewBag.TotalApplications = _context.Applications.Count();
            ViewBag.Users = _context.Users.OrderByDescending(u => u.Id).Take(10).ToList();
            ViewBag.Jobs = _context.JobPosts.Include(j => j.Employer).OrderByDescending(j => j.PostedDate).Take(10).ToList();
            return View();
        }

        public IActionResult ManageUsers(string? role)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            if (currentUserId.HasValue)
                query = query.Where(u => u.Id != currentUserId.Value);

            ViewBag.Roles = ValidRoles.ToList();
            ViewBag.SelectedRole = role;
            ViewBag.CurrentUserId = currentUserId;

            return View(query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();
            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateUserRole(int userId, string newRole)
        {
            if (!ValidRoles.Contains(newRole))
            {
                TempData["Error"] = "Invalid role selected.";
                return RedirectToAction(nameof(ManageUsers));
            }

            if (HttpContext.Session.GetInt32("UserId") == userId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            user.Role = newRole;
            user.IsActive = newRole != "Employer" || user.IsActive;
            _context.SaveChanges();

            TempData["Success"] = "User role updated successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserStatus(int id, bool makeActive)
        {
            if (HttpContext.Session.GetInt32("UserId") == id)
            {
                TempData["Error"] = "You cannot change your own account status.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.IsActive = makeActive;
            _context.SaveChanges();
            TempData["Success"] = "User status updated successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpGet]
        public IActionResult ViewAllJobs(string? status)
        {
            var jobs = _context.JobPosts
                .Include(j => j.Employer)
                .Where(j => string.IsNullOrEmpty(status) || j.Status == status)
                .OrderByDescending(j => j.PostedDate)
                .ToList();

            ViewBag.SelectedStatus = status;
            ViewBag.StatusOptions = ValidJobStatuses.ToList();
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateJobStatus(int id, string status)
        {
            if (!ValidJobStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid job status.";
                return RedirectToAction(nameof(ViewAllJobs));
            }

            var job = _context.JobPosts.Find(id);
            if (job == null) return NotFound();

            job.Status = status;
            job.IsActive = status == "Approved" && job.IsActive;
            _context.SaveChanges();
            TempData["Success"] = "Job status updated.";
            return RedirectToAction(nameof(ViewAllJobs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleJobActive(int jobId)
        {
            var job = _context.JobPosts.Find(jobId);
            if (job == null) return NotFound();

            job.IsActive = !job.IsActive;
            _context.SaveChanges();
            TempData["Success"] = "Job active status updated.";
            return RedirectToAction(nameof(ViewAllJobs));
        }
    }
}
