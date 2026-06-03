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
using System.Diagnostics;

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
            // Email already exists
            var existingUser =
                _context.Users
                .FirstOrDefault(u =>
                    u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already registered.");

                return View(model);
            }

            // Phone number validation
            if (!System.Text.RegularExpressions.Regex
                .IsMatch(model.PhoneNumber ?? "",
                @"^[6-9]\d{9}$"))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Enter valid 10 digit mobile number.");

                return View(model);
            }

            // Password validation
            if (!System.Text.RegularExpressions.Regex
                .IsMatch(model.Password ?? "",
                @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&]).{6,}$"))
            {
                ModelState.AddModelError(
                    "Password",
                    "Password must contain at least 6 characters, one letter, one number and one special character.");

                return View(model);
            }

            // Confirm password
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Passwords do not match.");

                return View(model);
            }

            // Family member count
            if (model.FamilyMembersCount <= 0)
            {
                ModelState.AddModelError(
                    "FamilyMembersCount",
                    "Family members count must be greater than 0.");

                return View(model);
            }

            // Owner/Tenant validation
            var flatResidents =
                _context.Residents
                .Where(r =>
                    r.Wing == model.Wing &&
                    r.FlatNumber == model.FlatNumber)
                .ToList();

            if (model.OwnerOrTenant == "Owner")
            {
                bool ownerExists =
                    flatResidents.Any(r =>
                        r.OwnerOrTenant == "Owner");

                if (ownerExists)
                {
                    ModelState.AddModelError(
                        "",
                        "Owner already registered for this flat.");

                    return View(model);
                }
            }

            if (model.OwnerOrTenant == "Tenant")
            {
                bool tenantExists =
                    flatResidents.Any(r =>
                        r.OwnerOrTenant == "Tenant");

                if (tenantExists)
                {
                    ModelState.AddModelError(
                        "",
                        "Tenant already registered for this flat.");

                    return View(model);
                }

                bool ownerExists =
                    flatResidents.Any(r =>
                        r.OwnerOrTenant == "Owner");

                if (!ownerExists)
                {
                    ModelState.AddModelError(
                        "",
                        "Owner must be registered before tenant registration.");

                    return View(model);
                }
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
            Console.WriteLine($"User Found: {user?.Email}");
            Console.WriteLine($"Role: {user?.Role}");
            Console.WriteLine($"Approval: {user?.ApprovalStatus}");


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
            Console.WriteLine("Password Verified");
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

            Console.WriteLine("Login Button Clicked");
            Console.WriteLine(model.Email);

            Console.WriteLine(user?.Role);

            Console.WriteLine("Redirecting...");

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