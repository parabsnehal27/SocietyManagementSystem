using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocietyManagementSystem.Models
{
    public class NoticeRead
    {
        [Key]
        public int NoticeReadId { get; set; }

        [ForeignKey("Notice")]
        public int NoticeId { get; set; }

        [ForeignKey("Resident")]
        public int ResidentId { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.Now;

        public Notice Notice { get; set; }

        public Resident Resident { get; set; }
    }
}