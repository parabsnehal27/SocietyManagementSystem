using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Models;

namespace SocietyManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Resident> Residents { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
    }
}