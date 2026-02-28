using AnimalNetwork.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    public class EventController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventController> _logger;
        private readonly string _connectionString;

        public EventController(IConfiguration configuration, ILogger<EventController> logger)
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
                   SELECT e.*, u.*, l.*,
                          (SELECT COUNT(*) FROM EventParticipants ep WHERE ep.EventId = e.Id) AS ParticipantCount
                   FROM Events e
                   LEFT JOIN Users u ON e.CreatorId = u.Id
                   LEFT JOIN Locations l ON e.LocationId = l.Id
                   WHERE e.StartTime >= @CurrentTime
                   ORDER BY e.StartTime";

                var eventsDictionary = new Dictionary<int, Event>();

                var events = await db.QueryAsync<Event, User, Location, Event>(
                    sql,
                    (evt, creator, location) => {
                        if (!eventsDictionary.TryGetValue(evt.Id, out var existingEvent))
                        {
                            existingEvent = evt;
                            existingEvent.Creator = creator;
                            existingEvent.Location = location;
                            existingEvent.Participants = new List<EventParticipant>();
                            eventsDictionary.Add(existingEvent.Id, existingEvent);
                        }
                        return existingEvent;
                    },
                    new { CurrentTime = DateTime.Now },
                    splitOn: "Id,Id");

                return View(eventsDictionary.Values.ToList());
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
                   SELECT e.*, u.*, l.*
                   FROM Events e
                   LEFT JOIN Users u ON e.CreatorId = u.Id
                   LEFT JOIN Locations l ON e.LocationId = l.Id
                   WHERE e.Id = @EventId";

                var eventData = await db.QueryAsync<Event, User, Location, Event>(
                    sql,
                    (evt, creator, location) => {
                        evt.Creator = creator;
                        evt.Location = location;
                        return evt;
                    },
                    new { EventId = id },
                    splitOn: "Id,Id");

                var evt = eventData.FirstOrDefault();

                if (evt == null)
                {
                    return NotFound();
                }

                string participantsSql = @"
                   SELECT ep.*, u.*
                   FROM EventParticipants ep
                   JOIN Users u ON ep.UserId = u.Id
                   WHERE ep.EventId = @EventId";

                var participants = await db.QueryAsync<EventParticipant, User, EventParticipant>(
                    participantsSql,
                    (participant, user) => {
                        participant.User = user;
                        return participant;
                    },
                    new { EventId = id },
                    splitOn: "Id");

                evt.Participants = participants.ToList();

                if (User.Identity.IsAuthenticated)
                {
                    int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    string statusSql = @"
                       SELECT Status 
                       FROM EventParticipants 
                       WHERE EventId = @EventId AND UserId = @UserId";

                    var participation = await db.QueryFirstOrDefaultAsync<string>(
                        statusSql, new { EventId = id, UserId = userId });

                    ViewBag.UserParticipationStatus = participation;
                }

                return View(evt);
            }
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(sql);
                ViewBag.Locations = locations;

                return View();
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event evt)
        {
            if (ModelState.IsValid)
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    evt.CreatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    evt.CreatedAt = DateTime.Now;

                    string insertSql = @"
                       INSERT INTO Events (Title, Description, LocationId, StartTime, EndTime, 
                                         CreatorId, CreatedAt, ImageUrl)
                       VALUES (@Title, @Description, @LocationId, @StartTime, @EndTime, 
                              @CreatorId, @CreatedAt, @ImageUrl);
                       SELECT CAST(SCOPE_IDENTITY() as int)";

                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            evt.Id = await db.QuerySingleAsync<int>(insertSql, evt, transaction);

                            string participantSql = @"
                               INSERT INTO EventParticipants (EventId, UserId, Status)
                               VALUES (@EventId, @UserId, @Status)";

                            await db.ExecuteAsync(participantSql, new
                            {
                                EventId = evt.Id,
                                UserId = evt.CreatorId,
                                Status = "Going"
                            }, transaction);

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }

                    return RedirectToAction(nameof(Details), new { id = evt.Id });
                }
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(sql);
                ViewBag.Locations = locations;
            }

            return View(evt);
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
                string sql = "SELECT * FROM Events WHERE Id = @Id";
                var evt = await db.QueryFirstOrDefaultAsync<Event>(sql, new { Id = id });

                if (evt == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (evt.CreatorId != userId && !isAdmin)
                {
                    return Forbid();
                }

                string locationSql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(locationSql);
                ViewBag.Locations = locations;

                return View(evt);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event evt)
        {
            if (id != evt.Id)
            {
                return NotFound();
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Events WHERE Id = @Id";
                var originalEvent = await db.QueryFirstOrDefaultAsync<Event>(checkSql, new { Id = id });

                if (originalEvent == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (originalEvent.CreatorId != userId && !isAdmin)
                {
                    return Forbid();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        evt.CreatorId = originalEvent.CreatorId;
                        evt.CreatedAt = originalEvent.CreatedAt;

                        string updateSql = @"
                           UPDATE Events 
                           SET Title = @Title, 
                               Description = @Description, 
                               LocationId = @LocationId, 
                               StartTime = @StartTime, 
                               EndTime = @EndTime, 
                               ImageUrl = @ImageUrl
                           WHERE Id = @Id";

                        await db.ExecuteAsync(updateSql, evt);

                        return RedirectToAction(nameof(Details), new { id = evt.Id });
                    }
                    catch
                    {
                        if (!await EventExists(evt.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                string locationSql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(locationSql);
                ViewBag.Locations = locations;

                return View(evt);
            }
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT * FROM Events WHERE Id = @Id";
                var evt = await db.QueryFirstOrDefaultAsync<Event>(checkSql, new { Id = id });

                if (evt == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.IsInRole("Admin");

                if (evt.CreatorId != userId && !isAdmin)
                {
                    return Forbid();
                }

                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        string deleteParticipantsSql = "DELETE FROM EventParticipants WHERE EventId = @Id";
                        await db.ExecuteAsync(deleteParticipantsSql, new { Id = id }, transaction);

                        string deleteEventSql = "DELETE FROM Events WHERE Id = @Id";
                        await db.ExecuteAsync(deleteEventSql, new { Id = id }, transaction);

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
        public async Task<IActionResult> Participate(int id, string status)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string checkSql = "SELECT COUNT(1) FROM Events WHERE Id = @Id";
                int eventCount = await db.ExecuteScalarAsync<int>(checkSql, new { Id = id });

                if (eventCount == 0)
                {
                    return NotFound();
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string checkParticipationSql = @"
                   SELECT Id FROM EventParticipants 
                   WHERE EventId = @EventId AND UserId = @UserId";

                var participationId = await db.QueryFirstOrDefaultAsync<int?>(
                    checkParticipationSql, new { EventId = id, UserId = userId });

                if (participationId.HasValue)
                {
                    string updateSql = @"
                       UPDATE EventParticipants 
                       SET Status = @Status 
                       WHERE Id = @Id";

                    await db.ExecuteAsync(updateSql, new { Id = participationId.Value, Status = status });
                }
                else
                {
                    string insertSql = @"
                       INSERT INTO EventParticipants (EventId, UserId, Status)
                       VALUES (@EventId, @UserId, @Status)";

                    await db.ExecuteAsync(insertSql, new { EventId = id, UserId = userId, Status = status });
                }

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveParticipation(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string deleteSql = @"
                   DELETE FROM EventParticipants 
                   WHERE EventId = @EventId AND UserId = @UserId";

                await db.ExecuteAsync(deleteSql, new { EventId = id, UserId = userId });

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> Past()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = @"
                   SELECT e.*, u.*, l.*,
                          (SELECT COUNT(*) FROM EventParticipants ep WHERE ep.EventId = e.Id) AS ParticipantCount
                   FROM Events e
                   LEFT JOIN Users u ON e.CreatorId = u.Id
                   LEFT JOIN Locations l ON e.LocationId = l.Id
                   WHERE e.EndTime < @CurrentTime
                   ORDER BY e.EndTime DESC";

                var eventsDictionary = new Dictionary<int, Event>();

                var events = await db.QueryAsync<Event, User, Location, Event>(
                    sql,
                    (evt, creator, location) => {
                        if (!eventsDictionary.TryGetValue(evt.Id, out var existingEvent))
                        {
                            existingEvent = evt;
                            existingEvent.Creator = creator;
                            existingEvent.Location = location;
                            existingEvent.Participants = new List<EventParticipant>();
                            eventsDictionary.Add(existingEvent.Id, existingEvent);
                        }
                        return existingEvent;
                    },
                    new { CurrentTime = DateTime.Now },
                    splitOn: "Id,Id");

                return View("Index", eventsDictionary.Values.ToList());
            }
        }

        [Authorize]
        public async Task<IActionResult> MyEvents()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = @"
                   SELECT e.*, u.*, l.*,
                          (SELECT COUNT(*) FROM EventParticipants ep WHERE ep.EventId = e.Id) AS ParticipantCount
                   FROM Events e
                   LEFT JOIN Users u ON e.CreatorId = u.Id
                   LEFT JOIN Locations l ON e.LocationId = l.Id
                   WHERE e.CreatorId = @UserId
                   ORDER BY e.CreatedAt DESC";

                var eventsDictionary = new Dictionary<int, Event>();

                var events = await db.QueryAsync<Event, User, Location, Event>(
                    sql,
                    (evt, creator, location) => {
                        if (!eventsDictionary.TryGetValue(evt.Id, out var existingEvent))
                        {
                            existingEvent = evt;
                            existingEvent.Creator = creator;
                            existingEvent.Location = location;
                            existingEvent.Participants = new List<EventParticipant>();
                            eventsDictionary.Add(existingEvent.Id, existingEvent);
                        }
                        return existingEvent;
                    },
                    new { UserId = userId },
                    splitOn: "Id,Id");

                return View("Index", eventsDictionary.Values.ToList());
            }
        }

        [Authorize]
        public async Task<IActionResult> Attending()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = @"
                   SELECT e.*, u.*, l.*,
                          (SELECT COUNT(*) FROM EventParticipants ep WHERE ep.EventId = e.Id) AS ParticipantCount
                   FROM EventParticipants ep
                   JOIN Events e ON ep.EventId = e.Id
                   LEFT JOIN Users u ON e.CreatorId = u.Id
                   LEFT JOIN Locations l ON e.LocationId = l.Id
                   WHERE ep.UserId = @UserId 
                     AND ep.Status = 'Going'
                     AND e.StartTime >= @CurrentTime
                   ORDER BY e.StartTime";

                var eventsDictionary = new Dictionary<int, Event>();

                var events = await db.QueryAsync<Event, User, Location, Event>(
                    sql,
                    (evt, creator, location) => {
                        if (!eventsDictionary.TryGetValue(evt.Id, out var existingEvent))
                        {
                            existingEvent = evt;
                            existingEvent.Creator = creator;
                            existingEvent.Location = location;
                            existingEvent.Participants = new List<EventParticipant>();
                            eventsDictionary.Add(existingEvent.Id, existingEvent);
                        }
                        return existingEvent;
                    },
                    new { UserId = userId, CurrentTime = DateTime.Now },
                    splitOn: "Id,Id");

                return View("Index", eventsDictionary.Values.ToList());
            }
        }

        public async Task<IActionResult> Search(string search, int? locationId, DateTime? startDate, DateTime? endDate)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                var conditions = new List<string>();

                string baseSql = @"
                   SELECT e.*, u.*, l.*,
                          (SELECT COUNT(*) FROM EventParticipants ep WHERE ep.EventId = e.Id) AS ParticipantCount
                   FROM Events e
                   LEFT JOIN Users u ON e.CreatorId = u.Id
                   LEFT JOIN Locations l ON e.LocationId = l.Id";

                if (!string.IsNullOrEmpty(search))
                {
                    conditions.Add("(e.Title LIKE @Search OR e.Description LIKE @Search)");
                    parameters.Add("Search", $"%{search}%");
                }

                if (locationId.HasValue)
                {
                    conditions.Add("e.LocationId = @LocationId");
                    parameters.Add("LocationId", locationId.Value);
                }

                if (startDate.HasValue)
                {
                    conditions.Add("e.StartTime >= @StartDate");
                    parameters.Add("StartDate", startDate.Value);
                }
                else
                {
                    conditions.Add("e.StartTime >= @CurrentTime");
                    parameters.Add("CurrentTime", DateTime.Now);
                }

                if (endDate.HasValue)
                {
                    conditions.Add("e.StartTime <= @EndDate");
                    parameters.Add("EndDate", endDate.Value);
                }

                if (conditions.Any())
                {
                    baseSql += " WHERE " + string.Join(" AND ", conditions);
                }

                baseSql += " ORDER BY e.StartTime";

                var eventsDictionary = new Dictionary<int, Event>();

                var events = await db.QueryAsync<Event, User, Location, Event>(
                    baseSql,
                    (evt, creator, location) => {
                        if (!eventsDictionary.TryGetValue(evt.Id, out var existingEvent))
                        {
                            existingEvent = evt;
                            existingEvent.Creator = creator;
                            existingEvent.Location = location;
                            existingEvent.Participants = new List<EventParticipant>();
                            eventsDictionary.Add(existingEvent.Id, existingEvent);
                        }
                        return existingEvent;
                    },
                    parameters,
                    splitOn: "Id,Id");

                string locationSql = "SELECT * FROM Locations ORDER BY Country, City";
                var locations = await db.QueryAsync<Location>(locationSql);
                ViewBag.Locations = locations;

                return View("Index", eventsDictionary.Values.ToList());
            }
        }

        private async Task<bool> EventExists(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "SELECT COUNT(1) FROM Events WHERE Id = @Id";
                int count = await db.ExecuteScalarAsync<int>(sql, new { Id = id });
                return count > 0;
            }
        }
    }
}