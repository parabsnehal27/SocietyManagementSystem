using System;

namespace SocietyManagementSystem.Models
{
    public class Maintenance
    {
        public int MaintenanceId { get; set; }

        public int ResidentId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public string? PaymentStatus { get; set; }

        public DateTime? PaidDate { get; set; }

        public string? PaymentMethod { get; set; }

        public string? TransactionReference { get; set; }

        public string? Remarks { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}