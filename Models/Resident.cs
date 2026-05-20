using System;

namespace SocietyManagementSystem.Models
{
    public class Resident
    {
        public int ResidentId { get; set; }

        public int UserId { get; set; }

        public string? FlatNumber { get; set; }

        public string? Wing { get; set; }

        public int? FloorNumber { get; set; }

        public string? OwnerOrTenant { get; set; }

        public int? FamilyMembersCount { get; set; }

        public string? VehicleNumber { get; set; }

        public string? ContactPhone { get; set; }

        public DateTime? MoveInDate { get; set; }

        public bool? IsApproved { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}