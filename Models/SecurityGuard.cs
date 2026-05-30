using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class SecurityGuard
    {
        [Key]
        public int GuardId { get; set; }

        // =========================
        // USER RELATIONSHIP
        // =========================

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        // =========================
        // SECURITY DETAILS
        // =========================

        [Required]
        public string ShiftTiming { get; set; }

        public DateTime JoiningDate { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string AadhaarNumber { get; set; }

        [Required]
        public decimal Salary { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}