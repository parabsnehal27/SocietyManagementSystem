using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class SecurityGuard
    {
        [Key]
        public int GuardId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        public string EmployeeCode { get; set; }

        public string? ShiftTiming { get; set; }

        public DateTime JoiningDate { get; set; }

        public string? Address { get; set; }

        public string? AadhaarNumber { get; set; }

        public decimal Salary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; }
    }
}