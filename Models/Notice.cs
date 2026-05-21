using System;

namespace SocietyManagementSystem.Models
{
    public class Notice
    {
        public int NoticeId { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? NoticeType { get; set; }

        public int PostedBy { get; set; }

        public DateTime? PostedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool? IsActive { get; set; }
    }
}