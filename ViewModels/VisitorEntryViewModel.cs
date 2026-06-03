using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.ViewModels
{
    public class VisitorEntryViewModel
    {
        [Required]
        public string VisitorName { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Purpose { get; set; }

        public string? VehicleNumber { get; set; }

        public string? IDProofType { get; set; }
        public bool IsIDVerified { get; set; }

        [Required]
        public string FlatNumber { get; set; }
    }
}