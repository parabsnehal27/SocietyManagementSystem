using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using WebPush;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace SocietyManagementSystem.Controllers
{
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public SecurityController( ApplicationDbContext context,IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private bool IsSecurityGuardLoggedIn()
        {
            return User.Identity.IsAuthenticated && User.IsInRole("SecurityGuard");
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // Dashboard Stats AJAX
        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            if (!IsSecurityGuardLoggedIn())
                return Unauthorized();

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

        // GET Add Visitor
        public IActionResult AddVisitorEntry()
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.FlatNumbers = _context.Residents
                .Select(r => new SelectListItem
                {
                    Value = r.FlatNumber,
                    Text = r.FlatNumber + " - " + r.Wing
                })
                .ToList();

            return View();
        }

        // POST Add Visitor
        [HttpPost]
        public IActionResult AddVisitorEntry(VisitorEntryViewModel model)
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");



            if (!ModelState.IsValid)
            {
                ViewBag.FlatNumbers = _context.Residents
                    .Select(r => new SelectListItem
                    {
                        Value = r.FlatNumber,
                        Text = r.FlatNumber + " - " + r.Wing
                    })
                    .ToList();

                return View(model);
            }
            if (!System.Text.RegularExpressions.Regex
                .IsMatch(model.PhoneNumber ?? "",
                @"^[6-9]\d{9}$"))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Enter valid 10 digit mobile number.");

                return View(model);
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var guard = _context.SecurityGuards
                .FirstOrDefault(g => g.UserId == userId);

            if (guard == null)
            {
                ModelState.AddModelError("", "Security guard not found.");
                return View(model);
            }

            var resident = _context.Residents
                .FirstOrDefault(r => r.FlatNumber == model.FlatNumber);

            if (resident == null)
            {
                ModelState.AddModelError("", "Resident not found.");

                ViewBag.FlatNumbers = _context.Residents
                    .Select(r => new SelectListItem
                    {
                        Value = r.FlatNumber,
                        Text = r.FlatNumber + " - " + r.Wing
                    })
                    .ToList();

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

            var residentUser = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

            if (residentUser != null)
            {
                var subscriptions = _context.PushSubscriptions
                    .Where(x => x.UserId == residentUser.UserId)
                    .ToList();

                foreach (var sub in subscriptions)
                {
                    try
                    {
                        SendPushNotification(
                            sub.Endpoint,
                            sub.P256DH,
                            sub.Auth,
                            visitor.VisitorName,
                            entry.EntryId
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            TempData["Success"] = "Visitor entry added successfully.";

            return RedirectToAction("VisitorLogs", "Security");
        }
        // Mark Exit
        public IActionResult MarkExit(int id)
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            var entry = _context.VisitorEntries
                .FirstOrDefault(v => v.EntryId == id);

            if (entry != null)
            {
                entry.ExitTime = DateTime.Now;
                _context.SaveChanges();
            }

            return RedirectToAction("VisitorLogs");
        }

        // AJAX Refresh Visitor Logs
        [HttpGet]
        public IActionResult GetVisitorLogs()
        {
            if (!IsSecurityGuardLoggedIn())
                return Unauthorized();

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
                    ExitTime = v.ExitTime.HasValue
                        ? v.ExitTime.Value.ToString("g")
                        : "Not Exited"
                })
                .ToList();

            return Json(visitorLogs);
        }
        public IActionResult VisitorLogs()
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            var visitorLogs = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Include(v => v.Resident)
                .OrderByDescending(v => v.EntryTime)
                .ToList();

            return View(visitorLogs);
        }

        // AJAX Search + Filter
        [HttpGet]
        public IActionResult FilterVisitorLogs(string searchTerm, string status)
        {
            if (!IsSecurityGuardLoggedIn())
                return Unauthorized();

            var logs = _context.VisitorEntries
                .Include(v => v.Visitor)
                .Include(v => v.Resident)
                .OrderByDescending(v => v.EntryTime)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                logs = logs.Where(v =>
                    v.Visitor.VisitorName.Contains(searchTerm) ||
                    v.Visitor.PhoneNumber.Contains(searchTerm) ||
                    v.Resident.FlatNumber.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                if (status == "Exited")
                {
                    logs = logs.Where(v => v.ExitTime != null);
                }
                else
                {
                    logs = logs.Where(v =>
                        v.ApprovalStatus == status &&
                        v.ExitTime == null);
                }
            }

            return PartialView("_VisitorLogsTable", logs.ToList());
        }
        private void SendPushNotification(string endpoint,string p256dh,string auth,string visitorName,int entryId)
        {
            var publicKey = _configuration["VapidSettings:PublicKey"];
            var privateKey = _configuration["VapidSettings:PrivateKey"];
            var subject = _configuration["VapidSettings:Subject"];

            var subscription = new WebPush.PushSubscription(endpoint, p256dh, auth);

            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

            var client = new WebPushClient();

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                title = "New Visitor Request",
                body = $"{visitorName} is waiting at the gate.",
                url = "/Resident/VisitorRequests",
                entryId = entryId
            });

            client.SendNotification(subscription, payload, vapidDetails);
        }
        public IActionResult Profile()
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdClaim =User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new SecurityProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,

                EmployeeId = user.UserId.ToString(),
                Shift = "Day Shift",
                Role = "Security Guard"
            };

            return View(model);
        }
        public IActionResult ChangePassword()
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!IsSecurityGuardLoggedIn())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Verify current password
            bool isValid = BCrypt.Net.BCrypt.Verify(
                model.CurrentPassword,
                user.PasswordHash);

            if (!isValid)
            {
                ModelState.AddModelError(
                    "",
                    "Current password is incorrect.");

                return View(model);
            }

            // Prevent same password
            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError(
                    "",
                    "New password must be different from current password.");

                return View(model);
            }

            // Hash and save new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                model.NewPassword);

            _context.SaveChanges();

            TempData["Success"] =
                "Password changed successfully.";

            return RedirectToAction("Profile");
        }
    }
}