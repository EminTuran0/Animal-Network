using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; }

        [Required, StringLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(128)]
        public string PasswordHash { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        public string Bio { get; set; }

        [StringLength(255)]
        public string ProfileImage { get; set; }

        public int? LocationId { get; set; }
        public Location Location { get; set; }

        public bool IsAdmin { get; set; }

        public DateTime JoinDate { get; set; }

        public DateTime? LastLogin { get; set; }

        // Navigation properties
        public ICollection<Animal> Animals { get; set; }
        public ICollection<Post> Posts { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Like> Likes { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<Event> CreatedEvents { get; set; }

        // Followers and following
        public ICollection<Follow> Followers { get; set; }
        public ICollection<Follow> Following { get; set; }

        // Animal follows
        public ICollection<AnimalFollow> AnimalFollows { get; set; }
    }
}
