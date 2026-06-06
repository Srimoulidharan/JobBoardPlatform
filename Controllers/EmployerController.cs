using JobBoardPlatform.Data;
using JobBoardPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobBoardPlatform.Controllers
{
    public class EmployerController : AuthenticatedController
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] ApplicationStatuses = { "Pending", "Shortlisted", "Hired", "Rejected" };

        public EmployerController(ApplicationDbContext context) : base("Employer")
        {
            _context = context;
        }

        public IActionResult Home()
        {
            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var jobs = _context.JobPosts
                .Where(j => j.EmployerId == employerId)
                .OrderByDescending(j => j.PostedDate)
                .ToList();

            return View(jobs);
        }

        [HttpGet]
        public IActionResult CreateJob()
        {
            return View(new JobPost { ExpiryDate = DateTime.UtcNow.AddDays(30) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateJob(JobPost job)
        {
            if (!ModelState.IsValid)
                return View(job);

            job.PostedDate = DateTime.UtcNow;
            job.EmployerId = HttpContext.Session.GetInt32("UserId")!.Value;
            job.Status = "Pending";
            job.IsActive = true;

            _context.JobPosts.Add(job);
            _context.SaveChanges();
            TempData["Success"] = "Job submitted. It will appear to job seekers after admin approval.";
            return RedirectToAction(nameof(Home));
        }

        [HttpGet]
        public IActionResult EditJob(int id)
        {
            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var job = _context.JobPosts.FirstOrDefault(j => j.Id == id && j.EmployerId == employerId);
            if (job == null) return NotFound();

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditJob(JobPost job)
        {
            if (!ModelState.IsValid)
                return View(job);

            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var existingJob = _context.JobPosts.FirstOrDefault(j => j.Id == job.Id && j.EmployerId == employerId);
            if (existingJob == null) return NotFound();

            existingJob.Title = job.Title;
            existingJob.Description = job.Description;
            existingJob.Location = job.Location;
            existingJob.ExpiryDate = job.ExpiryDate;
            existingJob.Status = "Pending";

            _context.SaveChanges();
            TempData["Success"] = "Job updated and sent for admin review.";
            return RedirectToAction(nameof(Home));
        }

        [HttpGet]
        public IActionResult DeleteJob(int id)
        {
            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var job = _context.JobPosts.FirstOrDefault(j => j.Id == id && j.EmployerId == employerId);
            if (job == null) return NotFound();

            return View(job);
        }

        [HttpPost, ActionName("DeleteJob")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteJobConfirmed(int id)
        {
            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var job = _context.JobPosts.FirstOrDefault(j => j.Id == id && j.EmployerId == employerId);
            if (job == null) return NotFound();

            _context.JobPosts.Remove(job);
            _context.SaveChanges();
            TempData["Success"] = "Job deleted.";
            return RedirectToAction(nameof(Home));
        }

        [HttpGet]
        public IActionResult ViewApplicants(int id)
        {
            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var job = _context.JobPosts
                .Include(j => j.Applications)
                .ThenInclude(a => a.User)
                .FirstOrDefault(j => j.Id == id && j.EmployerId == employerId);

            if (job == null) return NotFound();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateApplicationStatus(int applicationId, string status)
        {
            if (!ApplicationStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid application status.";
                return RedirectToAction(nameof(Home));
            }

            var employerId = HttpContext.Session.GetInt32("UserId")!.Value;
            var application = _context.Applications
                .Include(a => a.JobPost)
                .FirstOrDefault(a => a.Id == applicationId && a.JobPost.EmployerId == employerId);

            if (application == null) return NotFound();

            application.Status = status;
            _context.SaveChanges();
            TempData["Success"] = "Application status updated.";
            return RedirectToAction(nameof(ViewApplicants), new { id = application.JobPostId });
        }
    }
}
