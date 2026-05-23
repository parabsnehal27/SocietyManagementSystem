using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using System.Security.Claims;

namespace SocietyManagementSystem.Controllers
{
    [Authorize(Roles = "SecurityGuard")]
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SecurityController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            DateTime today = DateTime.Today;

            var stats = new
            {
                TotalVisitors = _context.VisitorEntries
                    .Count(v => v.EntryTime.Date == today),

                PendingVisitors = _context.VisitorEntries
                    .Count(v => v.ApprovalStatus == "Pending"),

                ApprovedVisitors = _context.VisitorEntries
                    .Count(v => v.ApprovalStatus == "Approved"),

                DeniedVisitors = _context.VisitorEntries
                    .Count(v => v.ApprovalStatus == "Rejected"),

                ActiveVisitors = _context.VisitorEntries
                    .Count(v => v.ApprovalStatus == "Approved" && v.ExitTime == null)
            };

            return Json(stats);
        }

        // GET
        public IActionResult AddVisitorEntry()
        {
            return View();
        }

        // POST
        [HttpPost]
        public IActionResult AddVisitorEntry(VisitorEntryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string userEmail = User.FindFirstValue(ClaimTypes.Email);

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            var guard = _context.SecurityGuards.FirstOrDefault(g => g.UserId == user.UserId);

            var resident = _context.Residents.FirstOrDefault(r => r.FlatNumber == model.FlatNumber);

            if (resident == null)
            {
                ModelState.AddModelError("", "Resident not found.");
                return View(model);
            }

            Visitor visitor = new Visitor
            {
                VisitorName = model.VisitorName,
                PhoneNumber = model.PhoneNumber,
                Purpose = model.Purpose,
                VehicleNumber = model.VehicleNumber,
                IDProofType = model.IDProofType,
                IsIDVerified = model.IsIDVerified
            };

            _context.Visitors.Add(visitor);
            _context.SaveChanges();

            VisitorEntry entry = new VisitorEntry
            {
                VisitorId = visitor.VisitorId,
                ResidentId = resident.ResidentId,
                AddedByGuardId = guard.GuardId,
                ApprovalStatus = "Pending"
            };

            _context.VisitorEntries.Add(entry);
            _context.SaveChanges();

            TempData["Success"] = "Visitor entry added successfully.";

            return RedirectToAction("VisitorLogs");
        }
        public IActionResult VisitorLogs()
        {
            var visitorLogs = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Include(v => v.Resident)
                .OrderByDescending(v => v.EntryTime)
                .ToList();

            return View(visitorLogs);
        }

        public IActionResult MarkExit(int id)
        {
            var entry = _context.VisitorEntries.FirstOrDefault(v => v.EntryId == id);

            if (entry != null)
            {
                entry.ExitTime = DateTime.Now;
                _context.SaveChanges();
            }

            return RedirectToAction("VisitorLogs");
        }
        [HttpGet]
        public IActionResult GetVisitorLogs()
        {
            var visitorLogs = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Include(v => v.Resident)
                .OrderByDescending(v => v.EntryTime)
                .Select(v => new
                {
                    v.EntryId,
                    VisitorName = v.Visitor.VisitorName,
                    PhoneNumber = v.Visitor.PhoneNumber,
                    Purpose = v.Visitor.Purpose,
                    FlatNumber = v.Resident.FlatNumber,
                    ApprovalStatus = v.ApprovalStatus,
                    EntryTime = v.EntryTime.ToString("g"),
                    ExitTime = v.ExitTime.HasValue ? v.ExitTime.Value.ToString("g") : "Not Exited"
                })
                .ToList();

            return Json(visitorLogs);
        }
    }
}