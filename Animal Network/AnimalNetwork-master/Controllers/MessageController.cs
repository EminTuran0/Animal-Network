using AnimalNetwork.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageController> _logger;
        private readonly string _connectionString;

        public MessageController(IConfiguration configuration, ILogger<MessageController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userIdsSql = @"
                   SELECT DISTINCT ReceiverId as UserId FROM Messages WHERE SenderId = @UserId
                   UNION 
                   SELECT DISTINCT SenderId as UserId FROM Messages WHERE ReceiverId = @UserId";

                var conversationUserIds = await db.QueryAsync<int>(userIdsSql, new { UserId = userId });

                if (!conversationUserIds.Any())
                {
                    return View(new List<object>());
                }

                string usersSql = "SELECT * FROM Users WHERE Id IN @UserIds";
                var conversationUsers = await db.QueryAsync<User>(usersSql, new { UserIds = conversationUserIds });

                var conversationData = new List<object>();

                foreach (var user in conversationUsers)
                {
                    string latestMessageSql = @"
                       SELECT TOP 1 * FROM Messages 
                       WHERE (SenderId = @UserId AND ReceiverId = @OtherUserId) 
                          OR (SenderId = @OtherUserId AND ReceiverId = @UserId)
                       ORDER BY SentAt DESC";

                    var latestMessage = await db.QueryFirstOrDefaultAsync<Message>(
                        latestMessageSql, new { UserId = userId, OtherUserId = user.Id });

                    if (latestMessage != null)
                    {
                        string unreadCountSql = @"
                           SELECT COUNT(*) FROM Messages 
                           WHERE SenderId = @OtherUserId 
                             AND ReceiverId = @UserId 
                             AND IsRead = 0";

                        int unreadCount = await db.ExecuteScalarAsync<int>(
                            unreadCountSql, new { UserId = userId, OtherUserId = user.Id });

                        conversationData.Add(new
                        {
                            User = user,
                            LatestMessage = latestMessage,
                            UnreadCount = unreadCount
                        });
                    }
                }

                conversationData = conversationData
                    .OrderByDescending(c => ((dynamic)c).LatestMessage.SentAt)
                    .ToList();

                return View(conversationData);
            }
        }

        public async Task<IActionResult> Conversation(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = "SELECT * FROM Users WHERE Id = @Id";
                var otherUser = await db.QueryFirstOrDefaultAsync<User>(userSql, new { Id = id });

                if (otherUser == null)
                {
                    return NotFound();
                }

                string messagesSql = @"
                   SELECT * FROM Messages 
                   WHERE (SenderId = @UserId AND ReceiverId = @OtherUserId) 
                      OR (SenderId = @OtherUserId AND ReceiverId = @UserId)
                   ORDER BY SentAt";

                var messages = await db.QueryAsync<Message>(
                    messagesSql, new { UserId = userId, OtherUserId = id });

                string updateSql = @"
                   UPDATE Messages 
                   SET IsRead = 1
                   WHERE SenderId = @OtherUserId 
                     AND ReceiverId = @UserId 
                     AND IsRead = 0";

                await db.ExecuteAsync(updateSql, new { UserId = userId, OtherUserId = id });

                ViewBag.OtherUser = otherUser;
                return View(messages);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(nameof(Conversation), new { id = receiverId });
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = "SELECT COUNT(1) FROM Users WHERE Id = @Id";
                int recipientExists = await db.ExecuteScalarAsync<int>(userSql, new { Id = receiverId });

                if (recipientExists == 0)
                {
                    return NotFound();
                }

                int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string insertSql = @"
                   INSERT INTO Messages (SenderId, ReceiverId, Content, IsRead, SentAt)
                   VALUES (@SenderId, @ReceiverId, @Content, @IsRead, @SentAt)";

                await db.ExecuteAsync(insertSql, new
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    IsRead = false,
                    SentAt = DateTime.Now
                });

                return RedirectToAction(nameof(Conversation), new { id = receiverId });
            }
        }

        public async Task<IActionResult> New(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string userSql = "SELECT * FROM Users WHERE Id = @Id";
                var recipient = await db.QueryFirstOrDefaultAsync<User>(userSql, new { Id = id });

                if (recipient == null)
                {
                    return NotFound();
                }

                ViewBag.Recipient = recipient;
                return View();
            }
        }

        public async Task<IActionResult> Unread()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string unreadCountSql = @"
                   SELECT COUNT(*) FROM Messages 
                   WHERE ReceiverId = @UserId AND IsRead = 0";

                int unreadCount = await db.ExecuteScalarAsync<int>(unreadCountSql, new { UserId = userId });

                return Json(new { count = unreadCount });
            }
        }
    }
}