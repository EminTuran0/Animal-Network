using AnimalNetwork.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    public class AnimalController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnimalController> _logger;
        private readonly string _connectionString;

        public AnimalController(IConfiguration configuration, ILogger<AnimalController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
     
                string sql = @"
                    SELECT a.*, b.*, at.*, u.*, l.*
                    FROM Animals a
                    LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                    LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                    LEFT JOIN Users u ON a.OwnerId = u.Id
                    LEFT JOIN Locations l ON a.LocationId = l.Id
                    ORDER BY a.RegisterDate DESC";

                var animalsDictionary = new Dictionary<int, Animal>();

                var animals = await db.QueryAsync<Animal, AnimalBreed, AnimalType, User, Location, Animal>(
                    sql,
                    (animal, breed, animalType, owner, location) => {
                        if (!animalsDictionary.TryGetValue(animal.Id, out var existingAnimal))
                        {
                            existingAnimal = animal;
                            existingAnimal.Breed = breed;
                            if (breed != null) breed.AnimalType = animalType;
                            existingAnimal.Owner = owner;
                            existingAnimal.Location = location;
                            existingAnimal.Followers = new List<AnimalFollow>();
                            animalsDictionary.Add(existingAnimal.Id, existingAnimal);
                        }
                        return existingAnimal;
                    },
                    splitOn: "Id,Id,Id,Id");

            
                string followersSql = @"
                    SELECT af.AnimalId, COUNT(*) as FollowerCount
                    FROM AnimalFollows af
                    GROUP BY af.AnimalId";

                var followerCounts = await db.QueryAsync<(int AnimalId, int FollowerCount)>(followersSql);

                foreach (var (animalId, count) in followerCounts)
                {
                    if (animalsDictionary.TryGetValue(animalId, out var animal))
                    {
                        for (int i = 0; i < count; i++)
                        {
             
                            animal.Followers.Add(new AnimalFollow { AnimalId = animalId });
                        }
                    }
                }

                return View(animalsDictionary.Values.ToList());
            }
        }

 
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
           
                string sql = @"
                    SELECT a.*, b.*, at.*, u.*, l.*
                    FROM Animals a
                    LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                    LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                    LEFT JOIN Users u ON a.OwnerId = u.Id
                    LEFT JOIN Locations l ON a.LocationId = l.Id
                    WHERE a.Id = @AnimalId";

                var animalData = await db.QueryAsync<Animal, AnimalBreed, AnimalType, User, Location, Animal>(
                    sql,
                    (animal, breed, animalType, owner, location) => {
                        animal.Breed = breed;
                        if (breed != null) breed.AnimalType = animalType;
                        animal.Owner = owner;
                        animal.Location = location;
                        return animal;
                    },
                    new { AnimalId = id },
                    splitOn: "Id,Id,Id,Id");

                var animal = animalData.FirstOrDefault();

                if (animal == null)
                {
                    return NotFound();
                }

            
                string followersSql = @"
                    SELECT * FROM AnimalFollows 
                    WHERE AnimalId = @AnimalId";

                var followers = await db.QueryAsync<AnimalFollow>(followersSql, new { AnimalId = id });
                animal.Followers = followers.ToList();

           
                if (User.Identity.IsAuthenticated)
                {
                    int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    string isFollowingSql = @"
                        SELECT COUNT(1) FROM AnimalFollows 
                        WHERE AnimalId = @AnimalId AND UserId = @UserId";

                    int followCount = await db.ExecuteScalarAsync<int>(isFollowingSql, new { AnimalId = id, UserId = userId });
                    ViewBag.IsFollowing = followCount > 0;
                }

   
                string postsSql = @"
                    SELECT p.*, u.*, 
                           (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount,
                           (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount
                    FROM Posts p
                    JOIN Users u ON p.UserId = u.Id
                    WHERE p.AnimalId = @AnimalId
                    ORDER BY p.CreatedAt DESC
                    OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

                var posts = await db.QueryAsync<dynamic>(postsSql, new { AnimalId = id });

                var recentPosts = posts.Select(p => {
                    var post = new Post
                    {
                        Id = p.Id,
                        Content = p.Content,
                        CreatedAt = p.CreatedAt,
                        ImageUrl = p.ImageUrl,
                        AnimalId = p.AnimalId,
                        UserId = p.UserId,
                        UpdatedAt = p.UpdatedAt,
                        User = new User
                        {
                            Id = p.Id,
                            Username = p.Username,
                            Email = p.Email,
                            ProfileImage = p.ProfileImage
                        },
                        Comments = new List<Comment>(),
                        Likes = new List<Like>()
                    };

                    for (int i = 0; i < (int)p.CommentCount; i++)
                        post.Comments.Add(new Comment());

                    for (int i = 0; i < (int)p.LikeCount; i++)
                        post.Likes.Add(new Like());

                    return post;
                }).ToList();

                ViewBag.RecentPosts = recentPosts;

                return View(animal);
            }
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            await LoadFormSelectLists();
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Animal animal)
        {
            if (ModelState.IsValid)
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    animal.OwnerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    animal.RegisterDate = DateTime.Now;

                    string sql = @"
                        INSERT INTO Animals (Name, BreedId, OwnerId, DateOfBirth, Gender, 
                                           Description, ProfileImage, LocationId, IsAdoptable, RegisterDate)
                        VALUES (@Name, @BreedId, @OwnerId, @DateOfBirth, @Gender, 
                               @Description, @ProfileImage, @LocationId, @IsAdoptable, @RegisterDate);
                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    animal.Id = await db.QuerySingleAsync<int>(sql, animal);

                    return RedirectToAction(nameof(Details), new { id = animal.Id });
                }
            }

            await LoadFormSelectLists();
            return View(animal);
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Animals WHERE Id = @Id";
                var animal = await db.QueryFirstOrDefaultAsync<Animal>(sql, new { Id = id });

                if (animal == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (animal.OwnerId != userId && !isAdmin)
                {
                    return Forbid();
                }

                await LoadFormSelectLists();
                return View(animal);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Animal animal)
        {
            if (id != animal.Id)
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Animals WHERE Id = @Id";
                var originalAnimal = await db.QueryFirstOrDefaultAsync<Animal>(checkSql, new { Id = id });

                if (originalAnimal == null)
                {
                    return NotFound();
                }
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (originalAnimal.OwnerId != userId && !isAdmin)
                {
                    return Forbid();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        animal.OwnerId = originalAnimal.OwnerId;
                        animal.RegisterDate = originalAnimal.RegisterDate;

                        string updateSql = @"
                            UPDATE Animals 
                            SET Name = @Name, 
                                BreedId = @BreedId, 
                                DateOfBirth = @DateOfBirth, 
                                Gender = @Gender, 
                                Description = @Description, 
                                ProfileImage = @ProfileImage, 
                                LocationId = @LocationId, 
                                IsAdoptable = @IsAdoptable
                            WHERE Id = @Id";

                        await db.ExecuteAsync(updateSql, animal);

                        return RedirectToAction(nameof(Details), new { id = animal.Id });
                    }
                    catch (Exception)
                    {
                        if (!await AnimalExists(animal.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            await LoadFormSelectLists();
            return View(animal);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT COUNT(1) FROM Animals WHERE Id = @Id";
                int animalCount = await db.ExecuteScalarAsync<int>(checkSql, new { Id = id });

                if (animalCount == 0)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string followCheckSql = @"
                    SELECT COUNT(1) FROM AnimalFollows 
                    WHERE AnimalId = @AnimalId AND UserId = @UserId";

                int followCount = await db.ExecuteScalarAsync<int>(
                    followCheckSql, new { AnimalId = id, UserId = userId });

                if (followCount == 0)
                {
                    string insertSql = @"
                        INSERT INTO AnimalFollows (AnimalId, UserId, CreatedAt)
                        VALUES (@AnimalId, @UserId, @CreatedAt)";

                    await db.ExecuteAsync(insertSql, new
                    {
                        AnimalId = id,
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    });
                }

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string deleteSql = @"
                    DELETE FROM AnimalFollows 
                    WHERE AnimalId = @AnimalId AND UserId = @UserId";

                await db.ExecuteAsync(deleteSql, new { AnimalId = id, UserId = userId });

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> Search(string search, int? typeId, int? breedId, int? locationId, bool? adoptable)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                var conditions = new List<string>();

                string baseSql = @"
                    SELECT a.*, b.*, at.*, u.*, l.*
                    FROM Animals a
                    LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                    LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                    LEFT JOIN Users u ON a.OwnerId = u.Id
                    LEFT JOIN Locations l ON a.LocationId = l.Id";

                if (!string.IsNullOrEmpty(search))
                {
                    conditions.Add("(a.Name LIKE @Search OR a.Description LIKE @Search)");
                    parameters.Add("Search", $"%{search}%");
                }

                if (typeId.HasValue)
                {
                    conditions.Add("b.AnimalTypeId = @TypeId");
                    parameters.Add("TypeId", typeId.Value);
                }

                if (breedId.HasValue)
                {
                    conditions.Add("a.BreedId = @BreedId");
                    parameters.Add("BreedId", breedId.Value);
                }

                if (locationId.HasValue)
                {
                    conditions.Add("a.LocationId = @LocationId");
                    parameters.Add("LocationId", locationId.Value);
                }

                if (adoptable.HasValue)
                {
                    conditions.Add("a.IsAdoptable = @IsAdoptable");
                    parameters.Add("IsAdoptable", adoptable.Value);
                }

                if (conditions.Any())
                {
                    baseSql += " WHERE " + string.Join(" AND ", conditions);
                }

                baseSql += " ORDER BY a.RegisterDate DESC";

                var animalsDictionary = new Dictionary<int, Animal>();

                var animals = await db.QueryAsync<Animal, AnimalBreed, AnimalType, User, Location, Animal>(
                    baseSql,
                    (animal, breed, animalType, owner, location) => {
                        if (!animalsDictionary.TryGetValue(animal.Id, out var existingAnimal))
                        {
                            existingAnimal = animal;
                            existingAnimal.Breed = breed;
                            if (breed != null) breed.AnimalType = animalType;
                            existingAnimal.Owner = owner;
                            existingAnimal.Location = location;
                            existingAnimal.Followers = new List<AnimalFollow>();
                            animalsDictionary.Add(existingAnimal.Id, existingAnimal);
                        }
                        return existingAnimal;
                    },
                    parameters,
                    splitOn: "Id,Id,Id,Id");

                string followersSql = @"
                    SELECT af.AnimalId, COUNT(*) as FollowerCount
                    FROM AnimalFollows af
                    GROUP BY af.AnimalId";

                var followerCounts = await db.QueryAsync<(int AnimalId, int FollowerCount)>(followersSql);

                foreach (var (animalId, count) in followerCounts)
                {
                    if (animalsDictionary.TryGetValue(animalId, out var animal))
                    {
                        for (int i = 0; i < count; i++)
                        {
                            animal.Followers.Add(new AnimalFollow { AnimalId = animalId });
                        }
                    }
                }

                await LoadFormSelectLists();
                return View("Index", animalsDictionary.Values.ToList());
            }
        }

        private async Task<bool> AnimalExists(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "SELECT COUNT(1) FROM Animals WHERE Id = @Id";
                int count = await db.ExecuteScalarAsync<int>(sql, new { Id = id });
                return count > 0;
            }
        }

        private async Task LoadFormSelectLists()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string typesSql = "SELECT * FROM AnimalTypes ORDER BY Name";
                var animalTypes = await db.QueryAsync<AnimalType>(typesSql);
                ViewBag.AnimalTypes = animalTypes;

                string breedsSql = @"
                    SELECT ab.*, at.*
                    FROM AnimalBreeds ab
                    JOIN AnimalTypes at ON ab.AnimalTypeId = at.Id
                    ORDER BY ab.Name";

                var breedsDictionary = new Dictionary<int, AnimalBreed>();

                var breeds = await db.QueryAsync<AnimalBreed, AnimalType, AnimalBreed>(
                    breedsSql,
                    (breed, animalType) => {
                        if (!breedsDictionary.TryGetValue(breed.Id, out var existingBreed))
                        {
                            existingBreed = breed;
                            existingBreed.AnimalType = animalType;
                            breedsDictionary.Add(existingBreed.Id, existingBreed);
                        }
                        return existingBreed;
                    },
                    splitOn: "Id");

                ViewBag.AnimalBreeds = breedsDictionary.Values.ToList();

                string locationsSql = @"
                    SELECT * FROM Locations 
                    ORDER BY Country, City";

                var locations = await db.QueryAsync<Location>(locationsSql);
                ViewBag.Locations = locations;
            }
        }
    }
}