using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JobBoardPlatform.Models
{
    public class Application
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [ValidateNever]
        public User User { get; set; } = null!;

        public int JobPostId { get; set; }

        [ValidateNever]
        public JobPost JobPost { get; set; } = null!;

        public string? Message { get; set; }
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public string? ResumeFilePath { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
