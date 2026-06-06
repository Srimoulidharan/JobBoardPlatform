using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JobBoardPlatform.Models
{
    public class JobPost
    {
        public int Id { get; set; }

        [Required, StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string Location { get; set; } = string.Empty;

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }

        [ForeignKey(nameof(Employer))]
        public int EmployerId { get; set; }

        [ValidateNever]
        public User Employer { get; set; } = null!;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public string Status { get; set; } = "Pending";
        public bool IsActive { get; set; } = true;
    }
}
