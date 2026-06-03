namespace SocietyManagementSystem.ViewModels
{
    public class EditSecurityGuardViewModel
    {
        public int GuardId { get; set; }
        public int UserId { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string EmployeeCode { get; set; }
        public string ShiftTiming { get; set; }
        public string Address { get; set; }
        public string AadhaarNumber { get; set; }

        public decimal Salary { get; set; }
    }
}
