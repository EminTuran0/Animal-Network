using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class AnimalBreed
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public int AnimalTypeId { get; set; }
        public AnimalType AnimalType { get; set; }

        public string Description { get; set; }

        public int? AverageLifespan { get; set; }

        public string CommonTraits { get; set; }

        // Navigation property
        public ICollection<Animal> Animals { get; set; }
    }
}
