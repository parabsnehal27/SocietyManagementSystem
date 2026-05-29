using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public IActionResult Index(string search)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var maintenanceList =
                _context.Maintenance.AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                maintenanceList =
                    maintenanceList.Where(m =>

                        m.Month.ToString()
                            .Contains(search)

                        ||

                        m.Year.ToString()
                            .Contains(search)

                        ||

                        m.PaymentStatus
                            .Contains(search)

                        ||

                        m.Amount.ToString()
                            .Contains(search)

                    );
            }

            return View(
                maintenanceList.ToList()
            );
        }

        public IActionResult ExportPdf()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
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

        public IActionResult ExportExcel()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
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

        //markpending
        public IActionResult MarkPending(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var maintenance =
                _context.Maintenance.Find(id);

            if (maintenance == null)
            {
                return NotFound();
            }

            maintenance.PaymentStatus =
                "pending";

            maintenance.PaidDate = null;

            _context.SaveChanges();

            TempData["Success"] =
                "Payment reverted successfully";

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

            

            maintenance.PaymentStatus = "pending";

            maintenance.CreatedAt = DateTime.Now;
            maintenance.ResidentId = 2;
            _context.Maintenance.Add(maintenance);

            _context.SaveChanges();

            TempData["Success"] = "Maintenance Added Successfully";

            return RedirectToAction("Index");
        }
    }
}