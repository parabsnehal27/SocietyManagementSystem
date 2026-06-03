using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.Models
{
    public class MaintenanceConfiguration
    {
        [Key]
        public int Id { get; set; }

        public decimal RepairMaintenance { get; set; }

        public decimal WaterCharges { get; set; }

        public decimal ServiceCharges { get; set; }

        public decimal ElectricityCharges { get; set; }

        public decimal ParkingCharges { get; set; }

        public decimal NonOccupancyCharges { get; set; }

        public decimal HouseKeepingCharges { get; set; }

        public decimal MunicipalTaxes { get; set; }

        public decimal LateFeePercentage { get; set; } = 18;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
