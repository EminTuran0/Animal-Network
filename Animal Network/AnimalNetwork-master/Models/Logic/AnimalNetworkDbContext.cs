using AnimalNetwork.Models;
using AnimalNetwork.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnimalNetwork.Models.Logic
{
    public class AnimalNetworkDbContext : DbContext
    {
        public AnimalNetworkDbContext(DbContextOptions<AnimalNetworkDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Animal> Animals { get; set; }
        public DbSet<AnimalType> AnimalTypes { get; set; }
        public DbSet<AnimalBreed> AnimalBreeds { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<AnimalFollow> AnimalFollows { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Like>()
                .HasIndex(l => new { l.PostId, l.UserId })
                .IsUnique();

            modelBuilder.Entity<Follow>()
                .HasIndex(f => new { f.FollowerId, f.FollowedId })
                .IsUnique();

            modelBuilder.Entity<AnimalFollow>()
                .HasIndex(af => new { af.UserId, af.AnimalId })
                .IsUnique();

            modelBuilder.Entity<EventParticipant>()
                .HasIndex(ep => new { ep.EventId, ep.UserId })
                .IsUnique();

            // Configure relationships

            // User - Animal (One-to-Many)
            modelBuilder.Entity<Animal>()
                .HasOne(a => a.Owner)
                .WithMany(u => u.Animals)
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Post (One-to-Many)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Animal - Post (One-to-Many)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Animal)
                .WithMany(a => a.Posts)
                .HasForeignKey(p => p.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Post - Comment (One-to-Many)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Comment (One-to-Many)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Post - Like (One-to-Many)
            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Like (One-to-Many)
            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Follow (Follower)
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Follow (Followed)
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Followed)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowedId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - AnimalFollow
            modelBuilder.Entity<AnimalFollow>()
                .HasOne(af => af.User)
                .WithMany(u => u.AnimalFollows)
                .HasForeignKey(af => af.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Animal - AnimalFollow
            modelBuilder.Entity<AnimalFollow>()
                .HasOne(af => af.Animal)
                .WithMany(a => a.Followers)
                .HasForeignKey(af => af.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Message (Sender)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Message (Receiver)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Event (Creator)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Event - EventParticipant
            modelBuilder.Entity<EventParticipant>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(ep => ep.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - EventParticipant
            modelBuilder.Entity<EventParticipant>()
                .HasOne(ep => ep.User)
                .WithMany()
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AnimalType - AnimalBreed
            modelBuilder.Entity<AnimalBreed>()
                .HasOne(ab => ab.AnimalType)
                .WithMany(at => at.Breeds)
                .HasForeignKey(ab => ab.AnimalTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // AnimalBreed - Animal
            modelBuilder.Entity<Animal>()
                .HasOne(a => a.Breed)
                .WithMany(ab => ab.Animals)
                .HasForeignKey(a => a.BreedId)
                .OnDelete(DeleteBehavior.Restrict);

            // Location - User
            modelBuilder.Entity<User>()
                .HasOne(u => u.Location)
                .WithMany(l => l.Users)
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Location - Animal
            modelBuilder.Entity<Animal>()
                .HasOne(a => a.Location)
                .WithMany(l => l.Animals)
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Location - Event
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Location)
                .WithMany(l => l.Events)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}