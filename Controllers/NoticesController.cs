using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;

namespace SocietyManagementSystem.Controllers
{
    public class NoticesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NoticesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("AdminSession") != null;
        }

        // LIST
        public IActionResult Index()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var notices = _context.Notices.ToList();

            foreach (var notice in notices)
            {
                if (notice.ExpiryDate < DateTime.Now)
                {
                    notice.IsActive = false;
                }
                else
                {
                    notice.IsActive = true;
                }
            }

            _context.SaveChanges();

            return View(notices);
        }

        // CREATE PAGE
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // SAVE NOTICE
        [HttpPost]
        public IActionResult Create(Notice notice)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            notice.PostedBy = 1;

            notice.PostedDate = DateTime.Now;

            notice.IsActive = true;

            _context.Notices.Add(notice);

            _context.SaveChanges();

            TempData["Success"] = "Notice Added Successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var notice = _context.Notices.Find(id);

            if (notice == null)
            {
                return NotFound();
            }

            _context.Notices.Remove(notice);

            _context.SaveChanges();

            TempData["Success"] = "Notice Deleted Successfully";

            return RedirectToAction("Index");
        }
    }
}