using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        public IActionResult Index(string search)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var visitors =
                _context.Visitors.AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                visitors = visitors.Where(v =>

                    v.VisitorName.Contains(search)

                    ||

                    v.PhoneNumber.Contains(search)

                    ||

                    v.VehicleNumber.Contains(search)


                    ||

                    v.Purpose.Contains(search)

                );
            }

            return View(
                visitors.ToList()
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

            var visitors =
                _context.Visitors.ToList();

            return new ViewAsPdf(
                "VisitorsPdf",
                visitors
            )
            {
                FileName = "VisitorsReport.pdf",

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

                    worksheet.Cell(row, 4).Value =
                        item.VehicleNumber;

                    worksheet.Cell(row, 5).Value =
                        item.CreatedAt
                            ?.ToString("dd-MM-yyyy");

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

        // CREATE PAGE
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Residents =
    _context.Residents
    .Select(r => new SelectListItem
    {
        Value = r.ResidentId.ToString(),

        Text =
            r.FlatNumber
            + " - " +
            r.OwnerOrTenant
    })
    .ToList();
            return View();
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

            

           
            visitor.CreatedAt = DateTime.Now;
            var residentExists =
    _context.Residents.Any(r =>
        r.ResidentId ==
        visitor.ResidentId);

            if (!residentExists)
            {
                TempData["Error"] =
                    "Please select valid resident";

                ViewBag.Residents =
                    _context.Residents
                    .Select(r => new SelectListItem
                    {
                        Value = r.ResidentId.ToString(),

                        Text =
                            r.FlatNumber
                            + " - " +
                            r.OwnerOrTenant
                    })
                    .ToList();

                return View(visitor);
            }

            if (string.IsNullOrWhiteSpace(visitor.Purpose))
            {
                TempData["Error"] =
                    "Purpose is required";

                return View(visitor);
            }
            _context.Visitors.Add(visitor);

            _context.SaveChanges();

            TempData["Success"] =
                "Visitor Added Successfully";

            return RedirectToAction("Index");
        }
    }
}