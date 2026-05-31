using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using Microsoft.AspNetCore.Hosting;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace SocietyManagementSystem.Controllers
{
    public class ResidentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ResidentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool IsResidentLoggedIn()
        {
            return User.Identity.IsAuthenticated && User.IsInRole("Resident");
        }

        private Resident GetCurrentResident()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            int userId = int.Parse(userIdClaim);

            return _context.Residents.FirstOrDefault(r => r.UserId == userId);
        }

        public IActionResult Dashboard()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

            ViewBag.Name = user?.FullName;
            ViewBag.FlatNumber = resident.FlatNumber;
            ViewBag.Wing = resident.Wing;
            ViewBag.Status = user?.ApprovalStatus ?? "Active";

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
                Description = model.Description,
                Category = model.Category,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Complaints.Add(complaint);
            _context.SaveChanges();

            if (model.AttachmentFile != null && model.AttachmentFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "complaints");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AttachmentFile.FileName);

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.AttachmentFile.CopyTo(stream);
                }

                ComplaintAttachment attachment = new ComplaintAttachment
                {
                    ComplaintId = complaint.ComplaintId,
                    FilePath = "/uploads/complaints/" + fileName,
                    UploadedAt = DateTime.Now
                };

                _context.ComplaintAttachments.Add(attachment);
                _context.SaveChanges();
            }

            TempData["Success"] = "Complaint submitted successfully.";

            return RedirectToAction("SubmitComplaint");
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
            {
                return RedirectToAction("Login", "Account");
            }

            var resident = GetCurrentResident();

            if (resident == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var entry = _context.VisitorEntries
                .FirstOrDefault(v =>
                    v.EntryId == id &&
                    v.ResidentId == resident.ResidentId);

            if (entry == null)
            {
                return NotFound();
            }

            entry.ApprovalStatus = "Approved";

            _context.SaveChanges();

            return RedirectToAction("VisitorRequests");
        }

        // Reject Visitor
        public IActionResult RejectVisitor(int id)
        {
            if (!IsResidentLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var resident = GetCurrentResident();

            if (resident == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var entry = _context.VisitorEntries
                .FirstOrDefault(v =>
                    v.EntryId == id &&
                    v.ResidentId == resident.ResidentId);

            if (entry == null)
            {
                return NotFound();
            }

            entry.ApprovalStatus = "Rejected";

            _context.SaveChanges();

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
            string email = User.FindFirst(ClaimTypes.Email)?.Value;

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

            return RedirectToAction("VisitorRequests");
        }
        [HttpGet]
        public IActionResult ApproveVisitorFromNotification(int entryId)
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var resident = _context.Residents.FirstOrDefault(r => r.UserId == user.UserId);

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var entry = _context.VisitorEntries
                .FirstOrDefault(v =>
                    v.EntryId == entryId &&
                    v.ResidentId == resident.ResidentId);

            if (entry == null)
                return RedirectToAction("VisitorRequests", "Resident");

            entry.ApprovalStatus = "Approved";

            _context.SaveChanges();

            return RedirectToAction("VisitorRequests", "Resident");
        }

        [HttpGet]
        public IActionResult RejectVisitorFromNotification(int entryId)
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var resident = _context.Residents.FirstOrDefault(r => r.UserId == user.UserId);

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var entry = _context.VisitorEntries
                .FirstOrDefault(v =>
                    v.EntryId == entryId &&
                    v.ResidentId == resident.ResidentId);

            if (entry == null)
                return RedirectToAction("VisitorRequests", "Resident");

            entry.ApprovalStatus = "Rejected";

            _context.SaveChanges();

            return RedirectToAction("VisitorRequests", "Resident");
        }

        public IActionResult Profile()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();


            if (resident == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ResidentProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,

                FlatNumber = resident.FlatNumber,
                Wing = resident.Wing,
                FloorNumber = resident.FloorNumber,

                OwnerOrTenant = resident.OwnerOrTenant,
                FamilyMembersCount = resident.FamilyMembersCount,

                VehicleNumber = resident.VehicleNumber,
                MoveInDate = resident.MoveInDate
            };

            return View(model);
        }
        public IActionResult EditProfile()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                VehicleNumber = resident.VehicleNumber,
                FamilyMembersCount = resident.FamilyMembersCount
            };

            return View(model);
        }
        [HttpPost]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            resident.VehicleNumber = model.VehicleNumber;
            resident.FamilyMembersCount = model.FamilyMembersCount;

            _context.SaveChanges();

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction("Profile");
        }
        public IActionResult ChangePassword()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

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
        public IActionResult GetDashboardStats()
        {
            if (!IsResidentLoggedIn())
                return Unauthorized();

            DateTime today = DateTime.Today;

            var stats = new
            {
                totalcomplaint = _context.Complaints
                    .Count(v => v.Status == "Resolved"),

                pendingcomplaint = _context.Complaints
                    .Count(v => v.Status == "Pending"),

                totalvisitors = _context.VisitorEntries
                    .Count(v => v.ApprovalStatus == "Approved")

                //DeniedVisitors = _context.VisitorEntries
                //    .Count(v => v.ApprovalStatus == "Rejected"),

            };

            return Json(stats);
        }
    }
}