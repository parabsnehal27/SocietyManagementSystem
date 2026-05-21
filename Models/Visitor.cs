using System;

namespace SocietyManagementSystem.Models
{
    public class Visitor
    {
        public int VisitorId { get; set; }

        public string? VisitorName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Purpose { get; set; }

        public string? VehicleNumber { get; set; }

        public string? IDProofType { get; set; }

        public string? IDProofNumber { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}