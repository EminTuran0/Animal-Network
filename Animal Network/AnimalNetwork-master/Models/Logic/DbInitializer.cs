using AnimalNetwork.Models.Entities;
using AnimalNetwork.Models.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimalNetwork.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AnimalNetworkDbContext context)
        {
            // Check if database is already populated
            if (context.Users.Any())
            {
                return; // Database has been seeded already
            }

            // Create locations
            var locations = CreateLocations();
            context.Locations.AddRange(locations);
            context.SaveChanges();

            // Create animal types
            var animalTypes = CreateAnimalTypes();
            context.AnimalTypes.AddRange(animalTypes);
            context.SaveChanges();

            // Create animal breeds
            var animalBreeds = CreateAnimalBreeds(animalTypes);
            context.AnimalBreeds.AddRange(animalBreeds);
            context.SaveChanges();

            // Create users (500 users)
            var users = CreateUsers(locations);
            context.Users.AddRange(users);
            context.SaveChanges();

            // Create animals (1000 animals)
            var animals = CreateAnimals(animalBreeds, users, locations);
            context.Animals.AddRange(animals);
            context.SaveChanges();

            // Create follows between users (2000 follows)
            var follows = CreateFollows(users);
            context.Follows.AddRange(follows);
            context.SaveChanges();

            // Create animal follows (2000 animal follows)
            var animalFollows = CreateAnimalFollows(users, animals);
            context.AnimalFollows.AddRange(animalFollows);
            context.SaveChanges();

            // Create events (100 events)
            var events = CreateEvents(users, locations);
            context.Events.AddRange(events);
            context.SaveChanges();

            // Create event participants (500 participants)
            var eventParticipants = CreateEventParticipants(users, events);
            context.EventParticipants.AddRange(eventParticipants);
            context.SaveChanges();

            // Create posts (3000 posts)
            var posts = CreatePosts(users, animals);
            context.Posts.AddRange(posts);
            context.SaveChanges();

            // Create comments (5000 comments)
            var comments = CreateComments(users, posts);
            context.Comments.AddRange(comments);
            context.SaveChanges();

            // Create likes (3000 likes)
            var likes = CreateLikes(users, posts);
            context.Likes.AddRange(likes);
            context.SaveChanges();

            // Create messages (500 messages)
            var messages = CreateMessages(users);
            context.Messages.AddRange(messages);
            context.SaveChanges();

            Console.WriteLine("Database seeded successfully with initial data.");
        }

        private static List<Location> CreateLocations()
        {
            var locations = new List<Location>
            {
                new Location { City = "Istanbul", Region = "Marmara", Country = "Turkey", PostalCode = "34000", Latitude = 41.0082m, Longitude = 28.9784m },
                new Location { City = "Ankara", Region = "Central Anatolia", Country = "Turkey", PostalCode = "06000", Latitude = 39.9334m, Longitude = 32.8597m },
                new Location { City = "Izmir", Region = "Aegean", Country = "Turkey", PostalCode = "35000", Latitude = 38.4237m, Longitude = 27.1428m },
                new Location { City = "Antalya", Region = "Mediterranean", Country = "Turkey", PostalCode = "07000", Latitude = 36.8969m, Longitude = 30.7133m },
                new Location { City = "Bursa", Region = "Marmara", Country = "Turkey", PostalCode = "16000", Latitude = 40.1885m, Longitude = 29.0610m },
                new Location { City = "Adana", Region = "Mediterranean", Country = "Turkey", PostalCode = "01000", Latitude = 37.0000m, Longitude = 35.3213m },
                new Location { City = "Gaziantep", Region = "Southeastern Anatolia", Country = "Turkey", PostalCode = "27000", Latitude = 37.0662m, Longitude = 37.3833m },
                new Location { City = "Konya", Region = "Central Anatolia", Country = "Turkey", PostalCode = "42000", Latitude = 37.8667m, Longitude = 32.4833m },
                new Location { City = "Mersin", Region = "Mediterranean", Country = "Turkey", PostalCode = "33000", Latitude = 36.8000m, Longitude = 34.6333m },
                new Location { City = "Diyarbakir", Region = "Southeastern Anatolia", Country = "Turkey", PostalCode = "21000", Latitude = 37.9144m, Longitude = 40.2306m },
                new Location { City = "London", Region = "England", Country = "United Kingdom", PostalCode = "SW1A", Latitude = 51.5074m, Longitude = -0.1278m },
                new Location { City = "New York", Region = "NY", Country = "USA", PostalCode = "10001", Latitude = 40.7128m, Longitude = -74.0060m },
                new Location { City = "Paris", Region = "Île-de-France", Country = "France", PostalCode = "75000", Latitude = 48.8566m, Longitude = 2.3522m },
                new Location { City = "Berlin", Region = "Berlin", Country = "Germany", PostalCode = "10115", Latitude = 52.5200m, Longitude = 13.4050m },
                new Location { City = "Rome", Region = "Lazio", Country = "Italy", PostalCode = "00100", Latitude = 41.9028m, Longitude = 12.4964m },
                new Location { City = "Madrid", Region = "Community of Madrid", Country = "Spain", PostalCode = "28001", Latitude = 40.4168m, Longitude = -3.7038m },
                new Location { City = "Tokyo", Region = "Kanto", Country = "Japan", PostalCode = "100-0001", Latitude = 35.6762m, Longitude = 139.6503m },
                new Location { City = "Sydney", Region = "New South Wales", Country = "Australia", PostalCode = "2000", Latitude = -33.8688m, Longitude = 151.2093m },
                new Location { City = "Toronto", Region = "Ontario", Country = "Canada", PostalCode = "M5H", Latitude = 43.6532m, Longitude = -79.3832m },
                new Location { City = "Dubai", Region = "Dubai", Country = "UAE", PostalCode = "00000", Latitude = 25.2048m, Longitude = 55.2708m }
            };

            return locations;
        }

        private static List<AnimalType> CreateAnimalTypes()
        {
            var animalTypes = new List<AnimalType>
            {
                new AnimalType { Name = "Dog", Description = "Domesticated mammals known for loyalty and companionship." },
                new AnimalType { Name = "Cat", Description = "Small carnivorous mammals known for independence and agility." },
                new AnimalType { Name = "Bird", Description = "Warm-blooded vertebrates characterized by feathers and wings." },
                new AnimalType { Name = "Fish", Description = "Aquatic vertebrates with gills and fins." },
                new AnimalType { Name = "Rabbit", Description = "Small mammals in the family Leporidae." },
                new AnimalType { Name = "Hamster", Description = "Small rodents belonging to the subfamily Cricetinae." },
                new AnimalType { Name = "Guinea Pig", Description = "Rodents belonging to the family Caviidae." },
                new AnimalType { Name = "Turtle", Description = "Reptiles with a special bony shell developed from their ribs." },
                new AnimalType { Name = "Horse", Description = "Large hoofed mammals belonging to the taxonomic family Equidae." },
                new AnimalType { Name = "Reptile", Description = "Cold-blooded vertebrates including snakes, lizards, and crocodiles." }
            };

            return animalTypes;
        }

        private static List<AnimalBreed> CreateAnimalBreeds(List<AnimalType> animalTypes)
        {
            var animalBreeds = new List<AnimalBreed>();
            var random = new Random();

            // Dog breeds
            var dogType = animalTypes.First(at => at.Name == "Dog");
            animalBreeds.AddRange(new List<AnimalBreed>
            {
                new AnimalBreed { Name = "Labrador Retriever", AnimalTypeId = dogType.Id, Description = "Friendly, outgoing, and high-spirited companions.", AverageLifespan = 12, CommonTraits = "Friendly, Active, Outgoing" },
                new AnimalBreed { Name = "German Shepherd", AnimalTypeId = dogType.Id, Description = "Confident, courageous, and intelligent working dogs.", AverageLifespan = 11, CommonTraits = "Intelligent, Loyal, Confident" },
                new AnimalBreed { Name = "Golden Retriever", AnimalTypeId = dogType.Id, Description = "Intelligent, friendly, and devoted companions.", AverageLifespan = 12, CommonTraits = "Reliable, Trustworthy, Confident" },
                new AnimalBreed { Name = "French Bulldog", AnimalTypeId = dogType.Id, Description = "Playful, smart, and adaptable companions.", AverageLifespan = 11, CommonTraits = "Playful, Alert, Smart" },
                new AnimalBreed { Name = "Beagle", AnimalTypeId = dogType.Id, Description = "Curious, friendly, and merry companions.", AverageLifespan = 13, CommonTraits = "Amiable, Even Tempered, Determined" },
                new AnimalBreed { Name = "Poodle", AnimalTypeId = dogType.Id, Description = "Active, intelligent, and elegant companions.", AverageLifespan = 14, CommonTraits = "Intelligent, Active, Alert" },
                new AnimalBreed { Name = "Siberian Husky", AnimalTypeId = dogType.Id, Description = "Outgoing, friendly, and dignified dogs.", AverageLifespan = 12, CommonTraits = "Outgoing, Mischievous, Loyal" },
                new AnimalBreed { Name = "Yorkshire Terrier", AnimalTypeId = dogType.Id, Description = "Affectionate, sprightly, and tomboyish companions.", AverageLifespan = 15, CommonTraits = "Affectionate, Sprightly, Intelligent" },
                new AnimalBreed { Name = "Boxer", AnimalTypeId = dogType.Id, Description = "Bright, energetic, and playful companions.", AverageLifespan = 10, CommonTraits = "Bright, Energetic, Loyal" },
                new AnimalBreed { Name = "Dachshund", AnimalTypeId = dogType.Id, Description = "Clever, lively, and courageous companions.", AverageLifespan = 13, CommonTraits = "Clever, Lively, Courageous" }
            });

            // Cat breeds
            var catType = animalTypes.First(at => at.Name == "Cat");
            animalBreeds.AddRange(new List<AnimalBreed>
            {
                new AnimalBreed { Name = "Persian", AnimalTypeId = catType.Id, Description = "Sweet, gentle, and quiet companions.", AverageLifespan = 15, CommonTraits = "Sweet, Gentle, Quiet" },
                new AnimalBreed { Name = "Maine Coon", AnimalTypeId = catType.Id, Description = "Gentle, intelligent, and friendly companions.", AverageLifespan = 14, CommonTraits = "Gentle, Intelligent, Friendly" },
                new AnimalBreed { Name = "Siamese", AnimalTypeId = catType.Id, Description = "Curious, intelligent, and social companions.", AverageLifespan = 15, CommonTraits = "Curious, Intelligent, Social" },
                new AnimalBreed { Name = "Ragdoll", AnimalTypeId = catType.Id, Description = "Gentle, calm, and sociable companions.", AverageLifespan = 14, CommonTraits = "Gentle, Calm, Sociable" },
                new AnimalBreed { Name = "Bengal", AnimalTypeId = catType.Id, Description = "Active, energetic, and playful companions.", AverageLifespan = 14, CommonTraits = "Active, Energetic, Playful" },
                new AnimalBreed { Name = "Sphynx", AnimalTypeId = catType.Id, Description = "Energetic, intelligent, and outgoing companions.", AverageLifespan = 13, CommonTraits = "Energetic, Intelligent, Outgoing" },
                new AnimalBreed { Name = "British Shorthair", AnimalTypeId = catType.Id, Description = "Easygoing, calm, and affectionate companions.", AverageLifespan = 14, CommonTraits = "Easygoing, Calm, Affectionate" },
                new AnimalBreed { Name = "Abyssinian", AnimalTypeId = catType.Id, Description = "Active, playful, and curious companions.", AverageLifespan = 15, CommonTraits = "Active, Playful, Curious" },
                new AnimalBreed { Name = "Scottish Fold", AnimalTypeId = catType.Id, Description = "Intelligent, sweet-tempered, and adaptable companions.", AverageLifespan = 13, CommonTraits = "Intelligent, Sweet-tempered, Adaptable" },
                new AnimalBreed { Name = "Turkish Angora", AnimalTypeId = catType.Id, Description = "Playful, intelligent, and adaptable companions.", AverageLifespan = 16, CommonTraits = "Playful, Intelligent, Adaptable" }
            });

            // Bird breeds
            var birdType = animalTypes.First(at => at.Name == "Bird");
            animalBreeds.AddRange(new List<AnimalBreed>
            {
                new AnimalBreed { Name = "Canary", AnimalTypeId = birdType.Id, Description = "Small, colorful singing birds.", AverageLifespan = 10, CommonTraits = "Vocal, Active, Colorful" },
                new AnimalBreed { Name = "Budgerigar", AnimalTypeId = birdType.Id, Description = "Small, long-tailed, seed-eating parrots.", AverageLifespan = 8, CommonTraits = "Social, Playful, Intelligent" },
                new AnimalBreed { Name = "Cockatiel", AnimalTypeId = birdType.Id, Description = "Small parrots characterized by their distinctive yellow crests.", AverageLifespan = 15, CommonTraits = "Vocal, Social, Affectionate" },
                new AnimalBreed { Name = "Lovebird", AnimalTypeId = birdType.Id, Description = "Small parrots known for their strong, monogamous pair bonding.", AverageLifespan = 12, CommonTraits = "Social, Playful, Curious" },
                new AnimalBreed { Name = "Finch", AnimalTypeId = birdType.Id, Description = "Small to medium-sized passerine birds.", AverageLifespan = 6, CommonTraits = "Social, Active, Vocal" }
            });

            // Rabbit breeds
            var rabbitType = animalTypes.First(at => at.Name == "Rabbit");
            animalBreeds.AddRange(new List<AnimalBreed>
            {
                new AnimalBreed { Name = "Holland Lop", AnimalTypeId = rabbitType.Id, Description = "Small rabbits characterized by their lopped ears.", AverageLifespan = 8, CommonTraits = "Friendly, Energetic, Curious" },
                new AnimalBreed { Name = "Mini Rex", AnimalTypeId = rabbitType.Id, Description = "Small rabbits known for their plush, velvet-like fur.", AverageLifespan = 7, CommonTraits = "Calm, Gentle, Soft-furred" },
                new AnimalBreed { Name = "Netherland Dwarf", AnimalTypeId = rabbitType.Id, Description = "One of the smallest rabbit breeds.", AverageLifespan = 10, CommonTraits = "Energetic, Curious, Tiny" },
                new AnimalBreed { Name = "Flemish Giant", AnimalTypeId = rabbitType.Id, Description = "One of the largest rabbit breeds.", AverageLifespan = 8, CommonTraits = "Docile, Gentle, Large" },
                new AnimalBreed { Name = "Lionhead", AnimalTypeId = rabbitType.Id, Description = "Rabbits characterized by their wool mane around the head.", AverageLifespan = 9, CommonTraits = "Playful, Friendly, Unique" }
            });

            // Add a few breeds for the remaining animal types
            foreach (var animalType in animalTypes.Where(at =>
                at.Name != "Dog" &&
                at.Name != "Cat" &&
                at.Name != "Bird" &&
                at.Name != "Rabbit"))
            {
                for (int i = 0; i < 3; i++)
                {
                    animalBreeds.Add(new AnimalBreed
                    {
                        Name = $"{animalType.Name} Breed {i + 1}",
                        AnimalTypeId = animalType.Id,
                        Description = $"A common {animalType.Name.ToLower()} breed.",
                        AverageLifespan = random.Next(5, 20),
                        CommonTraits = "Varies by individual"
                    });
                }
            }

            return animalBreeds;
        }

        private static List<User> CreateUsers(List<Location> locations)
        {
            var users = new List<User>();
            var random = new Random();
            var firstNames = new[] { "Emma", "Liam", "Olivia", "Noah", "Ava", "Oliver", "Isabella", "William", "Sophia", "Elijah", "Mia", "James", "Charlotte", "Benjamin", "Amelia", "Lucas", "Harper", "Mason", "Evelyn", "Logan", "Abigail", "Alexander", "Emily", "Ethan", "Elizabeth", "Jacob", "Mila", "Michael", "Ella", "Daniel", "Avery", "Henry", "Sofia", "Jackson", "Camila", "Sebastian", "Aria", "Aiden", "Scarlett", "Matthew", "Victoria", "Samuel", "Madison", "David", "Luna", "Joseph", "Grace", "Carter", "Chloe", "Owen", "Penelope", "Wyatt", "Layla", "John", "Riley", "Jack", "Zoey", "Luke", "Nora", "Jayden", "Lily", "Dylan", "Eleanor", "Grayson", "Hannah", "Levi", "Lillian", "Isaac", "Addison", "Gabriel", "Aubrey", "Julian", "Ellie", "Mateo", "Stella", "Anthony", "Natalie", "Jaxon", "Zoe", "Lincoln", "Leah", "Joshua", "Hazel", "Christopher", "Violet", "Andrew", "Aurora", "Theodore", "Savannah", "Caleb", "Audrey", "Ryan", "Brooklyn", "Asher", "Bella", "Nathan", "Claire", "Thomas", "Skylar", "Leo", "Lucy", "Isaiah", "Paisley", "Charles", "Everly", "Josiah", "Anna", "Hudson", "Caroline", "Christian", "Nova", "Hunter", "Genesis", "Connor", "Emilia", "Eli", "Kennedy", "Ezra", "Samantha", "Aaron", "Maya", "Landon", "Willow", "Adrian", "Kinsley", "Jonathan", "Naomi", "Nolan", "Aaliyah", "Jeremiah", "Elena", "Easton", "Sarah", "Elias", "Ariana", "Colton", "Allison", "Cameron", "Gabriella", "Carson", "Alice", "Robert", "Madelyn", "Angel", "Cora", "Maverick", "Ruby", "Nicholas", "Eva", "Dominic", "Serenity", "Jace", "Autumn", "Brayden", "Adeline", "Gael", "Hailey", "Rowan", "Gianna", "Harrison", "Valentina", "Bryson", "Isla", "Sawyer", "Eliana", "Amir", "Quinn", "Kingston", "Nevaeh", "Jason", "Ivy", "Giovanni", "Sadie", "Vincent", "Piper", "Ayden", "Lydia", "Chase", "Alexa", "Myles", "Josephine", "Zachary", "Emery", "Declan", "Julia", "Ezekiel", "Delilah", "Kai", "Arianna", "Cooper", "Vivian", "Bennett", "Kaylee", "Bentley", "Sophie", "Xavier", "Brielle", "Jaxson", "Madeline", "Parker", "Peyton", "Roman", "Rylee", "Jason", "Clara", "Santiago", "Hadley", "Cole", "Melanie", "Carlos", "Mackenzie", "Luis", "Reagan", "Damian", "Adalynn", "Max", "Liliana", "Everett", "Aubree", "Evan", "Jade", "Eduardo", "Katherine", "Jameson", "Isabelle", "Kevin", "Natalia", "Axel", "Raelynn", "Silas", "Maria", "Brandon", "Athena", "Theo", "Ximena", "Jude", "Arya", "Adam", "Leilani", "Weston", "Taylor", "Jose", "Faith", "Zion", "Rose", "Calvin", "Kylie", "Elliott", "Alexandra", "Malachi", "Mary", "Andres", "Margaret", "Emmanuel", "Lyla", "Leonardo", "Ashley", "Ivan", "Amaya", "Brooks", "Eliza", "Miles", "Brianna", "Zane", "Bailey", "Justin", "Andrea", "Ashton", "Khloe", "Greyson", "Jasmine", "Dean", "Melody", "Lorenzo", "Iris", "Timothy", "Isabel", "Richard", "Norah", "Nehemiah", "Annabelle", "August", "Valeria", "Spencer", "Emerson", "Ezequiel", "Maria", "Jayce", "Adalyn", "Beau", "Rebecca", "Noel", "Juliana", "Javier", "Kimberly", "Karter", "Mariah", "Colt", "Genevieve", "Rhett", "Lyric", "Judah", "Paradise", "Tucker", "Jayla", "Emmett", "Angela", "Avery", "Gracie", "Finn", "Laila", "Oakley", "Alaina", "Archer", "Ruth", "Scott", "Princess", "Victor", "Marley", "George", "Yaretzi", "Antonio", "Hallie", "Cohen", "Daisy", "Jeremy", "London", "Knox", "Diana", "Brady", "Charlie", "Kenneth", "Georgia", "Kye", "Journee", "Beckett", "Daniela", "Xander", "Mikayla", "Derek", "Skyler", "Donovan", "Presley", "Tate", "Cecilia", "Remington", "Anastasia", "Kristian", "Elliana", "Mark", "Mckenzie", "Wade", "Alana", "Joe", "Amy", "Phoenix", "Rachel", "Maximus", "Ana", "Brian", "Blake", "Cruz", "Morgan", "Kaleb", "Aliyah", "Rocco", "Alina", "Maddox", "Molly", "Sean", "Rosalie", "Kaden", "Callie", "Nikolai", "Juliet", "Keegan", "Cali", "Amir", "Londyn", "Matteo", "Dahlia", "Ibrahim", "Johanna", "Arthur", "Laura", "Kash", "Adelynn", "Walter", "Vivienne", "Colin", "Sawyer", "Lukas", "Camille", "Rafael", "Amari", "Cade", "Malia", "Gunner", "Sabrina", "Garrett", "Lia", "Erik", "Charlee", "Ricardo", "Tessa", "Martin", "Maggie", "Ryker", "Amanda", "Cody", "Kate", "Jett", "Alexandria", "Simon", "Alana", "Graham", "Adelaide", "Dallas", "Olive", "Paul", "Teagan", "Ali", "Joy", "Luca", "Gracelynn", "Callum", "Gracelyn", "Sage", "Haven", "Travis", "Paige", "Malcolm", "Kamila", "Casey", "Brynlee", "Ridge", "Gia", "Sterling", "Kailani", "Zayden", "Aisha", "Franklin", "Sienna", "Chance", "Myla", "Holden", "Lacey", "Paxton", "Lexie", "Kaiden", "Juliette", "Anderson", "Sutton", "Gregory", "River", "Raymond", "Alexis", "Abraham", "Sasha", "Messiah", "Mariana", "Peter", "Fernanda", "Reid", "Alayah", "Johnny", "Jasmin", "River", "Nina", "Julius", "Catalina", "Jacob", "Reese", "Shane", "Lila", "Ryder", "Arabella", "Cyrus", "Raelyn", "Keanu", "Farrah", "Cash", "Nalani", "Troy", "Lilah", "Abel", "Sara", "Ronan", "Adriana", "Beckett", "Nicole", "Devin", "Journey", "Jayceon", "Aniyah", "Hank", "Elise", "Corbin", "Kaydence", "Kameron", "Octavia", "Tobias", "Rowan", "Finn", "Miracle", "Mathias", "Angelina", "Harvey", "Carmen", "Rhodes", "Harley", "Tobias", "Ryan", "Miller", "Vanessa", "Jax", "Shelby", "Lewis", "Cataleya", "Marcos", "Anaya", "Landon", "Esmeralda", "Duke", "Celine", "Edgar", "Hayden", "Hayes", "Logan", "Jesse", "Angel", "Jay", "Danielle", "Cairo", "Oaklyn", "Benson", "Luciana", "Otto", "Elsie", "Jenson", "Kassidy", "Cairo", "Michelle", "Cannon", "June", "Hendrix", "Vienna", "Lawson", "Ophelia", "Steven", "Everleigh", "Arlo", "Lorelei" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson", "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers", "Long", "Ross", "Foster", "Jimenez", "Powell", "Jenkins", "Perry", "Russell", "Sullivan", "Bell", "Coleman", "Butler", "Henderson", "Barnes", "Gonzales", "Fisher", "Vasquez", "Simmons", "Romero", "Jordan", "Patterson", "Alexander", "Hamilton", "Graham", "Reynolds", "Griffin", "Wallace", "Moreno", "West", "Cole", "Hayes", "Bryant", "Herrera", "Gibson", "Ellis", "Tran", "Medina", "Aguilar", "Stevens", "Murray", "Ford", "Castro", "Marshall", "Owens", "Harrison", "Fernandez", "McDonald", "Woods", "Washington", "Kennedy", "Wells", "Chen", "Hoffman", "Meyer", "Soto", "Walsh", "Diaz", "Larson", "Webb", "Stephens", "Ferguson", "Ramos", "Carlson", "Garza", "Cunningham", "Spencer", "Fisher", "Little", "Wheeler", "Jensen", "Rhodes", "Ryan", "Ferguson", "Nichols", "Rose", "Stone", "Hawkins", "Dunn", "Perkins", "Hudson", "Spencer", "Gardner", "Stephens", "Payne", "Pierce", "Berry", "Matthews", "Arnold", "Wagner", "Willis", "Ray", "Watkins", "Olson", "Carroll", "Duncan", "Snyder", "Hart", "Cunningham", "Bradley", "Lane", "Andrews", "Ruiz", "Harper", "Fox", "Riley", "Armstrong", "Carpenter", "Weaver", "Greene", "Lawrence", "Elliott", "Chavez", "Sims", "Austin", "Peters", "Kelley", "Franklin", "Lawson", "Fields", "Gutierrez", "Schmidt", "Carr", "Vasquez", "Castillo", "Wheeler", "Chapman", "Oliver", "Montgomery", "Richards", "Williamson", "Johnston", "Banks", "Meyer", "Bishop", "McCoy", "Howell", "Alvarez", "Morrison", "Hansen", "Fernandez", "Garza", "Harvey", "Little", "Burton", "Stanley", "Nguyen", "George", "Jacobs", "Reid", "Kim", "Fuller", "Lynch", "Dean", "Gilbert", "Garrett", "Romero", "Welch", "Larson", "Frazier", "Burke", "Hanson", "Day", "Mendoza", "Moreno", "Bowman", "Medina", "Fowler", "Brewer", "Hoffman", "Carlson", "Silva", "Pearson", "Holland", "Douglas", "Fleming", "Jensen", "Vargas", "Byrd", "Davidson" };

            // Generate 500 users
            for (int i = 0; i < 500; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var username = $"{firstName.ToLower()}{lastName.ToLower()}{random.Next(1, 1000)}";
                var email = $"{username}@example.com";

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), // In a real app, use a secure password hashing method
                    FirstName = firstName,
                    LastName = lastName,
                    Bio = $"Hi, I'm {firstName}! I love animals and sharing their adventures.",
                    ProfileImage = $"/images/profiles/user_{i + 1}.jpg",
                    LocationId = locations[random.Next(locations.Count)].Id,
                    IsAdmin = i < 5, // Make the first 5 users admins
                    JoinDate = DateTime.Now.AddDays(-random.Next(1, 365)),
                    LastLogin = DateTime.Now.AddDays(-random.Next(0, 30))
                };

                users.Add(user);
            }

            return users;
        }

        private static List<Animal> CreateAnimals(List<AnimalBreed> animalBreeds, List<User> users, List<Location> locations)
        {
            var animals = new List<Animal>();
            var random = new Random();
            var animalNames = new[] { "Max", "Bella", "Charlie", "Lucy", "Cooper", "Luna", "Milo", "Daisy", "Rocky", "Molly", "Bear", "Sophie", "Duke", "Sadie", "Teddy", "Maggie", "Winston", "Chloe", "Oliver", "Bailey", "Leo", "Stella", "Bentley", "Nala", "Zeus", "Layla", "Jax", "Zoe", "Archie", "Coco", "Oscar", "Penny", "Tank", "Rosie", "Finn", "Ruby", "Louie", "Gracie", "Rex", "Willow", "Dexter", "Lola", "Simba", "Abby", "George", "Ellie", "Scout", "Dixie", "Rusty", "Belle", "Baxter", "Piper", "Blue", "Ginger", "Diesel", "Roxy", "Harley", "Lily", "Jackson", "Mia", "Bruce", "Nova", "Lucky", "Shadow", "Riley", "Peanut", "Sam", "Sasha", "Koda", "Athena", "Beau", "Emma", "Jasper", "Annie", "Tucker", "Olive", "Murphy", "Honey", "Ace", "Princess", "Gus", "Izzy", "Marley", "Lexi", "Boomer", "Lulu", "Ziggy", "Madison", "Sammy", "Pepper", "Hunter", "Maya", "Buddy", "Arya", "Chester", "Millie", "Henry", "Phoebe", "Toby", "Callie", "Copper", "Josie", "Jake", "Hazel", "Bo", "Winnie", "Moose", "Kinsley", "Bandit", "Quinn", "Buster", "Paisley", "Watson", "Aspen", "Bruno", "Charlotte", "Rocket", "Kona", "Thor", "Violet", "Theo", "Brooklyn", "Atlas", "Scarlett", "Otis", "Harper", "Chewy", "Ivy", "Dash", "Georgia", "Hank", "Nora", "Mac", "Skylar", "Cody", "Maisie", "Maverick", "Eve", "King", "Delilah", "Prince", "Ayla", "Joey", "Oakley", "Goose", "Millie", "Walter", "Remi", "Oakley", "Angel", "Ozzy", "Clara", "Ranger", "Hadley", "Titan", "Pebbles", "Mack", "Holly", "Frank", "Frankie", "Chief", "Bonnie", "Romeo", "Gigi", "Rudy", "Penelope", "Apollo", "Dottie", "Axel", "Clementine", "Ollie", "Poppy", "Roscoe", "Sandy", "Gunner", "Juniper", "Chip", "Minnie", "Louis", "Alyssa", "Whiskey", "Joy", "Hershey", "Jade", "Remy", "Sunny", "Major", "Sydney", "Kobe", "India", "Harry", "Fiona", "Scooter", "Flora", "Newton", "Pearl", "Cash", "Aurora", "Rusty", "Stevie", "Arlo", "Xena", "Samson", "Olive", "Roger", "Skye", "Wally", "Shelby", "Oreo", "Sage", "Monte", "Morgan", "Rambo", "Oakley", "Winston", "Eleanor", "Porter", "Betty", "Monty", "Amelia", "Doug", "Onyx", "Hercules", "Trixie", "Captain", "Reese", "Kane", "Blaze", "Bolt", "Rory", "Lightning", "Goldie", "Dodger", "Summer", "Denver", "Cricket", "Bernie", "Fern", "Gizmo", "Journey", "Hudson", "Opal", "Rascal", "Tallulah", "Draco", "Hope", "Humphrey", "Pixie", "Angus", "Maisy", "Shiloh", "Meadow", "Bowie", "Nellie", "Ozzie", "Misty", "Duncan", "Bridget", "Pickle", "Buttercup", "Flash", "Jubilee", "Rio", "Sparkle", "Pongo", "Star", "Winter", "Candy", "Atlas", "Dot", "Goliath", "Destiny", "Scooby", "Emerald", "Winston", "Stormy", "Odin", "Jubilee", "Alfred", "Coco", "Felix", "Cora", "Olaf", "Twinkle", "Sunny", "Dawn", "Gus", "Nyx", "Zeke", "Zelda", "Clifford", "Melody", "Simba", "Gloria", "Pluto", "Venus", "Nemo", "Mercury", "Bambi", "Bloom", "Raven", "Tulip", "Sylvester", "Daisy", "Tiger", "Laurel", "Mister", "Harmony", "Tom", "Dahlia", "Smokey", "Petal", "Garfield", "Blossom", "Stuart", "Clover", "Yogi", "Iris", "Paddington", "Marigold", "Hobbes", "Lotus", "Pooh", "Fawn", "Eeyore", "Dove", "Kermit", "Wren", "Mickey", "Sparrow", "Goofy", "Phoenix", "Donald", "Falcon", "Bugs", "Eagle", "Tweety", "Hawk", "Woodstock", "Robin", "Snoopy", "Cardinal", "Doodle", "Swan", "Fozzie", "Raven", "Gonzo", "Starling", "Kermit", "Finch", "Elmo", "Lark", "Barney", "Nightingale", "Cookie", "Bluejay", "Grover", "Canary", "Snuffy", "Chickadee", "Big Bird", "Owl" };

            // Generate 1000 animals
            for (int i = 0; i < 1000; i++)
            {
                var animalName = animalNames[random.Next(animalNames.Length)];
                var breed = animalBreeds[random.Next(animalBreeds.Count)];
                var owner = users[random.Next(users.Count)];
                var location = random.Next(2) == 0 ? owner.LocationId : locations[random.Next(locations.Count)].Id;
                var genders = new[] { "Male", "Female" };

                var animal = new Animal
                {
                    Name = animalName,
                    BreedId = breed.Id,
                    OwnerId = owner.Id,
                    DateOfBirth = DateTime.Now.AddYears(-random.Next(1, 10)).AddMonths(-random.Next(0, 12)),
                    Gender = genders[random.Next(genders.Length)],
                    Description = $"{animalName} is a loving {breed.Name} who enjoys {(random.Next(2) == 0 ? "playing outside" : "cuddling inside")}.",
                    ProfileImage = $"/images/animals/animal_{i + 1}.jpg",
                    LocationId = location,
                    IsAdoptable = random.Next(5) == 0, // 20% of animals are adoptable
                    RegisterDate = DateTime.Now.AddDays(-random.Next(1, 365))
                };

                animals.Add(animal);
            }

            return animals;
        }

        private static List<Follow> CreateFollows(List<User> users)
        {
            var follows = new List<Follow>();
            var random = new Random();
            var addedPairs = new HashSet<string>(); // To avoid duplicate follows

            // Generate 2000 follows
            for (int i = 0; i < 2000; i++)
            {
                var followerId = random.Next(users.Count);
                var followedId = random.Next(users.Count);

                // Ensure follower and followed are not the same person
                while (followerId == followedId)
                {
                    followedId = random.Next(users.Count);
                }

                var pair = $"{followerId}-{followedId}";

                // Check if this pair already exists
                if (!addedPairs.Contains(pair))
                {
                    follows.Add(new Follow
                    {
                        FollowerId = users[followerId].Id,
                        FollowedId = users[followedId].Id,
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 365))
                    });

                    addedPairs.Add(pair);
                }
                else
                {
                    // If duplicate, try again
                    i--;
                }
            }

            return follows;
        }

        private static List<AnimalFollow> CreateAnimalFollows(List<User> users, List<Animal> animals)
        {
            var animalFollows = new List<AnimalFollow>();
            var random = new Random();
            var addedPairs = new HashSet<string>(); // To avoid duplicate follows

            // Generate 2000 animal follows
            for (int i = 0; i < 2000; i++)
            {
                var userId = random.Next(users.Count);
                var animalId = random.Next(animals.Count);

                var pair = $"{userId}-{animalId}";

                // Check if this pair already exists
                if (!addedPairs.Contains(pair))
                {
                    animalFollows.Add(new AnimalFollow
                    {
                        UserId = users[userId].Id,
                        AnimalId = animals[animalId].Id,
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 365))
                    });

                    addedPairs.Add(pair);
                }
                else
                {
                    // If duplicate, try again
                    i--;
                }
            }

            return animalFollows;
        }

        private static List<Event> CreateEvents(List<User> users, List<Location> locations)
        {
            var events = new List<Event>();
            var random = new Random();
            var eventTitles = new[]
            {
                "Pet Adoption Day", "Dog Training Workshop", "Cat Show", "Kitten Socialization", "Puppy Playdate",
                "Pet Health Seminar", "Animal Rescue Fundraiser", "Dog Agility Competition", "Pet Photography Session",
                "Animal Wellness Fair", "Exotic Pet Expo", "Dog Walking Marathon", "Pet First Aid Course", "Bird Watching Tour",
                "Animal Shelter Volunteer Day", "Reptile Expo", "Aquarium Community Meeting", "Pet Costume Contest",
                "Small Animal Show", "Veterinary Open House", "Pet Supply Drive", "Wildlife Conservation Talk",
                "Animal Behavior Workshop", "Pet Memorial Service", "Animal Art Exhibition", "Pet Talent Show",
                "Animal Therapy Orientation", "Pet Nutrition Seminar", "Animal Rights Advocacy Meeting", "Service Dog Training Demo"
            };

            // Generate 100 events
            for (int i = 0; i < 100; i++)
            {
                var creator = users[random.Next(users.Count)];
                var eventTitle = eventTitles[random.Next(eventTitles.Length)];
                var startTime = DateTime.Now.AddDays(random.Next(-30, 60)); // Events from past month to next 2 months

                var newEvent = new Event
                {
                    Title = eventTitle,
                    Description = $"Join us for this exciting event! {(random.Next(2) == 0 ? "All pets welcome." : "Please bring your furry friends!")}",
                    LocationId = locations[random.Next(locations.Count)].Id,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(random.Next(1, 5)),
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.Now.AddDays(-random.Next(1, 60)),
                    ImageUrl = $"/images/events/event_{i + 1}.jpg"
                };

                events.Add(newEvent);
            }

            return events;
        }

        private static List<EventParticipant> CreateEventParticipants(List<User> users, List<Event> events)
        {
            var participants = new List<EventParticipant>();
            var random = new Random();
            var addedPairs = new HashSet<string>(); // To avoid duplicate participants
            var statuses = new[] { "Going", "Maybe", "Interested" };

            // Generate 500 event participants
            for (int i = 0; i < 500; i++)
            {
                var userId = random.Next(users.Count);
                var eventId = random.Next(events.Count);

                var pair = $"{userId}-{eventId}";

                // Check if this pair already exists
                if (!addedPairs.Contains(pair))
                {
                    participants.Add(new EventParticipant
                    {
                        UserId = users[userId].Id,
                        EventId = events[eventId].Id,
                        Status = statuses[random.Next(statuses.Length)]
                    });

                    addedPairs.Add(pair);
                }
                else
                {
                    // If duplicate, try again
                    i--;
                }
            }

            return participants;
        }

        private static List<Post> CreatePosts(List<User> users, List<Animal> animals)
        {
            var posts = new List<Post>();
            var random = new Random();
            var postContent = new[]
            {
                "Just had the best day at the park! {0} loved chasing squirrels.",
                "Bath time was a success today. {0} actually enjoyed it!",
                "Look at how cute {0} is when sleeping!",
                "First day with my new pet {0}. We're going to be best friends!",
                "Happy birthday to {0}! {1} years old today.",
                "Had to take {0} to the vet today. Everything looks good!",
                "Caught {0} being naughty today. How can I be mad at that face?",
                "Training session with {0} went really well today. So proud!",
                "Just got a new toy for {0} and it's already a favorite.",
                "Cuddle time with {0} is the best part of my day.",
                "Beach day with {0} was amazing! First time seeing the ocean.",
                "{0} made a new friend at the dog park today!",
                "Just adopted {0} from the shelter. Welcome to your forever home!",
                "Hiking adventure with {0} today. We're both exhausted!",
                "Rainy day snuggles with {0} are the best.",
                "Check out this cool trick {0} just learned!",
                "Road trip with {0} was a success. Such a good traveler!",
                "Snow day! {0} experienced snow for the first time.",
                "Guess who got a new haircut? {0} looks so different!",
                "Someone is excited about their new bed! {0} hasn't left it all day.",
                "Had a professional photoshoot with {0} today. Can't wait to see the pictures!",
                "Celebrating {0}'s adoption anniversary today! Best decision ever.",
                "{0} is helping me garden today. More digging than helping!",
                "My work from home assistant {0} is demanding pets instead of letting me work.",
                "Lazy Sunday with {0}. Perfect day!",
                "Check out {0}'s Halloween costume! Too cute!",
                "{0} has a new brother/sister! They're getting along great.",
                "Taught {0} a new command today. So smart!",
                "Sunset walk with {0} - perfect end to the day.",
                "Just meal prepped for {0} for the week. Such a spoiled pet!"
            };

            // Generate 3000 posts
            for (int i = 0; i < 3000; i++)
            {
                var user = users[random.Next(users.Count)];
                var includeAnimal = random.Next(100) < 80; // 80% of posts include an animal
                var animal = includeAnimal ? animals[random.Next(animals.Count)] : null;
                var content = postContent[random.Next(postContent.Length)];

                // Replace placeholders if an animal is included
                if (includeAnimal && animal != null)
                {
                    content = string.Format(content, animal.Name, random.Next(1, 15));
                }
                else
                {
                    content = "Just another day loving animals and pet life!";
                }

                var post = new Post
                {
                    UserId = user.Id,
                    AnimalId = includeAnimal && animal != null ? animal.Id : null,
                    Content = content,
                    ImageUrl = random.Next(100) < 70 ? $"/images/posts/post_{i + 1}.jpg" : null, // 70% of posts have images
                    CreatedAt = DateTime.Now.AddDays(-random.Next(1, 365)),
                    UpdatedAt = random.Next(100) < 15 ? DateTime.Now.AddDays(-random.Next(0, 30)) : null // 15% of posts are edited
                };

                posts.Add(post);
            }

            return posts;
        }

        private static List<Comment> CreateComments(List<User> users, List<Post> posts)
        {
            var comments = new List<Comment>();
            var random = new Random();
            var commentContent = new[]
            {
                "So cute!",
                "Adorable!",
                "This made my day!",
                "Love this!",
                "What a sweetheart!",
                "This is the content I'm here for!",
                "Beautiful picture!",
                "So precious!",
                "Aww, reminds me of my pet!",
                "Can't handle the cuteness!",
                "Best thing I've seen all day!",
                "That face! 😍",
                "My heart just melted!",
                "I wish I could like this twice!",
                "This brightened my day!",
                "Perfect!",
                "Thanks for sharing this!",
                "Made me smile!",
                "Too adorable for words!",
                "What a beauty!",
                "I'm in love!",
                "This is everything!",
                "Perfection!",
                "My pet does the same thing!",
                "Just too cute!",
                "What breed is this?",
                "How old?",
                "I need more pictures!",
                "Keep these coming!",
                "Your pet is the best!",
                "Goals!",
                "This is why I love this app!",
                "Can we set up a playdate?",
                "Looking good!",
                "Stunning!",
                "Gorgeous!",
                "What a character!",
                "That look!",
                "Pure joy!",
                "This is wholesome content!",
                "You're such a good pet parent!",
                "This deserves all the likes!",
                "Heart = melted!",
                "Stop it! Too cute!",
                "I can't even!",
                "Those eyes!",
                "Perfect timing on this photo!",
                "Frame-worthy!",
                "I'm obsessed!",
                "The cutest thing I've seen today!"
            };

            // Generate 5000 comments
            for (int i = 0; i < 5000; i++)
            {
                var user = users[random.Next(users.Count)];
                var post = posts[random.Next(posts.Count)];

                var comment = new Comment
                {
                    UserId = user.Id,
                    PostId = post.Id,
                    Content = commentContent[random.Next(commentContent.Length)],
                    CreatedAt = post.CreatedAt.AddHours(random.Next(1, 240)) // Comment made 1 to 10 days after post
                };

                comments.Add(comment);
            }

            return comments;
        }

        private static List<Like> CreateLikes(List<User> users, List<Post> posts)
        {
            var likes = new List<Like>();
            var random = new Random();
            var addedPairs = new HashSet<string>(); // To avoid duplicate likes

            // Generate 3000 likes
            for (int i = 0; i < 3000; i++)
            {
                var userId = random.Next(users.Count);
                var postId = random.Next(posts.Count);

                var pair = $"{userId}-{postId}";

                // Check if this pair already exists
                if (!addedPairs.Contains(pair))
                {
                    var post = posts[postId];

                    likes.Add(new Like
                    {
                        UserId = users[userId].Id,
                        PostId = post.Id,
                        CreatedAt = post.CreatedAt.AddHours(random.Next(1, 240)) // Like made 1 to 10 days after post
                    });

                    addedPairs.Add(pair);
                }
                else
                {
                    // If duplicate, try again
                    i--;
                }
            }

            return likes;
        }

        private static List<Message> CreateMessages(List<User> users)
        {
            var messages = new List<Message>();
            var random = new Random();
            var messageContent = new[]
            {
                "Hey, how are you?",
                "I saw your post about your pet. So cute!",
                "Would you be interested in setting up a playdate for our pets?",
                "Do you have any recommendations for a good vet in the area?",
                "What food brand do you use for your pet?",
                "I'm thinking of adopting a new pet. Any advice?",
                "Your pictures are amazing! What camera do you use?",
                "Have you tried that new pet store that opened downtown?",
                "Are you going to the pet adoption event this weekend?",
                "I love all your pet photos! Keep them coming!",
                "Do you know any good pet sitters in the area?",
                "What toys does your pet like the most?",
                "Have you taken any pet training classes you'd recommend?",
                "Just wanted to say hi and see how you and your pet are doing!",
                "Your pet is so well-behaved! How did you train them?",
                "I'm having trouble with my pet's behavior. Any suggestions?",
                "Have you tried that new pet-friendly café?",
                "Would you mind if I shared your post? It's too cute not to share!",
                "I'm looking for good hiking trails that are pet-friendly. Any ideas?",
                "What's your secret to taking such great pet photos?",
                "Do you make your own pet treats? I'd love the recipe!",
                "Have you ever used a pet sitting service? Which one?",
                "Your pet has the most adorable expressions!",
                "What's your favorite memory with your pet?",
                "Do you have any tips for traveling with pets?",
                "I'm new to pet ownership. Any beginner advice?",
                "What pet insurance do you use?",
                "Your pet looks so healthy! What's your care routine?",
                "Have you ever dealt with pet allergies? What helped?",
                "I'm looking for a good groomer. Any recommendations?"
            };

            // Generate 500 messages
            for (int i = 0; i < 500; i++)
            {
                var senderId = random.Next(users.Count);
                var receiverId = random.Next(users.Count);

                // Ensure sender and receiver are not the same person
                while (senderId == receiverId)
                {
                    receiverId = random.Next(users.Count);
                }

                var message = new Message
                {
                    SenderId = users[senderId].Id,
                    ReceiverId = users[receiverId].Id,
                    Content = messageContent[random.Next(messageContent.Length)],
                    IsRead = random.Next(2) == 0, // 50% chance of being read
                    SentAt = DateTime.Now.AddDays(-random.Next(1, 60))
                };

                messages.Add(message);
            }

            return messages;
        }
    }
}