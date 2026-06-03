using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using Microsoft.AspNetCore.Hosting;
using SocietyManagementSystem.Models;
using SocietyManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Rotativa;
using ClosedXML.Excel;
using Rotativa.AspNetCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }
    private bool IsAdminLoggedIn()
    {
        return User.Identity.IsAuthenticated && User.IsInRole("Admin");
    }
    public IActionResult Dashboard()
    {
        if (!IsAdminLoggedIn())
            return RedirectToAction("Login", "Account");

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

        ViewBag.RecentComplaints =
            _context.Complaints
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

        return View();
    }
    public IActionResult Complaints(string search)
    {
        var complaints = _context.Complaints.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            complaints = complaints.Where(c =>
                c.Title.Contains(search) ||
                c.Category.Contains(search) ||
                c.Status.Contains(search) ||
                c.Priority.Contains(search));
        }

        return View(complaints.ToList());
    }
    public IActionResult ResolveComplaint(int id)
    {
        var complaint = _context.Complaints.Find(id);

        if (complaint == null)
            return NotFound();

        complaint.Status = "Resolved";
        complaint.ResolvedAt = DateTime.Now;
        complaint.UpdatedAt = DateTime.Now;

        _context.SaveChanges();

        TempData["Success"] = "Complaint Resolved Successfully";

        return RedirectToAction(nameof(Complaints));
    }
    public IActionResult RejectComplaint(int id)
    {
        var complaint = _context.Complaints.Find(id);

        if (complaint == null)
            return NotFound();

        complaint.Status = "Rejected";
        complaint.UpdatedAt = DateTime.Now;

        _context.SaveChanges();

        TempData["Success"] = "Complaint Rejected Successfully";

        return RedirectToAction(nameof(Complaints));
    }
    public IActionResult ExportComplaintPdf()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction("Login","Account");
        }

        var complaints =
            _context.Complaints.ToList();

        return new ViewAsPdf("ComplaintsPdf",complaints
        )
        {
            FileName = "ComplaintsReport.pdf",

            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,

            PageSize =Rotativa.AspNetCore.Options.Size.A4
        };
    }

    public IActionResult ExportComplaintExcel()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account"
            );
        }

        var complaints =
            _context.Complaints.ToList();

        using (var workbook =new XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets
                    .Add("Complaints");

            // HEADERS
            worksheet.Cell(1, 1).Value =
                "Title";

            worksheet.Cell(1, 2).Value =
                "Description";

            worksheet.Cell(1, 3).Value =
                "Category";

            worksheet.Cell(1, 4).Value =
                "Priority";

            worksheet.Cell(1, 5).Value =
                "Status";

            worksheet.Cell(1, 6).Value =
                "Created Date";

            int row = 2;

            foreach (var item in complaints)
            {
                worksheet.Cell(row, 1).Value =
                    item.Title;

                worksheet.Cell(row, 2).Value =
                    item.Description;

                worksheet.Cell(row, 3).Value =
                    item.Category;

                worksheet.Cell(row, 4).Value =
                    item.Priority;

                worksheet.Cell(row, 5).Value =
                    item.Status;

                worksheet.Cell(row, 6).Value =
                    item.CreatedAt.ToString("dd-MM-yyyy");

                row++;
            }

            using (var stream =
                new MemoryStream())
            {
                workbook.SaveAs(stream);

                var content =
                    stream.ToArray();

                return File(
                    content,

                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                    "ComplaintsReport.xlsx"
                );
            }
        }
    }

    public IActionResult Notices(string search)
    {
        var notices = _context.Notices.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            notices = notices.Where(n =>
                n.Title.Contains(search) ||
                n.Description.Contains(search) ||
                n.NoticeType.Contains(search));
        }

        return View(notices.ToList());
    }
    public IActionResult CreateNotice()
    {
        return View();
    }
    [HttpPost]
    public IActionResult CreateNotice(Notice notice)
    {
        notice.PostedBy = 1;
        notice.PostedDate = DateTime.Now;
        notice.IsActive = true;

        _context.Notices.Add(notice);
        _context.SaveChanges();

        TempData["Success"] = "Notice Added Successfully";

        return RedirectToAction(nameof(Notices));
    }
    public IActionResult EditNotice(int id)
    {
        var notice = _context.Notices.Find(id);

        if (notice == null)
            return NotFound();

        return View(notice);
    }
    [HttpPost]
    public IActionResult EditNotice(Notice notice)
    {
        var existingNotice =
            _context.Notices.Find(notice.NoticeId);

        if (existingNotice == null)
            return NotFound();

        existingNotice.Title = notice.Title;
        existingNotice.Description = notice.Description;
        existingNotice.NoticeType = notice.NoticeType;
        existingNotice.ExpiryDate = notice.ExpiryDate;

        _context.SaveChanges();

        TempData["Success"] = "Notice Updated Successfully";

        return RedirectToAction(nameof(Notices));
    }
    public IActionResult DeleteNotice(int id)
    {
        var notice = _context.Notices.Find(id);

        if (notice == null)
            return NotFound();

        _context.Notices.Remove(notice);

        _context.SaveChanges();

        TempData["Success"] = "Notice Deleted Successfully";

        return RedirectToAction(nameof(Notices));
    }
    public IActionResult ExportNoticePdf()
    {
        var notices = _context.Notices.ToList();

        return new ViewAsPdf("/Views/Admin/NoticePdf.cshtml",notices)
        {
            FileName = "NoticesReport.pdf"
        };
    }


    public IActionResult Maintenance(string search)
    {
        var maintenanceList = _context.Maintenance
            .Include(m => m.Resident)
            .ThenInclude(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            maintenanceList = maintenanceList.Where(m =>
                m.Month.ToString().Contains(search) ||
                m.Year.ToString().Contains(search) ||
                m.PaymentStatus.Contains(search) ||
                m.Amount.ToString().Contains(search));
        }

        return View(maintenanceList.ToList());
    }
    public IActionResult MaintenanceSettings()
    {
        var config = _context.MaintenanceConfigurations
            .FirstOrDefault();

        if (config == null)
        {
            config = new MaintenanceConfiguration();
        }

        return View(config);
    }
    [HttpPost]
    public IActionResult MaintenanceSettings(
    MaintenanceConfiguration model)
    {
        var config = _context.MaintenanceConfigurations
            .FirstOrDefault();

        if (config == null)
        {
            model.CreatedAt = DateTime.Now;

            _context.MaintenanceConfigurations
                .Add(model);
        }
        else
        {
            config.RepairMaintenance =
                model.RepairMaintenance;

            config.WaterCharges =
                model.WaterCharges;

            config.ServiceCharges =
                model.ServiceCharges;

            config.ElectricityCharges =
                model.ElectricityCharges;

            config.ParkingCharges =
                model.ParkingCharges;

            config.NonOccupancyCharges =
                model.NonOccupancyCharges;

            config.HouseKeepingCharges =
                model.HouseKeepingCharges;

            config.MunicipalTaxes =
                model.MunicipalTaxes;

            config.LateFeePercentage =
                model.LateFeePercentage;

            config.UpdatedAt =
                DateTime.Now;
        }

        _context.SaveChanges();

        TempData["Success"] =
            "Maintenance Settings Saved Successfully";

        return RedirectToAction(
            nameof(MaintenanceSettings));
    }

    public IActionResult CreateMaintenance()
    {
        return View();
    }
    [HttpPost]
    public IActionResult CreateMaintenance(Maintenance maintenance)
    {
        if (maintenance.Year < DateTime.Now.Year)
        {
            TempData["Error"] = "Enter valid year";
            return View(maintenance);
        }

        if (maintenance.Month < 1 || maintenance.Month > 12)
        {
            TempData["Error"] = "Invalid month selected";
            return View(maintenance);
        }

        // Prevent duplicate generation
        bool alreadyExists = _context.Maintenance.Any(m =>
            m.Month == maintenance.Month &&
            m.Year == maintenance.Year);

        if (alreadyExists)
        {
            TempData["Error"] =
                "Maintenance for this month already exists.";

            return View(maintenance);
        }

        // Get maintenance configuration
        var config = _context.MaintenanceConfigurations
            .FirstOrDefault(x => x.IsActive);

        if (config == null)
        {
            TempData["Error"] =
                "Please configure Maintenance Settings first.";

            return View(maintenance);
        }

        // Get all owners
        var owners = _context.Residents
            .Where(r => r.OwnerOrTenant == "Owner")
            .ToList();

        foreach (var owner in owners)
        {
            // Calculate current month's maintenance
            decimal currentAmount =
                config.RepairMaintenance +
                config.WaterCharges +
                config.ServiceCharges +
                config.ElectricityCharges +
                config.HouseKeepingCharges +
                config.MunicipalTaxes;

            // Add parking charges only if vehicle exists
            if (!string.IsNullOrWhiteSpace(owner.VehicleNumber))
            {
                currentAmount += config.ParkingCharges;
            }

            // Previous unpaid dues
            decimal previousDue = _context.Maintenance
                .Where(m =>
                    m.ResidentId == owner.ResidentId &&
                    m.PaymentStatus != "Paid")
                .Sum(m => m.TotalDue);

            // Late fee on outstanding dues
            decimal lateFee =
                previousDue * (config.LateFeePercentage / 100);

            // Final amount payable
            decimal totalDue =
                currentAmount +
                previousDue +
                lateFee;

            var newMaintenance = new Maintenance
            {
                ResidentId = owner.ResidentId,

                Month = maintenance.Month,
                Year = maintenance.Year,

                Amount = currentAmount,

                PreviousDue = previousDue,

                LateFee = lateFee,

                TotalDue = totalDue,

                DueDate = maintenance.DueDate,

                PaymentStatus = "Pending",

                CreatedAt = DateTime.Now
            };

            _context.Maintenance.Add(newMaintenance);
        }

        _context.SaveChanges();

        TempData["Success"] =
            "Maintenance generated successfully for all owners.";

        return RedirectToAction(nameof(Maintenance));
    }
    public IActionResult EditMaintenance(int id)
    {
        var maintenance =
            _context.Maintenance
            .FirstOrDefault(m => m.MaintenanceId == id);

        if (maintenance == null)
            return NotFound();

        return View(maintenance);
    }
    [HttpPost]
    public IActionResult EditMaintenance(Maintenance maintenance)
    {
        var existingMaintenance =
            _context.Maintenance
            .FirstOrDefault(m =>
                m.MaintenanceId ==
                maintenance.MaintenanceId);

        if (existingMaintenance == null)
            return NotFound();

        existingMaintenance.Amount =
            maintenance.Amount;

        existingMaintenance.DueDate =
            maintenance.DueDate;

        existingMaintenance.PaymentStatus =
            maintenance.PaymentStatus;

        existingMaintenance.PaidDate =
            maintenance.PaidDate;

        _context.SaveChanges();

        TempData["Success"] =
            "Maintenance Updated Successfully";

        return RedirectToAction(nameof(Maintenance));
    }

    public IActionResult MarkMaintenancePending(int id)
    {
        var maintenance =
            _context.Maintenance.Find(id);

        if (maintenance == null)
            return NotFound();

        maintenance.PaymentStatus = "Pending";
        maintenance.PaidDate = null;

        _context.SaveChanges();

        TempData["Success"] =
            "Payment reverted successfully";

        return RedirectToAction(nameof(Maintenance));
    }
    // GET
    public IActionResult MarkPaid(int id)
    {
        var maintenance = _context.Maintenance
            .Include(m => m.Resident)
            .ThenInclude(r => r.User)
            .FirstOrDefault(m => m.MaintenanceId == id);

        if (maintenance == null)
            return NotFound();

        return View(maintenance);
    }
    [HttpPost]
    public IActionResult MarkPaid(
    int id,
    string paymentMethod,
    string transactionReference,
    string remarks)
    {
        var maintenance = _context.Maintenance
            .FirstOrDefault(m => m.MaintenanceId == id);

        if (maintenance == null)
            return NotFound();

        maintenance.PaymentStatus = "Paid";
        maintenance.PaidDate = DateTime.Now;
        maintenance.PaymentMethod = paymentMethod;
        maintenance.TransactionReference = transactionReference;
        maintenance.Remarks = remarks;

        _context.SaveChanges();

        TempData["Success"] =
            "Maintenance marked as paid.";

        return RedirectToAction(nameof(Maintenance));
    }

    public IActionResult DeleteMaintenance(int id)
    {
        var maintenance =
            _context.Maintenance.Find(id);

        if (maintenance == null)
            return NotFound();

        _context.Maintenance.Remove(maintenance);

        _context.SaveChanges();

        TempData["Success"] =
            "Maintenance Deleted Successfully";

        return RedirectToAction(nameof(Maintenance));
    }

    public IActionResult ExportMaintenancePdf()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction("Login","Account");
        }

        var maintenance =
            _context.Maintenance.ToList();

        return new ViewAsPdf(
            "MaintenancePdf",
            maintenance
        )
        {
            FileName = "MaintenanceReport.pdf",

            PageOrientation =
                Rotativa.AspNetCore.Options.Orientation.Landscape,

            PageSize =
                Rotativa.AspNetCore.Options.Size.A4
        };
    }

    public IActionResult ExportMaintenanceExcel()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction("Login","Account");
        }

        var maintenanceList =
            _context.Maintenance.ToList();

        using (var workbook =
            new XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets
                    .Add("Maintenance");

            // HEADERS
            worksheet.Cell(1, 1).Value =
                "Month";

            worksheet.Cell(1, 2).Value =
                "Year";

            worksheet.Cell(1, 3).Value =
                "Amount";

            worksheet.Cell(1, 4).Value =
                "Status";

            worksheet.Cell(1, 5).Value =
                "Due Date";

            worksheet.Cell(1, 6).Value =
                "Remarks";

            int row = 2;

            foreach (var item in maintenanceList)
            {
                worksheet.Cell(row, 1).Value =
                    item.Month;

                worksheet.Cell(row, 2).Value =
                    item.Year;

                worksheet.Cell(row, 3).Value =
                    item.Amount;

                worksheet.Cell(row, 4).Value =
                    item.PaymentStatus;

                worksheet.Cell(row, 5).Value =
                    item.DueDate.ToString("dd-MM-yyyy");

                worksheet.Cell(row, 6).Value =
                    item.Remarks;

                row++;
            }

            using (var stream =
                new MemoryStream())
            {
                workbook.SaveAs(stream);

                var content =
                    stream.ToArray();

                return File(
                    content,

                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                    "MaintenanceReport.xlsx"
                );
            }
        }
    }
    public IActionResult Residents(string search)
    {
        var residents = _context.Residents.Include(r => r.User).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            residents = residents.Where(r =>
                r.FlatNumber.ToString().Contains(search) ||
                r.Wing.Contains(search) ||
                r.OwnerOrTenant.Contains(search) ||
                r.ContactPhone.Contains(search));
        }

        return View(residents.ToList());
    }
    public IActionResult EditResident(int id)
    {
        var resident = _context.Residents.Find(id);

        if (resident == null)
            return NotFound();

        return View(resident);
    }

    [HttpPost]
    public IActionResult EditResident(Resident resident)
    {
        _context.Residents.Update(resident);

        _context.SaveChanges();

        TempData["Success"] =
            "Resident Updated Successfully";

        return RedirectToAction(nameof(Residents));
    }

    public IActionResult DeleteResident(int id)
    {
        var resident = _context.Residents.Find(id);

        if (resident == null)
            return NotFound();

        try
        {
            _context.Residents.Remove(resident);

            _context.SaveChanges();

            TempData["Success"] =
                "Resident Deleted Successfully";
        }
        catch
        {
            TempData["Error"] =
                "Cannot delete resident because related records exist.";
        }

        return RedirectToAction(nameof(Residents));
    }

    public IActionResult ApproveResident(int id)
    {
        var resident = _context.Residents.Find(id);

        if (resident == null)
            return NotFound();

        var user = _context.Users.Find(resident.UserId);

        if (user == null)
            return NotFound();

        user.ApprovalStatus = "Approved";

        _context.SaveChanges();

        TempData["Success"] = "Resident Approved Successfully";

        return RedirectToAction(nameof(Residents));
    }

    public IActionResult RejectResident(int id)
    {
        var resident = _context.Residents.Find(id);

        if (resident == null)
            return NotFound();

        var user = _context.Users.Find(resident.UserId);

        if (user == null)
            return NotFound();

        user.ApprovalStatus = "Rejected";

        _context.SaveChanges();

        TempData["Success"] = "Resident Rejected Successfully";

        return RedirectToAction(nameof(Residents));
    }
    public IActionResult ExportResidentPdf()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account"
            );
        }

        var residents =
            _context.Residents.ToList();

        return new ViewAsPdf(
            "ResidentsPdf",
            residents
        )
        {
            FileName = "ResidentsReport.pdf",

            PageOrientation =
                Rotativa.AspNetCore.Options.Orientation.Landscape,

            PageSize =
                Rotativa.AspNetCore.Options.Size.A4
        };
    }

    public IActionResult ExportResidentExcel()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Auth"
            );
        }

        var residents =
            _context.Residents.ToList();

        using (var workbook =
            new XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets
                    .Add("Residents");

            // HEADERS
            worksheet.Cell(1, 1).Value =
                "Flat";

            worksheet.Cell(1, 2).Value =
                "Wing";

            worksheet.Cell(1, 3).Value =
                "Floor";

            worksheet.Cell(1, 4).Value =
                "Owner/Tenant";

            worksheet.Cell(1, 5).Value =
                "Phone";

            worksheet.Cell(1, 6).Value =
                "Vehicle";

            worksheet.Cell(1, 7).Value =
                "Status";

            int row = 2;

            foreach (var item in residents)
            {
                worksheet.Cell(row, 1).Value =
                    item.FlatNumber;

                worksheet.Cell(row, 2).Value =
                    item.Wing;

                worksheet.Cell(row, 3).Value =
                    item.FloorNumber;

                worksheet.Cell(row, 4).Value =
                    item.OwnerOrTenant;

                worksheet.Cell(row, 5).Value =
                    item.ContactPhone;

                worksheet.Cell(row, 6).Value =
                    item.VehicleNumber;

                //worksheet.Cell(row, 7).Value =
                //    item.ApprovalStatus == "Approved" || "Reject"
                //    ? "Approved"
                //    : "Pending";

                row++;
            }

            using (var stream =
                new MemoryStream())
            {
                workbook.SaveAs(stream);

                var content =
                    stream.ToArray();

                return File(
                    content,

                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                    "ResidentsReport.xlsx"
                );
            }
        }
    }

    public IActionResult Security(string search)
    {
        var guards = _context.SecurityGuards.Include(s => s.User).Where(s => s.User.Role == "SecurityGuard")
            .AsQueryable();

        // SEARCH
        if (!string.IsNullOrEmpty(search))
        {
            guards = guards.Where(s =>

                s.User.FullName.Contains(search) || s.ShiftTiming.Contains(search) || s.Address.Contains(search)
            );
        }

        return View(guards.ToList());
    }
    private void LoadSecurityUsers()
    {
        ViewBag.Users = _context.Users
            .Where(u => u.Role == "SecurityGuard")
            .ToList();
    }
    public IActionResult CreateSecurity()
    {
        return View();
    }
    [HttpPost]
    public IActionResult CreateSecurity(CreateSecurityGuardViewModel model)
    {
        var existingUser =
                _context.Users
                .FirstOrDefault(u =>
                    u.Email == model.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                "Email",
                "Email already registered.");

            return View(model);
        }
        var existingempcode =
                _context.SecurityGuards
                .FirstOrDefault(u =>
                    u.EmployeeCode == model.EmployeeCode);

        if (existingempcode != null)
        {
            ModelState.AddModelError(
                "EmployeeCode",
                "EmployeeCode already Exist.");

            return View(model);
        }
        // Phone number validation
        if (!System.Text.RegularExpressions.Regex
            .IsMatch(model.PhoneNumber ?? "",
            @"^[6-9]\d{9}$"))
        {
            ModelState.AddModelError(
                "PhoneNumber",
                "Enter valid 10 digit mobile number.");

            return View(model);
        }


        // Password validation
        if (!System.Text.RegularExpressions.Regex
            .IsMatch(model.Password ?? "",
            @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&]).{6,}$"))
        {
            ModelState.AddModelError(
                "Password",
                "Password must contain at least 6 characters, one letter, one number and one special character.");

            return View(model);
        }
        if (model.Salary <= 0)
        {
            ModelState.AddModelError(
                "Salary",
                "Salary must be greater than 0.");

            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.AadhaarNumber))
        {
            ModelState.AddModelError("AadharNumber",
                "Aadhar Number is required.");
        }
        else if (!Regex.IsMatch(model.AadhaarNumber, @"^\d{12}$"))
        {
            ModelState.AddModelError("AadharNumber",
                "Aadhar Number must contain exactly 12 digits.");
        }

        var user = new User
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), // hash later
            Role = "SecurityGuard",
            ApprovalStatus = "Approved",
            IsActive = true
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        var guard = new SecurityGuard
        {
            UserId = user.UserId,
            EmployeeCode = model.EmployeeCode,
            ShiftTiming = model.ShiftTiming,
            Address = model.Address,
            AadhaarNumber = model.AadhaarNumber,
            Salary = model.Salary,
            JoiningDate = DateTime.Now
        };

        _context.SecurityGuards.Add(guard);
        _context.SaveChanges();

        TempData["Success"] = "Security Guard Added Successfully";

        return RedirectToAction("Security");
    }
    public IActionResult EditSecurity(int id)
    {
        var guard = _context.SecurityGuards
            .Include(s => s.User)
            .FirstOrDefault(s => s.GuardId == id);

        if (guard == null)
        {
            return NotFound();
        }

        var model = new EditSecurityGuardViewModel
        {
            GuardId = guard.GuardId,
            UserId = guard.UserId,

            FullName = guard.User.FullName,
            Email = guard.User.Email,
            PhoneNumber = guard.User.PhoneNumber,

            EmployeeCode = guard.EmployeeCode,
            ShiftTiming = guard.ShiftTiming,
            Address = guard.Address,
            AadhaarNumber = guard.AadhaarNumber,
            Salary = guard.Salary
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult EditSecurity(EditSecurityGuardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var guard = _context.SecurityGuards
            .FirstOrDefault(s => s.GuardId == model.GuardId);

        if (guard == null)
        {
            return NotFound();
        }

        var user = _context.Users
            .FirstOrDefault(u => u.UserId == model.UserId);

        if (user == null)
        {
            return NotFound();
        }

        // Update User
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;

        // Update Security Guard
        guard.EmployeeCode = model.EmployeeCode;
        guard.ShiftTiming = model.ShiftTiming;
        guard.Address = model.Address;
        guard.AadhaarNumber = model.AadhaarNumber;
        guard.Salary = model.Salary;

        _context.SaveChanges();

        TempData["Success"] =
            "Security Guard Updated Successfully";

        return RedirectToAction("Security");
    }

    public IActionResult Visitor(string search)
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction("Login","Account");
        }

        var visitors =_context.Visitors.AsQueryable();

        // SEARCH
        if (!string.IsNullOrEmpty(search))
        {
            visitors = visitors.Where(
                v =>v.VisitorName.Contains(search) || v.PhoneNumber.Contains(search) || v.VehicleNumber.Contains(search) || v.Purpose.Contains(search)

            );
        }

        return View(
            visitors.ToList()
        );
    }

    public IActionResult ExportVisitorPdf()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction("Login","Account");
        }

        var visitors =
            _context.Visitors.ToList();

        return new ViewAsPdf("VisitorsPdf",visitors)
        {
            FileName = "VisitorsReport.pdf",

            PageOrientation =
                Rotativa.AspNetCore.Options.Orientation.Landscape,

            PageSize =
                Rotativa.AspNetCore.Options.Size.A4
        };
    }

    public IActionResult ExportVisitorExcel()
    {
        if (!IsAdminLoggedIn())
        {
            return RedirectToAction("Login","Account");
        }

        var visitors =
            _context.Visitors.ToList();

        using (var workbook =
            new XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets
                    .Add("Visitors");

            // HEADERS
            worksheet.Cell(1, 1).Value =
                "Visitor Name";

            worksheet.Cell(1, 2).Value =
                "Phone";

            worksheet.Cell(1, 3).Value =
                "Purpose";

            worksheet.Cell(1, 4).Value =
                "Vehicle";

            worksheet.Cell(1, 5).Value =
                "Created Date";

            int row = 2;

            foreach (var item in visitors)
            {
                worksheet.Cell(row, 1).Value =
                    item.VisitorName;

                worksheet.Cell(row, 2).Value =
                    item.PhoneNumber;

                worksheet.Cell(row, 3).Value =
                    item.Purpose;

                worksheet.Cell(row, 4).Value =item.VehicleNumber;

                worksheet.Cell(row, 5).Value =
                    item.CreatedAt.ToString("dd-MM-yyyy");

                row++;
            }

            using (var stream =
                new MemoryStream())
            {
                workbook.SaveAs(stream);

                var content =
                    stream.ToArray();

                return File(
                    content,

                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                    "VisitorsReport.xlsx"
                );
            }
        }
    }
}