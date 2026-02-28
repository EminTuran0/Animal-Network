using AnimalNetwork.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    public class PostController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PostController> _logger;
        private readonly string _connectionString;

        public PostController(IConfiguration configuration, ILogger<PostController> logger)
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
                   SELECT TOP 50 p.*, u.*, a.*, b.*, at.*,
                          (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount,
                          (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount
                   FROM Posts p
                   JOIN Users u ON p.UserId = u.Id
                   LEFT JOIN Animals a ON p.AnimalId = a.Id
                   LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                   LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                   ORDER BY p.CreatedAt DESC";

                var postsDictionary = new Dictionary<int, Post>();

                await db.QueryAsync<Post, User, Animal, AnimalBreed, AnimalType, Post>(
                    sql,
                    (post, user, animal, breed, animalType) => {
                        if (!postsDictionary.TryGetValue(post.Id, out var existingPost))
                        {
                            existingPost = post;
                            existingPost.User = user;
                            existingPost.Comments = new List<Comment>();
                            existingPost.Likes = new List<Like>();

                            if (animal != null)
                            {
                                existingPost.Animal = animal;
                                animal.Breed = breed;
                                if (breed != null) breed.AnimalType = animalType;
                            }

                            postsDictionary.Add(existingPost.Id, existingPost);
                        }
                        return existingPost;
                    },
                    splitOn: "Id,Id,Id,Id");

                string commentsSql = @"
                   SELECT c.*, u.*
                   FROM Comments c
                   JOIN Users u ON c.UserId = u.Id
                   WHERE c.PostId IN @PostIds";

                var postIds = postsDictionary.Keys.ToArray();
                var comments = await db.QueryAsync<Comment, User, Comment>(
                    commentsSql,
                    (comment, user) => {
                        comment.User = user;
                        return comment;
                    },
                    new { PostIds = postIds },
                    splitOn: "Id");

                foreach (var comment in comments)
                {
                    if (postsDictionary.TryGetValue(comment.PostId, out var post))
                    {
                        post.Comments.Add(comment);
                    }
                }

                string likesSql = "SELECT * FROM Likes WHERE PostId IN @PostIds";
                var likes = await db.QueryAsync<Like>(likesSql, new { PostIds = postIds });

                foreach (var like in likes)
                {
                    if (postsDictionary.TryGetValue(like.PostId, out var post))
                    {
                        post.Likes.Add(like);
                    }
                }

                return View(postsDictionary.Values.ToList());
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
                   SELECT p.*, u.*, a.*, b.*, at.*
                   FROM Posts p
                   JOIN Users u ON p.UserId = u.Id
                   LEFT JOIN Animals a ON p.AnimalId = a.Id
                   LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                   LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                   WHERE p.Id = @PostId";

                var postData = await db.QueryAsync<Post, User, Animal, AnimalBreed, AnimalType, Post>(
                    sql,
                    (post, user, animal, breed, animalType) => {
                        post.User = user;
                        post.Comments = new List<Comment>();
                        post.Likes = new List<Like>();

                        if (animal != null)
                        {
                            post.Animal = animal;
                            animal.Breed = breed;
                            if (breed != null) breed.AnimalType = animalType;
                        }

                        return post;
                    },
                    new { PostId = id },
                    splitOn: "Id,Id,Id,Id");

                var post = postData.FirstOrDefault();

                if (post == null)
                {
                    return NotFound();
                }

                string commentsSql = @"
                   SELECT c.*, u.*
                   FROM Comments c
                   JOIN Users u ON c.UserId = u.Id
                   WHERE c.PostId = @PostId
                   ORDER BY c.CreatedAt";

                var comments = await db.QueryAsync<Comment, User, Comment>(
                    commentsSql,
                    (comment, user) => {
                        comment.User = user;
                        return comment;
                    },
                    new { PostId = id },
                    splitOn: "Id");

                post.Comments = comments.ToList();

                string likesSql = "SELECT * FROM Likes WHERE PostId = @PostId";
                var likes = await db.QueryAsync<Like>(likesSql, new { PostId = id });
                post.Likes = likes.ToList();

                if (User.Identity.IsAuthenticated)
                {
                    int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    string hasLikedSql = @"
                       SELECT COUNT(1) FROM Likes 
                       WHERE PostId = @PostId AND UserId = @UserId";

                    int likeCount = await db.ExecuteScalarAsync<int>(
                        hasLikedSql, new { PostId = id, UserId = userId });

                    ViewBag.HasLiked = likeCount > 0;
                }

                return View(post);
            }
        }

        [Authorize]
        public async Task<IActionResult> Create(int? animalId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                if (animalId.HasValue)
                {
                    string animalSql = @"
                       SELECT a.*, u.*
                       FROM Animals a
                       JOIN Users u ON a.OwnerId = u.Id
                       WHERE a.Id = @AnimalId";

                    var animalData = await db.QueryAsync<Animal, User, Animal>(
                        animalSql,
                        (animal, owner) => {
                            animal.Owner = owner;
                            return animal;
                        },
                        new { AnimalId = animalId },
                        splitOn: "Id");

                    var animal = animalData.FirstOrDefault();

                    if (animal == null)
                    {
                        return NotFound();
                    }

                    int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (animal.OwnerId != userId)
                    {
                        return Forbid();
                    }

                    ViewBag.SelectedAnimal = animal;
                }

                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                string userAnimalsSql = "SELECT * FROM Animals WHERE OwnerId = @OwnerId";
                var userAnimals = await db.QueryAsync<Animal>(userAnimalsSql, new { OwnerId = currentUserId });

                ViewBag.UserAnimals = userAnimals;

                return View();
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post)
        {
            if (ModelState.IsValid)
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    post.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    post.CreatedAt = DateTime.Now;

                    if (post.AnimalId.HasValue)
                    {
                        string animalCheckSql = @"
                           SELECT COUNT(1) FROM Animals 
                           WHERE Id = @AnimalId AND OwnerId = @OwnerId";

                        int validAnimal = await db.ExecuteScalarAsync<int>(
                            animalCheckSql,
                            new { AnimalId = post.AnimalId, OwnerId = post.UserId });

                        if (validAnimal == 0)
                        {
                            ModelState.AddModelError("AnimalId", "You can only post about animals you own");

                            string userAnimalsSql = "SELECT * FROM Animals WHERE OwnerId = @OwnerId";
                            var userAnimals = await db.QueryAsync<Animal>(
                                userAnimalsSql, new { OwnerId = post.UserId });

                            ViewBag.UserAnimals = userAnimals;
                            return View(post);
                        }
                    }

                    string insertSql = @"
                       INSERT INTO Posts (UserId, AnimalId, Content, ImageUrl, CreatedAt)
                       VALUES (@UserId, @AnimalId, @Content, @ImageUrl, @CreatedAt);
                       SELECT CAST(SCOPE_IDENTITY() as int)";

                    post.Id = await db.QuerySingleAsync<int>(insertSql, post);

                    return RedirectToAction(nameof(Details), new { id = post.Id });
                }
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                string userAnimalsSql = "SELECT * FROM Animals WHERE OwnerId = @OwnerId";
                var userAnimals = await db.QueryAsync<Animal>(userAnimalsSql, new { OwnerId = currentUserId });

                ViewBag.UserAnimals = userAnimals;
            }

            return View(post);
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
                string postSql = @"
                   SELECT p.*, a.*
                   FROM Posts p
                   LEFT JOIN Animals a ON p.AnimalId = a.Id
                   WHERE p.Id = @PostId";

                var postData = await db.QueryAsync<Post, Animal, Post>(
                    postSql,
                    (post, animal) => {
                        post.Animal = animal;
                        return post;
                    },
                    new { PostId = id },
                    splitOn: "Id");

                var post = postData.FirstOrDefault();

                if (post == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (post.UserId != userId && !isAdmin)
                {
                    return Forbid();
                }

                string userAnimalsSql = "SELECT * FROM Animals WHERE OwnerId = @OwnerId";
                var userAnimals = await db.QueryAsync<Animal>(userAnimalsSql, new { OwnerId = userId });

                ViewBag.UserAnimals = userAnimals;
                return View(post);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Posts WHERE Id = @Id";
                var originalPost = await db.QueryFirstOrDefaultAsync<Post>(checkSql, new { Id = id });

                if (originalPost == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (originalPost.UserId != userId && !isAdmin)
                {
                    return Forbid();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        post.UserId = originalPost.UserId;
                        post.CreatedAt = originalPost.CreatedAt;
                        post.UpdatedAt = DateTime.Now;

                        string updateSql = @"
                           UPDATE Posts 
                           SET Content = @Content, 
                               ImageUrl = @ImageUrl, 
                               AnimalId = @AnimalId, 
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id";

                        await db.ExecuteAsync(updateSql, post);

                        return RedirectToAction(nameof(Details), new { id = post.Id });
                    }
                    catch (Exception)
                    {
                        if (!await PostExists(db, post.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                string userAnimalsSql = "SELECT * FROM Animals WHERE OwnerId = @OwnerId";
                var userAnimals = await db.QueryAsync<Animal>(userAnimalsSql, new { OwnerId = userId });

                ViewBag.UserAnimals = userAnimals;
                return View(post);
            }
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Posts WHERE Id = @Id";
                var post = await db.QueryFirstOrDefaultAsync<Post>(checkSql, new { Id = id });

                if (post == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (post.UserId != userId && !isAdmin)
                {
                    return Forbid();
                }

                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        string deleteCommentsSql = "DELETE FROM Comments WHERE PostId = @Id";
                        await db.ExecuteAsync(deleteCommentsSql, new { Id = id }, transaction);

                        string deleteLikesSql = "DELETE FROM Likes WHERE PostId = @Id";
                        await db.ExecuteAsync(deleteLikesSql, new { Id = id }, transaction);

                        string deletePostSql = "DELETE FROM Posts WHERE Id = @Id";
                        await db.ExecuteAsync(deletePostSql, new { Id = id }, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT COUNT(1) FROM Posts WHERE Id = @Id";
                int postExists = await db.ExecuteScalarAsync<int>(checkSql, new { Id = id });

                if (postExists == 0)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string likeCheckSql = @"
                   SELECT COUNT(1) FROM Likes 
                   WHERE PostId = @PostId AND UserId = @UserId";

                int likeExists = await db.ExecuteScalarAsync<int>(
                    likeCheckSql, new { PostId = id, UserId = userId });

                if (likeExists == 0)
                {
                    string insertSql = @"
                       INSERT INTO Likes (PostId, UserId, CreatedAt)
                       VALUES (@PostId, @UserId, @CreatedAt)";

                    await db.ExecuteAsync(insertSql, new
                    {
                        PostId = id,
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
        public async Task<IActionResult> Unlike(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string deleteSql = @"
                   DELETE FROM Likes 
                   WHERE PostId = @PostId AND UserId = @UserId";

                await db.ExecuteAsync(deleteSql, new { PostId = id, UserId = userId });

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(nameof(Details), new { id = postId });
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT COUNT(1) FROM Posts WHERE Id = @Id";
                int postExists = await db.ExecuteScalarAsync<int>(checkSql, new { Id = postId });

                if (postExists == 0)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string insertSql = @"
                   INSERT INTO Comments (PostId, UserId, Content, CreatedAt)
                   VALUES (@PostId, @UserId, @Content, @CreatedAt)";

                await db.ExecuteAsync(insertSql, new
                {
                    PostId = postId,
                    UserId = userId,
                    Content = content,
                    CreatedAt = DateTime.Now
                });

                return RedirectToAction(nameof(Details), new { id = postId });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id, int postId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Comments WHERE Id = @Id";
                var comment = await db.QueryFirstOrDefaultAsync<Comment>(checkSql, new { Id = id });

                if (comment == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (comment.UserId != userId && !isAdmin)
                {
                    return Forbid();
                }

                string deleteSql = "DELETE FROM Comments WHERE Id = @Id";
                await db.ExecuteAsync(deleteSql, new { Id = id });

                return RedirectToAction(nameof(Details), new { id = postId });
            }
        }

        [Authorize]
        public async Task<IActionResult> Feed()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string followedUsersSql = @"
                   SELECT FollowedId FROM Follows 
                   WHERE FollowerId = @UserId";

                var followedUserIds = await db.QueryAsync<int>(followedUsersSql, new { UserId = userId });

                string followedAnimalsSql = @"
                   SELECT AnimalId FROM AnimalFollows 
                   WHERE UserId = @UserId";

                var followedAnimalIds = await db.QueryAsync<int>(followedAnimalsSql, new { UserId = userId });

                var parameters = new DynamicParameters();
                parameters.Add("UserId", userId);
                parameters.Add("FollowedUserIds", followedUserIds.Count() > 0 ? followedUserIds.ToArray() : new int[] { -1 });
                parameters.Add("FollowedAnimalIds", followedAnimalIds.Count() > 0 ? followedAnimalIds.ToArray() : new int[] { -1 });

                string feedSql = @"
                   SELECT TOP 50 p.*, u.*, a.*, b.*, at.*,
                          (SELECT COUNT(*) FROM Comments c WHERE c.PostId = p.Id) AS CommentCount,
                          (SELECT COUNT(*) FROM Likes l WHERE l.PostId = p.Id) AS LikeCount
                   FROM Posts p
                   JOIN Users u ON p.UserId = u.Id
                   LEFT JOIN Animals a ON p.AnimalId = a.Id
                   LEFT JOIN AnimalBreeds b ON a.BreedId = b.Id
                   LEFT JOIN AnimalTypes at ON b.AnimalTypeId = at.Id
                   WHERE p.UserId = @UserId 
                      OR p.UserId IN @FollowedUserIds
                      OR p.AnimalId IN @FollowedAnimalIds
                   ORDER BY p.CreatedAt DESC";

                var postsDictionary = new Dictionary<int, Post>();

                await db.QueryAsync<Post, User, Animal, AnimalBreed, AnimalType, Post>(
                    feedSql,
                    (post, user, animal, breed, animalType) => {
                        if (!postsDictionary.TryGetValue(post.Id, out var existingPost))
                        {
                            existingPost = post;
                            existingPost.User = user;
                            existingPost.Comments = new List<Comment>();
                            existingPost.Likes = new List<Like>();

                            if (animal != null)
                            {
                                existingPost.Animal = animal;
                                animal.Breed = breed;
                                if (breed != null) breed.AnimalType = animalType;
                            }

                            postsDictionary.Add(existingPost.Id, existingPost);
                        }
                        return existingPost;
                    },
                    parameters,
                    splitOn: "Id,Id,Id,Id");

                string commentsSql = @"
                   SELECT c.*, u.*
                   FROM Comments c
                   JOIN Users u ON c.UserId = u.Id
                   WHERE c.PostId IN @PostIds";

                var postIds = postsDictionary.Keys.ToArray();
                if (postIds.Length > 0)
                {
                    var comments = await db.QueryAsync<Comment, User, Comment>(
                        commentsSql,
                        (comment, user) => {
                            comment.User = user;
                            return comment;
                        },
                        new { PostIds = postIds },
                        splitOn: "Id");

                    foreach (var comment in comments)
                    {
                        if (postsDictionary.TryGetValue(comment.PostId, out var post))
                        {
                            post.Comments.Add(comment);
                        }
                    }

                    string likesSql = "SELECT * FROM Likes WHERE PostId IN @PostIds";
                    var likes = await db.QueryAsync<Like>(likesSql, new { PostIds = postIds });

                    foreach (var like in likes)
                    {
                        if (postsDictionary.TryGetValue(like.PostId, out var post))
                        {
                            post.Likes.Add(like);
                        }
                    }
                }

                return View("Index", postsDictionary.Values.ToList());
            }
        }

        private async Task<bool> PostExists(IDbConnection db, int id)
        {
            string sql = "SELECT COUNT(1) FROM Posts WHERE Id = @Id";
            int count = await db.ExecuteScalarAsync<int>(sql, new { Id = id });
            return count > 0;
        }
    }
}