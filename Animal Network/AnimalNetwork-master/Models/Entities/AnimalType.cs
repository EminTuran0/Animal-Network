using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class AnimalType
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        // Navigation property
        public ICollection<AnimalBreed> Breeds { get; set; }
    }
}
