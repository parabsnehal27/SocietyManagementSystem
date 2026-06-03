using System.ComponentModel.DataAnnotations;

namespace SocietyManagementSystem.Models
{
    public class PushSubscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        public int UserId { get; set; }

        public string Endpoint { get; set; }

        public string P256DH { get; set; }

        public string Auth { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}