using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using Microsoft.AspNetCore.Hosting;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using iTextSharp.text;
using iTextSharp.text.pdf;

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

        public IActionResult dashboard()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("login", "account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("login", "account");

            var user = _context.Users
                .FirstOrDefault(u => u.UserId == resident.UserId);

            var recentNotices = _context.Notices
            .Where(n => n.IsActive)
            .OrderByDescending(n => n.PostedDate)
            .Take(5)
            .ToList();

            ViewBag.RecentNotices = recentNotices;

            ViewBag.name = user?.FullName;
            ViewBag.flatnumber = resident.FlatNumber;
            ViewBag.wing = resident.Wing;
            ViewBag.status = user?.ApprovalStatus ?? "active";

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
        public IActionResult MyMaintenance()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var maintenanceList = _context.Maintenance
                .Where(m => m.ResidentId == resident.ResidentId)
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();

            return View(maintenanceList);
        }
        public IActionResult MaintenanceDetails(int id)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var maintenance = _context.Maintenance
                .FirstOrDefault(m =>
                    m.MaintenanceId == id &&
                    m.ResidentId == resident.ResidentId);

            if (maintenance == null)
                return NotFound();

            return View(maintenance);
        }

        public IActionResult DownloadMaintenanceBill(int id)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var maintenance = _context.Maintenance
                .Include(m => m.Resident)
                .ThenInclude(r => r.User)
                .FirstOrDefault(m =>
                    m.MaintenanceId == id &&
                    m.ResidentId == resident.ResidentId);

            if (maintenance == null)
                return NotFound();

            var config = _context.MaintenanceConfigurations
                .FirstOrDefault(x => x.IsActive);

            using (MemoryStream ms = new MemoryStream())
            {
                Document document =
                    new Document(PageSize.A4, 25, 25, 25, 25);

                PdfWriter.GetInstance(document, ms);

                document.Open();

                Font titleFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        18);

                Font headingFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        12);

                Font normalFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA,
                        10);

                Font boldFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        10);

                Font totalFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        12);

                // ====================================
                // SOCIETY HEADER
                // ====================================

                Paragraph systemName =
                    new Paragraph(
                        " Akurli Sai Shraddha Co-operative Housing Society",
                        titleFont);

                systemName.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(systemName);

                Paragraph societyName =
                    new Paragraph(
                        " 39/RSC-4, Akurli, Kandivali (East), Mumbai - 400101 ",
                        headingFont);

                societyName.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(societyName);

                Paragraph address =
                    new Paragraph(
                        "",
                        normalFont);

                address.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(address);

                document.Add(new Paragraph(" "));

                string billNo =
                    " BILL" +
                    maintenance.MaintenanceId
                    .ToString(" D5 ");

                // ====================================
                // BILL + RESIDENT DETAILS
                // ====================================

                PdfPTable detailsTable =
                    new PdfPTable(2);

                detailsTable.WidthPercentage = 100;

                detailsTable.SetWidths(
                    new float[] { 50, 50 });

                PdfPCell billCell =
                    new PdfPCell();

                billCell.AddElement(
                    new Paragraph(
                        "  BILL DETAILS",
                        headingFont));

                billCell.AddElement(
                    new Paragraph(
                        "  Bill No : " + billNo));

                billCell.AddElement(
                    new Paragraph(
                        "  Generated Date : "
                        + DateTime.Now.ToString("dd-MM-yyyy")));

                billCell.AddElement(
                    new Paragraph(
                        "  Due Date : "
                        + maintenance.DueDate
                            .ToString("dd-MM-yyyy")));
                billCell.AddElement(new Paragraph());

                PdfPCell residentCell =
                    new PdfPCell();

                residentCell.AddElement(
                    new Paragraph(
                        "  RESIDENT DETAILS",
                        headingFont));

                residentCell.AddElement(
                    new Paragraph(
                        "  Resident : "
                        + maintenance.Resident.User.FullName));

                residentCell.AddElement(
                    new Paragraph(
                        "  Flat No : "
                        + maintenance.Resident.Wing
                        + "-"
                        + maintenance.Resident.FlatNumber));

                residentCell.AddElement(
                    new Paragraph(
                        "  Month : "
                        + System.Globalization.CultureInfo
                            .CurrentCulture
                            .DateTimeFormat
                            .GetMonthName(maintenance.Month)
                        + " "
                        + maintenance.Year));
                residentCell.AddElement(new Paragraph());
                document.Add(new Paragraph(" "));

                detailsTable.AddCell(billCell);
                detailsTable.AddCell(residentCell);

                document.Add(detailsTable);

                document.Add(new Paragraph(" "));

                // ====================================
                // BILL TITLE
                // ====================================

                Paragraph billTitle =
                    new Paragraph(
                        "MAINTENANCE BILL",
                        headingFont);

                billTitle.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(billTitle);

                document.Add(new Paragraph(" "));

                // ====================================
                // CHARGES TABLE
                // ====================================

                PdfPTable table =
                    new PdfPTable(2);

                table.WidthPercentage = 100;

                table.SetWidths(
                    new float[] { 70, 30 });

                PdfPCell h1 =
                    new PdfPCell(
                        new Phrase(
                            "Particulars",
                            boldFont));

                PdfPCell h2 =
                    new PdfPCell(
                        new Phrase(
                            "Amount (₹)",
                            boldFont));

                h1.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                h2.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                table.AddCell(h1);
                table.AddCell(h2);

                table.AddCell("  Repair Maintenance");
                table.AddCell(
                    config.RepairMaintenance
                    .ToString("N2"));

                table.AddCell("  Water Charges");
                table.AddCell(
                    config.WaterCharges
                    .ToString("N2"));

                table.AddCell("  Service Charges");
                table.AddCell(
                    config.ServiceCharges
                    .ToString("N2"));

                table.AddCell("  Electricity Charges");
                table.AddCell(
                    config.ElectricityCharges
                    .ToString("N2"));

                if (!string.IsNullOrWhiteSpace(
                    maintenance.Resident.VehicleNumber))
                {
                    table.AddCell("  Parking Charges");
                    table.AddCell(
                        config.ParkingCharges
                        .ToString("N2"));
                }

                table.AddCell("  House Keeping");
                table.AddCell(
                    config.HouseKeepingCharges
                    .ToString("N2"));

                table.AddCell("  Municipal Taxes");
                table.AddCell(
                    config.MunicipalTaxes
                    .ToString("N2"));

                table.AddCell("  Current Maintenance");
                table.AddCell(
                    maintenance.Amount
                    .ToString("N2"));

                table.AddCell("  Previous Due");
                table.AddCell(
                    maintenance.PreviousDue
                    .ToString("N2"));

                table.AddCell("  Late Fee");
                table.AddCell(
                    maintenance.LateFee
                    .ToString("N2"));
                
                PdfPCell totalText =
                    new PdfPCell(
                        new Phrase(
                            "TOTAL PAYABLE",
                            totalFont));

                PdfPCell totalAmount =
                    new PdfPCell(
                        new Phrase(
                            maintenance.TotalDue
                            .ToString("N2"),
                            totalFont));

                totalText.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                totalAmount.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                table.AddCell(totalText);
                table.AddCell(totalAmount);

                document.Add(table);

                document.Add(new Paragraph(" "));

                // ====================================
                // STATUS
                // ====================================

                PdfPTable statusTable =
                    new PdfPTable(1);

                statusTable.WidthPercentage = 100;

                statusTable.AddCell(
                    new Phrase(
                        "STATUS : "
                        + maintenance.PaymentStatus,
                        boldFont));

                document.Add(statusTable);

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));
                Paragraph note =
                    new Paragraph(
                        "Please pay before due date to avoid additional late fee charges.",
                        normalFont);

                note.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(note);

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));
                // ====================================
                // SIGNATURE
                // ====================================

                PdfPTable signTable =
                    new PdfPTable(1);

                signTable.WidthPercentage = 100;

                PdfPCell signCell =
                    new PdfPCell(
                        new Phrase(
                            "Authorized Signatory"));

                signCell.Border =
                    Rectangle.TOP_BORDER;

                signCell.HorizontalAlignment =
                    Element.ALIGN_RIGHT;

                signTable.AddCell(signCell);

                document.Add(signTable);

                document.Close();

                return File(
                    ms.ToArray(),
                    "application/pdf",
                    $"MaintenanceBill_{billNo}.pdf");
            }
        }
        public IActionResult DownloadReceipt(int id)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            if (resident == null)
                return RedirectToAction("Login", "Account");

            var maintenance = _context.Maintenance
                .Include(m => m.Resident)
                .ThenInclude(r => r.User)
                .FirstOrDefault(m =>
                    m.MaintenanceId == id &&
                    m.ResidentId == resident.ResidentId);

            if (maintenance == null)
                return NotFound();

            if (maintenance.PaymentStatus != "Paid")
            {
                TempData["Error"] =
                    "Receipt is available only for paid maintenance.";

                return RedirectToAction(nameof(MyMaintenance));
            }

            var config = _context.MaintenanceConfigurations
                .FirstOrDefault(x => x.IsActive);

            using (MemoryStream ms = new MemoryStream())
            {
                Document document =
                    new Document(PageSize.A4, 40, 40, 40, 40);

                PdfWriter.GetInstance(document, ms);

                document.Open();

                Font titleFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        20);

                Font headingFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        14);

                Font normalFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA,
                        11);

                Font boldFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        11);

                // ===========================
                // HEADER
                // ===========================

                Paragraph systemName = new Paragraph("Sai Shraddha Co-operative Housing Society", titleFont);

                systemName.Alignment = Element.ALIGN_CENTER;

                document.Add(systemName);

                Paragraph societyName = new Paragraph(
                    "39/RSC-4, Akurli, Kandivali (East), Mumbai - 400101 ",
                    headingFont);

                societyName.Alignment = Element.ALIGN_CENTER;

                document.Add(societyName);

                Paragraph address = new Paragraph(
                    "",
                    normalFont);

                address.Alignment = Element.ALIGN_CENTER;

                document.Add(address);

                document.Add(new Paragraph(" "));

                Paragraph receiptTitle =
                    new Paragraph(
                        "Maintenance Payment Receipt",
                        headingFont);

                receiptTitle.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(receiptTitle);

                document.Add(new Paragraph(" "));

                string receiptNo =
                    "RCP" +
                    maintenance.MaintenanceId
                    .ToString("D5");

                // ===========================
                // RESIDENT DETAILS
                // ===========================

                PdfPTable detailsTable =new PdfPTable(2);

                detailsTable.WidthPercentage = 100;

                detailsTable.SetWidths(
                    new float[] { 50, 50 });

                PdfPCell receiptCell =
                    new PdfPCell();

                receiptCell.AddElement(
                    new Paragraph(
                        "  RECEIPT DETAILS",
                        headingFont));

                receiptCell.AddElement(
                    new Paragraph(
                        "  Receipt No : "
                        + receiptNo));

                receiptCell.AddElement(
                    new Paragraph(
                        "  Receipt Date : "
                        + maintenance.PaidDate?
                            .ToString("dd-MM-yyyy")));

                receiptCell.AddElement(
                    new Paragraph(
                        "  Paid Date : "
                        + maintenance.PaidDate?
                            .ToString("dd-MM-yyyy")));

                PdfPCell residentCell =
                    new PdfPCell();

                residentCell.AddElement(
                    new Paragraph(
                        "  RESIDENT DETAILS",
                        headingFont));

                residentCell.AddElement(
                    new Paragraph(
                        "  Resident : "
                        + maintenance.Resident.User.FullName));

                residentCell.AddElement(
                    new Paragraph(
                        "  Flat No : "
                        + maintenance.Resident.Wing
                        + "-"
                        + maintenance.Resident.FlatNumber));

                residentCell.AddElement(
                    new Paragraph(
                        "  Month : "
                        + System.Globalization.CultureInfo
                            .CurrentCulture
                            .DateTimeFormat
                            .GetMonthName(
                                maintenance.Month)
                        + " "
                        + maintenance.Year));

                detailsTable.AddCell(receiptCell);
                detailsTable.AddCell(residentCell);

                document.Add(detailsTable);

                document.Add(new Paragraph(" "));

                // ===========================
                // MAINTENANCE BREAKDOWN
                // ===========================

                Paragraph breakdownTitle =
    new Paragraph(
        "MAINTENANCE BREAKDOWN",
        headingFont);

                breakdownTitle.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(breakdownTitle);

                document.Add(new Paragraph(" "));
                PdfPTable table =
                    new PdfPTable(2);

                table.WidthPercentage = 100;

                table.SetWidths(
                    new float[] { 70, 30 });

                PdfPCell h1 =
                   new PdfPCell(
                       new Phrase(
                           "Particulars",
                           boldFont));

                PdfPCell h2 =
                    new PdfPCell(
                        new Phrase(
                            "Amount (₹)",
                            boldFont));

                h1.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                h2.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                table.AddCell(h1);
                table.AddCell(h2);

                table.AddCell(
                     "  Repair Maintenance");

                table.AddCell(
                    config.RepairMaintenance
                    .ToString("N2"));

                table.AddCell(
                    "  Water Charges");

                table.AddCell(
                    config.WaterCharges
                    .ToString("N2"));

                table.AddCell(
                    "  Service Charges");

                table.AddCell(
                    config.ServiceCharges
                    .ToString("N2"));

                table.AddCell(
                    "  Electricity Charges");

                table.AddCell(
                    config.ElectricityCharges
                    .ToString("N2"));

                if (!string.IsNullOrWhiteSpace(
                    maintenance.Resident.VehicleNumber))
                {
                    table.AddCell(
                        "  Parking Charges");

                    table.AddCell(
                        config.ParkingCharges
                        .ToString("N2"));
                }

                table.AddCell(
                    "  House Keeping");

                table.AddCell(
                    config.HouseKeepingCharges
                    .ToString("N2"));

                table.AddCell(
                    "  Municipal Taxes");

                table.AddCell(
                    config.MunicipalTaxes
                    .ToString("N2"));

                table.AddCell(
                    "  Current Maintenance");

                table.AddCell(
                    maintenance.Amount
                    .ToString("N2"));

                table.AddCell(
                    "  Previous Due");

                table.AddCell(
                    maintenance.PreviousDue
                    .ToString("N2"));

                table.AddCell(
                    "  Late Fee");

                table.AddCell(
                    maintenance.LateFee
                    .ToString("N2"));

                PdfPCell totalText =
                    new PdfPCell(
                        new Phrase(
                            "TOTAL PAID",
                            boldFont));

                PdfPCell totalAmount =
                    new PdfPCell(
                        new Phrase(
                            maintenance.TotalDue
                            .ToString("N2"),
                            boldFont));

                totalText.HorizontalAlignment =
    Element.ALIGN_CENTER;

                totalAmount.HorizontalAlignment =
                    Element.ALIGN_CENTER;

                table.AddCell(totalText);
                table.AddCell(totalAmount);

                document.Add(table);

                document.Add(new Paragraph(" "));

                // ===========================
                // PAYMENT DETAILS
                // ===========================

                document.Add(
                    new Paragraph(
                        "Payment Information",
                        headingFont));

                document.Add(
                    new Paragraph(
                        "Payment Method : "
                        + maintenance.PaymentMethod));

                document.Add(
                    new Paragraph(
                        "Transaction Reference : "
                        + maintenance.TransactionReference));

                document.Add(
                    new Paragraph(
                        "Paid Date : "
                        + maintenance.PaidDate?.ToString("dd-MM-yyyy")));

                if (!string.IsNullOrWhiteSpace(
                    maintenance.Remarks))
                {
                    document.Add(
                        new Paragraph(
                            "Remarks : "
                            + maintenance.Remarks));
                }

                document.Add(new Paragraph(" "));

                // ===========================
                // FOOTER
                // ===========================

                document.Add(new Paragraph(" "));

                Paragraph footer =
                    new Paragraph(
                        "This is a computer generated receipt.\nThank you for your payment.",
                        normalFont);

                footer.Alignment =
                    Element.ALIGN_CENTER;

                document.Add(footer);

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));

                PdfPTable signTable =
                    new PdfPTable(2);

                signTable.WidthPercentage = 100;

                signTable.AddCell(
                    new PdfPCell(
                        new Phrase(
                            "Authorized Signatory"))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment =
                            Element.ALIGN_LEFT
                    });

                signTable.AddCell(
                    new PdfPCell(
                        new Phrase(
                            "Resident Signature"))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment =
                            Element.ALIGN_RIGHT
                    });

                document.Add(signTable);

                document.Close();

                return File(
                    ms.ToArray(),
                    "application/pdf",
                    $"Receipt_{receiptNo}.pdf");
            }
        }
        public IActionResult Notices()
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            var notices = _context.Notices
                .Where(n =>
                    n.IsActive &&
                    (n.ExpiryDate == null ||
                     n.ExpiryDate >= DateTime.Today))
                .OrderByDescending(n => n.PostedDate)
                .ToList();

            ViewBag.ReadNotices = _context.NoticeReads
                .Where(x => x.ResidentId == resident.ResidentId)
                .Select(x => x.NoticeId)
                .ToList();

            return View(notices);
        }
        public IActionResult NoticeDetails(int id)
        {
            if (!IsResidentLoggedIn())
                return RedirectToAction("Login", "Account");

            var resident = GetCurrentResident();

            var notice = _context.Notices
                .Include(n => n.User)
                .FirstOrDefault(n => n.NoticeId == id);

            if (notice == null)
                return NotFound();

            bool alreadyRead =
                _context.NoticeReads.Any(x =>
                    x.NoticeId == id &&
                    x.ResidentId == resident.ResidentId);

            if (!alreadyRead)
            {
                _context.NoticeReads.Add(
                    new NoticeRead
                    {
                        NoticeId = id,
                        ResidentId = resident.ResidentId,
                        ReadAt = DateTime.Now
                    });

                _context.SaveChanges();
            }

            return View(notice);
        }
    }
    
}