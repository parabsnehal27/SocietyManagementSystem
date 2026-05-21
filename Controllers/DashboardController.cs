using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using System.Linq;

namespace SocietyManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
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

            // COUNTS
            ViewBag.TotalResidents =
                _context.Residents.Count();

            ViewBag.TotalComplaints =
                _context.Complaints.Count();

            ViewBag.PendingComplaints =
                _context.Complaints
                    .Count(c => c.Status == "Pending");

            ViewBag.ResolvedComplaints =
                _context.Complaints
                    .Count(c => c.Status == "Resolved");

            ViewBag.TotalNotices =
                _context.Notices.Count();

            ViewBag.TotalVisitors =
                _context.Visitors.Count();

            ViewBag.PaidMaintenance =
                _context.Maintenance
                    .Count(m => m.PaymentStatus == "Paid");

            ViewBag.PendingMaintenance =
                _context.Maintenance
                    .Count(m => m.PaymentStatus == "Pending");

            // RECENT COMPLAINTS
            ViewBag.RecentComplaints =
                _context.Complaints
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

            return View();
        }

        //RESIDENTS DASHBOARD
        public IActionResult ResidentDashboard()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            ViewBag.TotalComplaints =
                _context.Complaints.Count();

            ViewBag.TotalNotices =
                _context.Notices.Count();

            ViewBag.PendingMaintenance =
                _context.Maintenance
                    .Count(m => m.PaymentStatus != "Paid");

            return View();
        }

    }
}