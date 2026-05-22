using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using BCrypt.Net;


namespace SocietyManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
                MoveInDate = model.MoveInDate,
                IsApproved = false
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
            return View();
        }

        // POST Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
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

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Role based redirect
            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            if (user.Role == "Resident")
                return RedirectToAction("Dashboard", "Resident");

            if (user.Role == "SecurityGuard")
                return RedirectToAction("Dashboard", "Security");

            return RedirectToAction("Index", "Home");
        }
    }
}