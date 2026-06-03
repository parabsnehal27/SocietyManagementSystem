using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class ComplaintAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [ForeignKey("Complaint")]
        public int ComplaintId { get; set; }

        [Required]
        public string FilePath { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public Complaint Complaint { get; set; }
    }
}