using JobBoardPlatform.Data;
using JobBoardPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobBoardPlatform.Controllers
{
    public class JobSeekerController : AuthenticatedController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private static readonly HashSet<string> AllowedResumeExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx" };

        public JobSeekerController(ApplicationDbContext context, IWebHostEnvironment env) : base("JobSeeker")
        {
            _context = context;
            _env = env;
        }

        public IActionResult Home()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.UserName = user.FullName;
            ViewBag.RecentJobs = AvailableJobsQuery()
                .OrderByDescending(j => j.PostedDate)
                .Take(10)
                .ToList();

            return View();
        }

        public IActionResult JobListings(string? searchTitle, string? searchLocation)
        {
            var jobs = AvailableJobsQuery();

            if (!string.IsNullOrWhiteSpace(searchTitle))
                jobs = jobs.Where(j => j.Title.Contains(searchTitle));

            if (!string.IsNullOrWhiteSpace(searchLocation))
                jobs = jobs.Where(j => j.Location.Contains(searchLocation));

            ViewBag.SearchTitle = searchTitle;
            ViewBag.SearchLocation = searchLocation;
            return View(jobs.OrderByDescending(j => j.PostedDate).ToList());
        }

        public IActionResult Apply(int id)
        {
            return RedirectToAction(nameof(ApplyForm), new { jobId = id });
        }

        [HttpGet]
        public IActionResult ApplyForm(int jobId)
        {
            var job = AvailableJobsQuery().FirstOrDefault(j => j.Id == jobId);
            if (job == null) return NotFound();

            ViewBag.JobTitle = job.Title;
            return View(new Application { JobPostId = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyForm(Application application, IFormFile resumeFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var job = AvailableJobsQuery().FirstOrDefault(j => j.Id == application.JobPostId);
            if (job == null) return NotFound();

            if (_context.Applications.Any(a => a.JobPostId == application.JobPostId && a.UserId == userId))
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction(nameof(JobListings));
            }

            if (resumeFile == null || resumeFile.Length == 0)
                ModelState.AddModelError("resumeFile", "Please upload your resume.");
            else if (!AllowedResumeExtensions.Contains(Path.GetExtension(resumeFile.FileName)))
                ModelState.AddModelError("resumeFile", "Resume must be a PDF, DOC, or DOCX file.");

            if (!ModelState.IsValid)
            {
                ViewBag.JobTitle = job.Title;
                return View(application);
            }

            var uploadsDirectory = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDirectory);

            var extension = Path.GetExtension(resumeFile.FileName);
            var fileName = $"resume_{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadsDirectory, fileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
                resumeFile.CopyTo(stream);

            application.UserId = userId;
            application.AppliedDate = DateTime.UtcNow;
            application.ResumeFilePath = "/uploads/" + fileName;
            application.Status = "Pending";

            _context.Applications.Add(application);
            _context.SaveChanges();
            TempData["Success"] = "Your application has been submitted.";
            return RedirectToAction(nameof(MyApplications));
        }

        public IActionResult MyApplications()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var applications = _context.Applications
                .Include(a => a.JobPost)
                .ThenInclude(j => j.Employer)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .ToList();

            return View(applications);
        }

        private IQueryable<JobPost> AvailableJobsQuery()
        {
            var now = DateTime.UtcNow;
            return _context.JobPosts
                .Include(j => j.Employer)
                .Where(j => j.IsActive && j.Status == "Approved" && (j.ExpiryDate == null || j.ExpiryDate >= now));
        }
    }
}
