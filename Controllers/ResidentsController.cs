using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;

namespace SocietyManagementSystem.Controllers
{
    public class ResidentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResidentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("AdminSession") != null;
        }

        public IActionResult Index()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }
            
            var residents = _context.Residents.ToList();

            return View(residents);
        }

        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // GET EDIT PAGE
        public IActionResult Edit(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var resident = _context.Residents.Find(id);

            if (resident == null)
            {
                return NotFound();
            }

            return View(resident);
        }

        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var resident = _context.Residents.Find(id);

            if (resident == null)
            {
                return NotFound();
            }

            _context.Residents.Remove(resident);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Approve(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var resident = _context.Residents.Find(id);

            if (resident == null)
            {
                return NotFound();
            }

            resident.IsApproved = true;

            _context.SaveChanges();

            TempData["Success"] = "Resident Approved Successfully";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Edit(Resident resident)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            _context.Residents.Update(resident);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Create(Resident resident)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            resident.UserId = 1;
            resident.CreatedAt = DateTime.Now;

            _context.Residents.Add(resident);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}