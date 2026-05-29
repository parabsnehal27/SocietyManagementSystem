using System;
using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.Models
{
    public class SecurityGuard
    {
        [Key]
        public int GuardId { get; set; }

        public int? UserId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? ShiftTiming { get; set; }

        public DateTime? JoiningDate { get; set; }

        public string? Address { get; set; }

        public string? AadhaarNumber { get; set; }

        public decimal? Salary { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}