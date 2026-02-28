using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class Animal
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public int BreedId { get; set; }
        public AnimalBreed Breed { get; set; }

        public int OwnerId { get; set; }
        public User Owner { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string Gender { get; set; }

        public string Description { get; set; }

        [StringLength(255)]
        public string ProfileImage { get; set; }

        public int? LocationId { get; set; }
        public Location Location { get; set; }

        public bool IsAdoptable { get; set; }

        public DateTime RegisterDate { get; set; }

        // Navigation properties
        public ICollection<Post> Posts { get; set; }
        public ICollection<AnimalFollow> Followers { get; set; }
    }
}
