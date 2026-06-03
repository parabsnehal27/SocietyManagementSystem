using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.ViewModels
{
    public class RegisterSecurityGuardViewModel
    {
        // Users table fields
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        // SecurityGuards table fields
        [Required]
        public string EmployeeCode { get; set; }

        [Required]
        public string ShiftTiming { get; set; }

        [Required]
        public DateTime JoiningDate { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [StringLength(12, MinimumLength = 12, ErrorMessage = "Aadhaar number must be 12 digits.")]
        public string AadhaarNumber { get; set; }

        [Required]
        [Range(1, 100000)]
        public decimal Salary { get; set; }
    }
}