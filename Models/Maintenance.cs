using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class Maintenance
    {
        [Key]
        public int MaintenanceId { get; set; }

        [ForeignKey("Resident")]
        public int ResidentId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public string PaymentStatus { get; set; } = "Pending";

        public DateTime? PaidDate { get; set; }

        public string? PaymentMethod { get; set; }

        public string? TransactionReference { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Resident Resident { get; set; }
    }
}