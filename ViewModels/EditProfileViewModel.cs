using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string VehicleNumber { get; set; }

        public int FamilyMembersCount { get; set; }
    }
}