using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using System.Linq;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SocietyManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public AccountController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier,
            user.UserId.ToString()),

        new Claim(ClaimTypes.Email,
            user.Email),

        new Claim(ClaimTypes.Role,
            user.Role),

        new Claim(ClaimTypes.Name,
            user.FullName)
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // GET Register
        [HttpGet]
        public IActionResult RegisterResident()
        {
            return View();
        }

        // POST Register
        [HttpPost]
        public IActionResult RegisterResident(RegisterResidentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check email already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(model);
            }

            // Save Users table
            User user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Resident",
                ApprovalStatus = "Pending",
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Save Residents table
            Resident resident = new Resident
            {
                UserId = user.UserId,
                FlatNumber = model.FlatNumber,
                Wing = model.Wing,
                FloorNumber = model.FloorNumber,
                OwnerOrTenant = model.OwnerOrTenant,
                FamilyMembersCount = model.FamilyMembersCount,
                VehicleNumber = model.VehicleNumber,
                ContactPhone = model.ContactPhone,
                MoveInDate = model.MoveInDate
            };

            _context.Residents.Add(resident);
            _context.SaveChanges();

            TempData["Success"] = "Registration submitted. Wait for admin approval.";

            return RedirectToAction("Login");
        }

        // GET Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");

                if (User.IsInRole("Resident"))
                    return RedirectToAction("Dashboard", "Resident");

                if (User.IsInRole("SecurityGuard"))
                    return RedirectToAction("Dashboard", "Security");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            bool passwordMatch = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!passwordMatch)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user.ApprovalStatus != "Approved")
            {
                ModelState.AddModelError("", "Your account is pending admin approval.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(ClaimTypes.Name,
                    user.FullName),

                new Claim(ClaimTypes.Email,
                    user.Email),

                new Claim(ClaimTypes.Role,
                    user.Role)
            };

            var claimsIdentity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties =
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);


            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            if (user.Role == "Resident")
                return RedirectToAction("Dashboard", "Resident");

            if (user.Role == "SecurityGuard")
                return RedirectToAction("Dashboard", "Security");

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}