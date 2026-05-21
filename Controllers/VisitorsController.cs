using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace SocietyManagementSystem.Controllers
{
    public class VisitorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VisitorsController(ApplicationDbContext context)
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

            var visitors = _context.Visitors.ToList();

            return View(visitors);
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

        //DELETE
        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var visitor = _context.Visitors.Find(id);

            if (visitor == null)
            {
                return NotFound();
            }

            _context.Visitors.Remove(visitor);

            _context.SaveChanges();

            TempData["Success"] =
                "Visitor Deleted Successfully";

            return RedirectToAction("Index");
        }

        // SAVE VISITOR
        [HttpPost]
        public IActionResult Create(Visitor visitor)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            // PHONE VALIDATION
            bool validPhone =
                Regex.IsMatch(
                    visitor.PhoneNumber ?? "",
                    @"^[0-9]{10}$"
                );

            if (!validPhone)
            {
                TempData["Error"] =
                    "Phone number must contain exactly 10 digits";

                return View(visitor);
            }

            // VEHICLE VALIDATION
            if (!string.IsNullOrEmpty(visitor.VehicleNumber))
            {
                bool validVehicle =
                    Regex.IsMatch(
                        visitor.VehicleNumber,
                        @"^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$"
                    );

                if (!validVehicle)
                {
                    TempData["Error"] =
                        "Invalid Vehicle Number Format";

                    return View(visitor);
                }
            }

            // AADHAR VALIDATION
            if (visitor.IDProofType == "Aadhar Card")
            {
                bool validAadhar =
                    Regex.IsMatch(
                        visitor.IDProofNumber ?? "",
                        @"^[0-9]{12}$"
                    );

                if (!validAadhar)
                {
                    TempData["Error"] =
                        "Invalid Aadhar Number";

                    return View(visitor);
                }
            }

            // PAN VALIDATION
            if (visitor.IDProofType == "PAN Card")
            {
                bool validPan =
                    Regex.IsMatch(
                        visitor.IDProofNumber ?? "",
                        @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"
                    );

                if (!validPan)
                {
                    TempData["Error"] =
                        "Invalid PAN Number";

                    return View(visitor);
                }
            }

           
            visitor.CreatedAt = DateTime.Now;

            _context.Visitors.Add(visitor);

            _context.SaveChanges();

            TempData["Success"] =
                "Visitor Added Successfully";

            return RedirectToAction("Index");
        }
    }
}