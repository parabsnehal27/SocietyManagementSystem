namespace SocietyManagementSystem.ViewModels
{
    public class ResidentProfileViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string FlatNumber { get; set; }
        public string Wing { get; set; }
        public int FloorNumber { get; set; }

        public string OwnerOrTenant { get; set; }
        public int FamilyMembersCount { get; set; }

        public string VehicleNumber { get; set; }

        public DateTime MoveInDate { get; set; }

    }
}