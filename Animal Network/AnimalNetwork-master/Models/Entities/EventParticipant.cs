using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class EventParticipant
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; }
    }
}
