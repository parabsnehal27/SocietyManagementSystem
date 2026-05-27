using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SocietyManagementSystem.Controllers
{
    public class ResidentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResidentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsResidentLoggedIn()
        {
            return HttpContext.Session.GetString("UserRole") == "Resident";
        }

        private Resident GetCurrentResident()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return null;

            return _context.Residents.FirstOrDefault(r => r.UserId == userId);
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // GET Submit Complaint
        public IActionResult SubmitComplaint()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST Submit Complaint
        [HttpPost]
        public IActionResult SubmitComplaint(ComplaintViewModel model)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            Complaint complaint = new Complaint
            {
                ResidentId = resident.ResidentId,
                Title = model.Title,
                Category = model.Category,
                Description = model.Description,
                Status = "Pending",
                Priority = null,
                AssignedTo = null
            };

            _context.Complaints.Add(complaint);
            _context.SaveChanges();

            TempData["Success"] = "Complaint submitted successfully.";

            return RedirectToAction("TrackComplaints");
        }

        // Track Complaints
        public IActionResult TrackComplaints()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var complaints = _context.Complaints
                .Where(c => c.ResidentId == resident.ResidentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View(complaints);
        }

        // Visitor Requests
        public IActionResult VisitorRequests()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var requests = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Where(v => v.ResidentId == resident.ResidentId)
                .OrderByDescending(v => v.EntryTime)
                .ToList();

            return View(requests);
        }

        // Approve Visitor
        public IActionResult ApproveVisitor(int id)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var entry = _context.VisitorEntries
                .FirstOrDefault(v =>
                    v.EntryId == id &&
                    v.ResidentId == resident.ResidentId);

            if (entry != null)
            {
                entry.ApprovalStatus = "Approved";
                _context.SaveChanges();
            }

            return RedirectToAction("VisitorRequests");
        }

        // Reject Visitor
        public IActionResult RejectVisitor(int id)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var entry = _context.VisitorEntries
                .FirstOrDefault(v =>
                    v.EntryId == id &&
                    v.ResidentId == resident.ResidentId);

            if (entry != null)
            {
                entry.ApprovalStatus = "Rejected";
                _context.SaveChanges();
            }

            return RedirectToAction("VisitorRequests");
        }

        // AJAX Visitor Requests
        [HttpGet]
        public IActionResult GetVisitorRequests()
        {
            if (!IsResidentLoggedIn())
                return Unauthorized();

            var resident = GetCurrentResident();

            if (resident == null)
                return Unauthorized();

            var requests = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Where(v => v.ResidentId == resident.ResidentId)
                .OrderByDescending(v => v.EntryTime)
                .Select(v => new
                {
                    v.EntryId,
                    VisitorName = v.Visitor.VisitorName,
                    PhoneNumber = v.Visitor.PhoneNumber,
                    Purpose = v.Visitor.Purpose,
                    IDProofType = v.Visitor.IDProofType,
                    IsIDVerified = v.Visitor.IsIDVerified,
                    ApprovalStatus = v.ApprovalStatus,
                    EntryTime = v.EntryTime.ToString("g")
                })
                .ToList();

            return Json(requests);
        }

        [HttpPost]
        public IActionResult SavePushSubscription([FromBody] PushSubscriptionViewModel model)
        {
            string email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return Unauthorized();
            }

            bool exists = _context.PushSubscriptions.Any(x =>
                x.UserId == user.UserId &&
                x.Endpoint == model.Endpoint);

            if (!exists)
            {
                PushSubscription subscription = new PushSubscription
                {
                    UserId = user.UserId,
                    Endpoint = model.Endpoint,
                    P256DH = model.P256DH,
                    Auth = model.Auth,
                    CreatedAt = DateTime.Now
                };

                _context.PushSubscriptions.Add(subscription);
                _context.SaveChanges();
            }

            return Ok();
        }
    }
}