using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class Complaint
    {
        [Key]
        public int ComplaintId { get; set; }

        [ForeignKey("Resident")]
        public int ResidentId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }

        public string? Priority { get; set; }

        public string Status { get; set; } = "Pending";

        public int? AssignedTo { get; set; }

        public string? ResolutionRemarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public Resident Resident { get; set; }
    }
}