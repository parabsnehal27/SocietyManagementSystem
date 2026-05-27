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
        public DbSet<SecurityGuard> SecurityGuards { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Maintenance> Maintenance { get; set; }
        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<VisitorEntry> VisitorEntries { get; set; }
        public DbSet<NoticeRead> NoticeReads { get; set; }
        public DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }

        public DbSet<PushSubscription> PushSubscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Email unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Resident ↔ User (One-to-One)
            modelBuilder.Entity<Resident>()
                .HasOne(r => r.User)
                .WithOne()
                .HasForeignKey<Resident>(r => r.UserId);

            // SecurityGuard ↔ User (One-to-One)
            modelBuilder.Entity<SecurityGuard>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<SecurityGuard>(s => s.UserId);

            // VisitorEntry ↔ SecurityGuard
            modelBuilder.Entity<VisitorEntry>()
                .HasOne(v => v.SecurityGuard)
                .WithMany()
                .HasForeignKey(v => v.AddedByGuardId)
                .OnDelete(DeleteBehavior.Restrict);

            // VisitorEntry ↔ Resident
            modelBuilder.Entity<VisitorEntry>()
                .HasOne(v => v.Resident)
                .WithMany()
                .HasForeignKey(v => v.ResidentId)
                .OnDelete(DeleteBehavior.Restrict);

            // VisitorEntry ↔ Visitor
            modelBuilder.Entity<VisitorEntry>()
                .HasOne(v => v.Visitor)
                .WithMany()
                .HasForeignKey(v => v.VisitorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}