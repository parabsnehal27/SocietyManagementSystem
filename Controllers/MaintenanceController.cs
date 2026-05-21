using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;

namespace SocietyManagementSystem.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceController(ApplicationDbContext context)
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

            var maintenanceList = _context.Maintenance.ToList();

            return View(maintenanceList);
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

        //MARKPAID
        public IActionResult MarkPaid(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maintenance = _context.Maintenance.Find(id);

            if (maintenance == null)
            {
                return NotFound();
            }

            maintenance.PaymentStatus = "Paid";

            maintenance.PaidDate = DateTime.Now;

            maintenance.PaymentMethod = "Cash";

            maintenance.TransactionReference =
                "TXN" + DateTime.Now.Ticks;

            _context.SaveChanges();

            TempData["Success"] =
                "Maintenance Marked As Paid";

            return RedirectToAction("Index");
        }

        //DELETE
        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var maintenance = _context.Maintenance.Find(id);

            if (maintenance == null)
            {
                return NotFound();
            }

            _context.Maintenance.Remove(maintenance);

            _context.SaveChanges();

            TempData["Success"] =
                "Maintenance Deleted Successfully";

            return RedirectToAction("Index");
        }



        // SAVE
        [HttpPost]
        public IActionResult Create(Maintenance maintenance)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            // IMPORTANT:
            // Replace with real ResidentId existing in DB

            // YEAR VALIDATION
            if (maintenance.Year < 2020 ||
                maintenance.Year > 2100)
            {
                TempData["Error"] =
                    "Enter valid year";

                return View(maintenance);
            }

            // MONTH VALIDATION
            if (maintenance.Month < 1 ||
                maintenance.Month > 12)
            {
                TempData["Error"] =
                    "Invalid month selected";

                return View(maintenance);
            }

            // AMOUNT VALIDATION
            if (maintenance.Amount <= 0)
            {
                TempData["Error"] =
                    "Amount must be greater than 0";

                return View(maintenance);
            }

            maintenance.ResidentId = 2;

            maintenance.PaymentStatus = "pending";

            maintenance.CreatedAt = DateTime.Now;

            _context.Maintenance.Add(maintenance);

            _context.SaveChanges();

            TempData["Success"] = "Maintenance Added Successfully";

            return RedirectToAction("Index");
        }
    }
}