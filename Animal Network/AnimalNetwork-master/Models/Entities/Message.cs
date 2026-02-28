using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class Message
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; }

        public int ReceiverId { get; set; }
        public User Receiver { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }
    }
}
