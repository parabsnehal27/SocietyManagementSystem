using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class VisitorEntry
    {
        [Key]
        public int EntryId { get; set; }

        [ForeignKey("Visitor")]
        public int VisitorId { get; set; }

        [ForeignKey("Resident")]
        public int ResidentId { get; set; }

        [ForeignKey("SecurityGuard")]
        public int AddedByGuardId { get; set; }

        public int? ApprovedBy { get; set; }

        public string ApprovalStatus { get; set; } = "Pending";

        public DateTime EntryTime { get; set; } = DateTime.Now;

        public DateTime? ExitTime { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Visitor Visitor { get; set; }

        public Resident Resident { get; set; }

        public SecurityGuard SecurityGuard { get; set; }
    }
}