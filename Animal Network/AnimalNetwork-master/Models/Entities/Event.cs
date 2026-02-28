using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class Event
    {
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; }

        public string Description { get; set; }

        public int? LocationId { get; set; }
        public Location Location { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public int CreatorId { get; set; }
        public User Creator { get; set; }

        public DateTime CreatedAt { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; }

        // Navigation property
        public ICollection<EventParticipant> Participants { get; set; }
    }
}
