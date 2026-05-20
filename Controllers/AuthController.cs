using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace SocietyManagementSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET LOGIN PAGE
        public IActionResult Login()
        {
            return View();
        }

        // POST LOGIN
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Email == email
                                  && x.PasswordHash == password);

            if (user != null)
            {
                HttpContext.Session.SetString("AdminSession", user.Email);

                return RedirectToAction("Dashboard", "Admin");
            }

            ViewBag.Error = "Invalid Email or Password";

            return View();
        }

        // GET FORGOT PASSWORD PAGE
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST FORGOT PASSWORD
        [HttpPost]
        public IActionResult ForgotPassword(string email, string newPassword)
        {
            // Temporary Hardcoded Email Check
            if (email == "admin@gmail.com")
            {
                // Temporary password update
                TempData["Success"] = "Password Reset Successfully";

                return RedirectToAction("Login");
            }

            ViewBag.Error = "Email not found";

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}