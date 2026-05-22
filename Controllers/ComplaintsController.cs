using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;

namespace SocietyManagementSystem.Controllers
{
    public class ComplaintsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComplaintsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("AdminSession") != null;
        }

        // LIST
        public IActionResult Index(string search)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var complaints =
                _context.Complaints.AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                complaints = complaints.Where(c =>

                    c.Title.Contains(search)

                    ||

                    c.Category.Contains(search)

                    ||

                    c.Status.Contains(search)

                    ||

                    c.Priority.Contains(search)

                );
            }

            return View(
                complaints.ToList()
            );
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

        // SAVE COMPLAINT
        [HttpPost]
        public IActionResult Create(Complaint complaint)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            complaint.ResidentId = 2;

            complaint.Status = "Pending";

            complaint.CreatedAt = DateTime.Now;

            _context.Complaints.Add(complaint);

            _context.SaveChanges();

            TempData["Success"] = "Complaint Added Successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Resolve(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var complaint = _context.Complaints.Find(id);

            if (complaint == null)
            {
                return NotFound();
            }

            complaint.Status = "Resolved";

            complaint.ResolvedAt = DateTime.Now;

            complaint.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = "Complaint Resolved Successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var complaint = _context.Complaints.Find(id);

            if (complaint == null)
            {
                return NotFound();
            }

            _context.Complaints.Remove(complaint);

            _context.SaveChanges();

            TempData["Success"] = "Complaint Deleted Successfully";

            return RedirectToAction("Index");
        }


    }
}