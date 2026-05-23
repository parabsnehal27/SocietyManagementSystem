using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using System.Security.Claims;

namespace SocietyManagementSystem.Controllers
{
    public class ResidentController : Controller
    {
        [Authorize(Roles = "Resident")]
        public IActionResult Dashboard()
        {
            return View();
        }
        private readonly ApplicationDbContext _context;

        public ResidentController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET
        public IActionResult SubmitComplaint()
        {
            return View();
        }

        // POST
        [HttpPost]
        public IActionResult SubmitComplaint(ComplaintViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string userEmail = User.FindFirstValue(ClaimTypes.Email);

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            var resident = _context.Residents.FirstOrDefault(r => r.UserId == user.UserId);

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
        public IActionResult TrackComplaints()
        {
            string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var resident = _context.Residents.FirstOrDefault(r => r.UserId == user.UserId);

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var complaints = _context.Complaints
                .Where(c => c.ResidentId == resident.ResidentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View(complaints);
        }

        public IActionResult VisitorRequests()
        {
            string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            var resident = _context.Residents.FirstOrDefault(r => r.UserId == user.UserId);

            var requests = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Where(v => v.ResidentId == resident.ResidentId)
                .OrderByDescending(v => v.EntryTime)
                .ToList();

            return View(requests);
        }

        public IActionResult ApproveVisitor(int id)
        {
            var entry = _context.VisitorEntries.FirstOrDefault(v => v.EntryId == id);

            if (entry != null)
            {
                entry.ApprovalStatus = "Approved";
                _context.SaveChanges();
            }

            return RedirectToAction("VisitorRequests");
        }

        public IActionResult RejectVisitor(int id)
        {
            var entry = _context.VisitorEntries.FirstOrDefault(v => v.EntryId == id);

            if (entry != null)
            {
                entry.ApprovalStatus = "Rejected";
                _context.SaveChanges();
            }

            return RedirectToAction("VisitorRequests");
        }
        [HttpGet]
        public IActionResult GetVisitorRequests()
        {
            string userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            var resident = _context.Residents.FirstOrDefault(r => r.UserId == user.UserId);

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
    }
}