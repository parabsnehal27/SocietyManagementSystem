using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class Notice
    {
        [Key]
        public int NoticeId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        public string? NoticeType { get; set; }

        [ForeignKey("User")]
        public int PostedBy { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public User User { get; set; }
        public string? AttachmentPath { get; set; }
    }
}