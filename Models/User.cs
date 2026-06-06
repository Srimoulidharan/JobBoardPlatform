using System.ComponentModel.DataAnnotations;

namespace JobBoardPlatform.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(160)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "JobSeeker";

        public string? ProfilePicture { get; set; }
        public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }
    }
}
