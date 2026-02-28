using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class Post
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int? AnimalId { get; set; }
        public Animal Animal { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Like> Likes { get; set; }
    }
}
