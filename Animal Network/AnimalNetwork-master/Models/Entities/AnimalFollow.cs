namespace AnimalNetwork.Models.Entities
{
    public class AnimalFollow
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int AnimalId { get; set; }
        public Animal Animal { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
