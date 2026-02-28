using AnimalNetwork.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProfileController> _logger;
        private readonly string _connectionString;

        public ProfileController(IConfiguration configuration, ILogger<ProfileController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                User user = null;


                if (int.TryParse(id, out int userId))
                {
                    user = await GetUserWithDetails(db, userId);
                }
                else
                {
                    string usernameSql = "SELECT * FROM Users WHERE Username = @Username";
                    user = await db.QueryFirstOrDefaultAsync<User>(usernameSql, new { Username = id });

                    if (user != null)
                    {
                        user = await GetUserWithDetails(db, user.Id);
                    }
                }

                if (user == null)
                {
                    return NotFound();
                }

                if (User.Identity.IsAuthenticated)
                {
                    int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                    string followingSql = @"
                       SELECT COUNT(1) FROM Follows 
                       WHERE FollowerId = @FollowerId AND FollowedId = @FollowedId";

                    int followCount = await db.ExecuteScalarAsync<int>(followingSql,
                        new { FollowerId = currentUserId, FollowedId = user.Id });

                    ViewBag.IsFollowing = followCount > 0;
                    ViewBag.IsOwnProfile = currentUserId == user.Id;
                }

                string recentPostsSql = @"
                   SELECT p.*, a.*, 
                         (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount,
                         (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount
                   FROM Posts p
                   LEFT JOIN Animals a ON p.AnimalId = a.Id
                   WHERE p.UserId = @UserId
                   ORDER BY p.CreatedAt DESC
                   OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

                var posts = await db.QueryAsync<dynamic>(recentPostsSql, new { UserId = user.Id });

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
                        Animal = p.Id != null ? new Animal
                        {
                            Id = p.Id,
                            Name = p.Name
                        } : null,
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

                return View(user);
            }
        }

        [Authorize]
        public async Task<IActionResult> Edit()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = @"
                   SELECT u.*, l.* 
                   FROM Users u
                   LEFT JOIN Locations l ON u.LocationId = l.Id
                   WHERE u.Id = @UserId";

                var result = await db.QueryAsync<User, Location, User>(
                    userSql,
                    (user, location) => {
                        user.Location = location;
                        return user;
                    },
                    new { UserId = userId },
                    splitOn: "Id");

                var user = result.FirstOrDefault();

                if (user == null)
                {
                    return NotFound();
                }

                string locationsSql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(locationsSql);
                ViewBag.Locations = locations;

                return View(user);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (userId != user.Id)
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Users WHERE Id = @Id";
                var originalUser = await db.QueryFirstOrDefaultAsync<User>(checkSql, new { Id = userId });

                if (originalUser == null)
                {
                    return NotFound();
                }

                ModelState.Remove("Username");
                ModelState.Remove("Email");
                ModelState.Remove("PasswordHash");

                if (ModelState.IsValid)
                {
                    try
                    {
                        string updateSql = @"
                           UPDATE Users 
                           SET FirstName = @FirstName, 
                               LastName = @LastName, 
                               Bio = @Bio, 
                               ProfileImage = @ProfileImage, 
                               LocationId = @LocationId
                           WHERE Id = @Id";

                        await db.ExecuteAsync(updateSql, new
                        {
                            Id = userId,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Bio = user.Bio,
                            ProfileImage = user.ProfileImage,
                            LocationId = user.LocationId
                        });

                        return RedirectToAction(nameof(Details), new { id = userId });
                    }
                    catch (Exception)
                    {
                        if (!await UserExists(db, user.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                string locationsSql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(locationsSql);
                ViewBag.Locations = locations;

                return View(user);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = "SELECT COUNT(1) FROM Users WHERE Id = @Id";
                int userExists = await db.ExecuteScalarAsync<int>(userSql, new { Id = id });

                if (userExists == 0)
                {
                    return NotFound();
                }

                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (currentUserId == id)
                {
                    return BadRequest();
                }

                string checkSql = @"
                   SELECT COUNT(1) FROM Follows 
                   WHERE FollowerId = @FollowerId AND FollowedId = @FollowedId";

                int followExists = await db.ExecuteScalarAsync<int>(checkSql,
                    new { FollowerId = currentUserId, FollowedId = id });

                if (followExists == 0)
                {
                    string insertSql = @"
                       INSERT INTO Follows (FollowerId, FollowedId, CreatedAt)
                       VALUES (@FollowerId, @FollowedId, @CreatedAt)";

                    await db.ExecuteAsync(insertSql, new
                    {
                        FollowerId = currentUserId,
                        FollowedId = id,
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
                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string deleteSql = @"
                   DELETE FROM Follows 
                   WHERE FollowerId = @FollowerId AND FollowedId = @FollowedId";

                await db.ExecuteAsync(deleteSql,
                    new { FollowerId = currentUserId, FollowedId = id });

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [Authorize]
        public async Task<IActionResult> MyAnimals()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = @"
                   SELECT a.*, b.*, at.*, l.*,
                          (SELECT COUNT(*) FROM AnimalFollows af WHERE af.AnimalId = a.Id) AS FollowerCount
                   FROM Animals a
                   LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                   LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                   LEFT JOIN Locations l ON a.LocationId = l.Id
                   WHERE a.OwnerId = @OwnerId
                   ORDER BY a.RegisterDate DESC";

                var animalsDictionary = new Dictionary<int, Animal>();

                var animals = await db.QueryAsync<Animal, AnimalBreed, AnimalType, Location, Animal>(
                    sql,
                    (animal, breed, animalType, location) => {
                        if (!animalsDictionary.TryGetValue(animal.Id, out var existingAnimal))
                        {
                            existingAnimal = animal;
                            existingAnimal.Breed = breed;
                            if (breed != null) breed.AnimalType = animalType;
                            existingAnimal.Location = location;
                            existingAnimal.Followers = new List<AnimalFollow>();
                            animalsDictionary.Add(existingAnimal.Id, existingAnimal);
                        }
                        return existingAnimal;
                    },
                    new { OwnerId = userId },
                    splitOn: "Id,Id,Id");

                return View(animalsDictionary.Values.ToList());
            }
        }

        [Authorize]
        public async Task<IActionResult> Followers(int? id)
        {
            int userId = id ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = "SELECT * FROM Users WHERE Id = @Id";
                var user = await db.QueryFirstOrDefaultAsync<User>(userSql, new { Id = userId });

                if (user == null)
                {
                    return NotFound();
                }

                string followersSql = @"
                   SELECT u.* 
                   FROM Follows f
                   JOIN Users u ON f.FollowerId = u.Id
                   WHERE f.FollowedId = @UserId";

                var followers = await db.QueryAsync<User>(followersSql, new { UserId = userId });

                ViewBag.User = user;
                return View(followers.ToList());
            }
        }

        [Authorize]
        public async Task<IActionResult> Following(int? id)
        {
            int userId = id ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = "SELECT * FROM Users WHERE Id = @Id";
                var user = await db.QueryFirstOrDefaultAsync<User>(userSql, new { Id = userId });

                if (user == null)
                {
                    return NotFound();
                }

                string followingSql = @"
                   SELECT u.* 
                   FROM Follows f
                   JOIN Users u ON f.FollowedId = u.Id
                   WHERE f.FollowerId = @UserId";

                var following = await db.QueryAsync<User>(followingSql, new { UserId = userId });

                ViewBag.User = user;
                return View(following.ToList());
            }
        }

        public async Task<IActionResult> Search(string search)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                if (string.IsNullOrEmpty(search))
                {
                    return View(new List<User>());
                }

                string sql = @"
                   SELECT TOP 50 u.*, l.* 
                   FROM Users u
                   LEFT JOIN Locations l ON u.LocationId = l.Id
                   WHERE u.Username LIKE @Search 
                      OR u.FirstName LIKE @Search 
                      OR u.LastName LIKE @Search
                   ORDER BY u.Username";

                var usersDictionary = new Dictionary<int, User>();

                var users = await db.QueryAsync<User, Location, User>(
                    sql,
                    (user, location) => {
                        if (!usersDictionary.TryGetValue(user.Id, out var existingUser))
                        {
                            existingUser = user;
                            existingUser.Location = location;
                            usersDictionary.Add(existingUser.Id, existingUser);
                        }
                        return existingUser;
                    },
                    new { Search = $"%{search}%" },
                    splitOn: "Id");

                return View(usersDictionary.Values.ToList());
            }
        }

        private async Task<User> GetUserWithDetails(IDbConnection db, int userId)
        {
            string userSql = @"
               SELECT u.*, l.* 
               FROM Users u
               LEFT JOIN Locations l ON u.LocationId = l.Id
               WHERE u.Id = @UserId";

            var userResult = await db.QueryAsync<User, Location, User>(
                userSql,
                (user, location) => {
                    user.Location = location;
                    return user;
                },
                new { UserId = userId },
                splitOn: "Id");

            var user = userResult.FirstOrDefault();

            if (user == null)
            {
                return null;
            }

            string animalsSql = @"
               SELECT a.*, b.*, at.*
               FROM Animals a
               LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
               LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
               WHERE a.OwnerId = @UserId";

            var animalsDictionary = new Dictionary<int, Animal>();

            await db.QueryAsync<Animal, AnimalBreed, AnimalType, Animal>(
                animalsSql,
                (animal, breed, animalType) => {
                    if (!animalsDictionary.TryGetValue(animal.Id, out var existingAnimal))
                    {
                        existingAnimal = animal;
                        existingAnimal.Breed = breed;
                        if (breed != null) breed.AnimalType = animalType;
                        animalsDictionary.Add(existingAnimal.Id, existingAnimal);
                    }
                    return existingAnimal;
                },
                new { UserId = userId },
                splitOn: "Id,Id");

            user.Animals = animalsDictionary.Values.ToList();

            string followersSql = @"
               SELECT f.*, u.*
               FROM Follows f
               JOIN Users u ON f.FollowerId = u.Id
               WHERE f.FollowedId = @UserId";

            var followersDictionary = new Dictionary<int, Follow>();

            await db.QueryAsync<Follow, User, Follow>(
                followersSql,
                (follow, follower) => {
                    if (!followersDictionary.TryGetValue(follow.Id, out var existingFollow))
                    {
                        existingFollow = follow;
                        existingFollow.Follower = follower;
                        followersDictionary.Add(existingFollow.Id, existingFollow);
                    }
                    return existingFollow;
                },
                new { UserId = userId },
                splitOn: "Id");

            user.Followers = followersDictionary.Values.ToList();

            string followingSql = @"
               SELECT f.*, u.*
               FROM Follows f
               JOIN Users u ON f.FollowedId = u.Id
               WHERE f.FollowerId = @UserId";

            var followingDictionary = new Dictionary<int, Follow>();

            await db.QueryAsync<Follow, User, Follow>(
                followingSql,
                (follow, followed) => {
                    if (!followingDictionary.TryGetValue(follow.Id, out var existingFollow))
                    {
                        existingFollow = follow;
                        existingFollow.Followed = followed;
                        followingDictionary.Add(existingFollow.Id, existingFollow);
                    }
                    return existingFollow;
                },
                new { UserId = userId },
                splitOn: "Id");

            user.Following = followingDictionary.Values.ToList();

            string animalFollowsSql = @"
               SELECT af.*, a.*
               FROM AnimalFollows af
               JOIN Animals a ON af.AnimalId = a.Id
               WHERE af.UserId = @UserId";

            var animalFollowsDictionary = new Dictionary<int, AnimalFollow>();

            await db.QueryAsync<AnimalFollow, Animal, AnimalFollow>(
                animalFollowsSql,
                (animalFollow, animal) => {
                    if (!animalFollowsDictionary.TryGetValue(animalFollow.Id, out var existingFollow))
                    {
                        existingFollow = animalFollow;
                        existingFollow.Animal = animal;
                        animalFollowsDictionary.Add(existingFollow.Id, existingFollow);
                    }
                    return existingFollow;
                },
                new { UserId = userId },
                splitOn: "Id");

            user.AnimalFollows = animalFollowsDictionary.Values.ToList();

            return user;
        }

        private async Task<bool> UserExists(IDbConnection db, int id)
        {
            string sql = "SELECT COUNT(1) FROM Users WHERE Id = @Id";
            int count = await db.ExecuteScalarAsync<int>(sql, new { Id = id });
            return count > 0;
        }
    }
}