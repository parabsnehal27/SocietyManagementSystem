using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class Resident
    {
        [Key]
        public int ResidentId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        public string FlatNumber { get; set; }

        [Required]
        public string Wing { get; set; }

        public int FloorNumber { get; set; }

        [Required]
        public string OwnerOrTenant { get; set; }

        public int FamilyMembersCount { get; set; }

        public string? VehicleNumber { get; set; }

        [Required]
        public string ContactPhone { get; set; }

        public DateTime MoveInDate { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; }
    }
}