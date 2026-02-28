using AnimalNetwork.Models.Entities;
using AnimalNetwork.Models.ViewModels;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly string _connectionString;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Feed", "Post");
                }

                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    var popularAnimals = new List<Animal>();
                    var upcomingEvents = new List<Event>();

                    string animalsSql = @"
                       SELECT TOP 6 a.*, b.*, at.*, u.*, 
                                  (SELECT COUNT(*) FROM AnimalFollows af WHERE af.AnimalId = a.Id) AS FollowerCount
                       FROM Animals a
                       JOIN AnimalBreeds b ON a.BreedId = b.Id
                       JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                       JOIN Users u ON a.OwnerId = u.Id
                       ORDER BY FollowerCount DESC";

                    var animalDict = new Dictionary<int, Animal>();

                    await db.QueryAsync<Animal, AnimalBreed, AnimalType, User, Animal>(
                        animalsSql,
                        (animal, breed, animalType, owner) => {
                            if (!animalDict.TryGetValue(animal.Id, out var existingAnimal))
                            {
                                existingAnimal = animal;
                                existingAnimal.Breed = breed;
                                if (breed != null) breed.AnimalType = animalType;
                                existingAnimal.Owner = owner;
                                existingAnimal.Followers = new List<AnimalFollow>();
                                animalDict.Add(existingAnimal.Id, existingAnimal);
                            }
                            return existingAnimal;
                        },
                        splitOn: "Id,Id,Id");

                    popularAnimals = animalDict.Values.ToList();

                    string followersSql = @"
                       SELECT af.AnimalId, COUNT(*) as FollowerCount
                       FROM AnimalFollows af
                       WHERE af.AnimalId IN @AnimalIds
                       GROUP BY af.AnimalId";

                    var animalIds = popularAnimals.Select(a => a.Id).ToArray();
                    var followerCounts = await db.QueryAsync<(int AnimalId, int FollowerCount)>(
                        followersSql, new { AnimalIds = animalIds });

                    foreach (var (animalId, count) in followerCounts)
                    {
                        if (animalDict.TryGetValue(animalId, out var animal))
                        {
                            for (int i = 0; i < count; i++)
                            {
                                animal.Followers.Add(new AnimalFollow { AnimalId = animalId });
                            }
                        }
                    }

                    string eventsSql = @"
                       SELECT TOP 3 e.*, l.*, u.*
                       FROM Events e
                       JOIN Users u ON e.CreatorId = u.Id
                       LEFT JOIN Locations l ON e.LocationId = l.Id
                       WHERE e.StartTime > @CurrentTime
                       ORDER BY e.StartTime";

                    var eventsDictionary = new Dictionary<int, Event>();

                    await db.QueryAsync<Event, Location, User, Event>(
                        eventsSql,
                        (evt, location, creator) => {
                            if (!eventsDictionary.TryGetValue(evt.Id, out var existingEvent))
                            {
                                existingEvent = evt;
                                existingEvent.Location = location;
                                existingEvent.Creator = creator;
                                eventsDictionary.Add(existingEvent.Id, existingEvent);
                            }
                            return existingEvent;
                        },
                        new { CurrentTime = DateTime.Now },
                        splitOn: "Id,Id");

                    upcomingEvents = eventsDictionary.Values.ToList();

                    ViewBag.PopularPosts = new List<Post>();
                    ViewBag.PopularAnimals = popularAnimals;
                    ViewBag.UpcomingEvents = upcomingEvents;

                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                ViewBag.PopularPosts = new List<Post>();
                ViewBag.PopularAnimals = new List<Animal>();
                ViewBag.UpcomingEvents = new List<Event>();

                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return View(new
                {
                    Users = new List<User>(),
                    Animals = new List<Animal>(),
                    Posts = new List<Post>()
                });
            }

            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    var searchParam = $"%{query}%";

                    string usersSql = @"
                       SELECT TOP 5 * FROM Users 
                       WHERE Username LIKE @Query 
                          OR (FirstName IS NOT NULL AND FirstName LIKE @Query) 
                          OR (LastName IS NOT NULL AND LastName LIKE @Query)";

                    var users = await db.QueryAsync<User>(usersSql, new { Query = searchParam });

                    string animalsSql = @"
                       SELECT TOP 5 a.*, b.*, at.*, u.*
                       FROM Animals a
                       LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                       LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                       LEFT JOIN Users u ON a.OwnerId = u.Id
                       WHERE a.Name LIKE @Query 
                          OR a.Description LIKE @Query
                          OR (b.Name LIKE @Query)
                          OR (at.Name LIKE @Query)";

                    var animalDict = new Dictionary<int, Animal>();

                    await db.QueryAsync<Animal, AnimalBreed, AnimalType, User, Animal>(
                        animalsSql,
                        (animal, breed, animalType, owner) => {
                            if (!animalDict.TryGetValue(animal.Id, out var existingAnimal))
                            {
                                existingAnimal = animal;
                                existingAnimal.Breed = breed;
                                if (breed != null) breed.AnimalType = animalType;
                                existingAnimal.Owner = owner;
                                animalDict.Add(existingAnimal.Id, existingAnimal);
                            }
                            return existingAnimal;
                        },
                        new { Query = searchParam },
                        splitOn: "Id,Id,Id");

                    var animals = animalDict.Values.ToList();

                    string postsSql = @"
                       SELECT TOP 5 p.*, u.*, a.*
                       FROM Posts p
                       JOIN Users u ON p.UserId = u.Id
                       LEFT JOIN Animals a ON p.AnimalId = a.Id
                       WHERE p.Content LIKE @Query
                       ORDER BY p.CreatedAt DESC";

                    var postDict = new Dictionary<int, Post>();

                    await db.QueryAsync<Post, User, Animal, Post>(
                        postsSql,
                        (post, user, animal) => {
                            if (!postDict.TryGetValue(post.Id, out var existingPost))
                            {
                                existingPost = post;
                                existingPost.User = user;
                                existingPost.Animal = animal;
                                postDict.Add(existingPost.Id, existingPost);
                            }
                            return existingPost;
                        },
                        new { Query = searchParam },
                        splitOn: "Id,Id");

                    var posts = postDict.Values.ToList();

                    ViewBag.Query = query;
                    return View(new
                    {
                        Users = users,
                        Animals = animals,
                        Posts = posts
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in search");
                ViewBag.Error = "An error occurred while searching. Please try again.";
                ViewBag.Query = query;

                return View(new
                {
                    Users = new List<User>(),
                    Animals = new List<Animal>(),
                    Posts = new List<Post>()
                });
            }
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }
    }
}