using AnimalNetwork.Models.Entities;
using AnimalNetwork.Models.ViewModels;
using BCrypt.Net;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AnimalNetwork.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration, ILogger<AccountController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    string sql = "SELECT * FROM Users WHERE Email = @Email";
                    var user = await db.QueryFirstOrDefaultAsync<User>(sql, new { Email = model.Email });

                    if (user != null && VerifyPassword(model.Password, user.PasswordHash))
                    {
                        await SignInUser(user);

                        string updateSql = "UPDATE Users SET LastLogin = @LastLogin WHERE Id = @Id";
                        await db.ExecuteAsync(updateSql, new { LastLogin = DateTime.Now, Id = user.Id });

                        return RedirectToAction("Index", "Home");
                    }

                    ModelState.AddModelError("", "Invalid email or password");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    string emailCheckSql = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    int emailCount = await db.ExecuteScalarAsync<int>(emailCheckSql, new { Email = model.Email });

                    if (emailCount > 0)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        return View(model);
                    }

                 
                    string usernameCheckSql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
                    int usernameCount = await db.ExecuteScalarAsync<int>(usernameCheckSql, new { Username = model.Username });

                    if (usernameCount > 0)
                    {
                        ModelState.AddModelError("Username", "This username is already taken");
                        return View(model);
                    }

                 
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        JoinDate = DateTime.Now,
                        LastLogin = DateTime.Now,
                        IsAdmin = false,
                        Bio = $"Hi, I'm {model.FirstName}! I love animals and sharing their adventures."
                    };

                    string insertSql = @"
                        INSERT INTO Users (Username, Email, FirstName, LastName, PasswordHash, Bio, ProfileImage, 
                                          LocationId, IsAdmin, JoinDate, LastLogin)
                        VALUES (@Username, @Email, @FirstName, @LastName, @PasswordHash, @Bio, @ProfileImage, 
                                @LocationId, @IsAdmin, @JoinDate, @LastLogin);
                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    user.Id = await db.QuerySingleAsync<int>(insertSql, user);

                    await SignInUser(user);

                    return RedirectToAction("Index", "Home");
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Only a real BCrypt hash may authenticate. A stored value that is not a hash
            // must never be accepted as a password.
            if (string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith("$2"))
            {
                _logger.LogWarning("Login attempt against an account whose password is not BCrypt-hashed.");
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (SaltParseException ex)
            {
                _logger.LogWarning(ex, "Stored password hash could not be parsed.");
                return false;
            }
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                new Claim(ClaimTypes.Surname, user.LastName ?? string.Empty),
                new Claim("ProfileImage", user.ProfileImage ?? string.Empty)
            };

            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}