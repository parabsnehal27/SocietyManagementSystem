using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.ViewModels
{
    public class RegisterResidentViewModel
    {
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
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FlatNumber { get; set; }

        [Required]
        public string Wing { get; set; }

        public int FloorNumber { get; set; }

        [Required]
        public string OwnerOrTenant { get; set; }

        public int FamilyMembersCount { get; set; }

        public string VehicleNumber { get; set; }

        [Required]
        public string ContactPhone { get; set; }

        [Required]
        public DateTime MoveInDate { get; set; }
    }
}