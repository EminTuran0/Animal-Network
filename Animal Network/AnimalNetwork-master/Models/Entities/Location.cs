using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace AnimalNetwork.Models.Entities
{
    public class Location
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string Region { get; set; }

        [Required, StringLength(100)]
        public string Country { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; }
        public ICollection<Animal> Animals { get; set; }
        public ICollection<Event> Events { get; set; }
    }
}
