using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using Rotativa.AspNetCore;
using ClosedXML.Excel;
using System.IO;
namespace SocietyManagementSystem.Controllers
{
    public class ResidentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResidentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("AdminSession") != null;
        }

        public IActionResult Index(string search)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var residents =
                _context.Residents.AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                residents = residents.Where(r =>

                    r.FlatNumber.ToString()
                        .Contains(search)

                    ||

                    r.Wing.Contains(search)

                    ||

                    r.OwnerOrTenant.Contains(search)

                    ||

                    r.ContactPhone.Contains(search)

                );
            }

            return View(
                residents.ToList()
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

        public IActionResult ExportExcel()
        {
            if (!IsLoggedIn())
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

                    worksheet.Cell(row, 7).Value =
                        item.IsApproved == true
                        ? "Approved"
                        : "Pending";

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

        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // GET EDIT PAGE
        public IActionResult Edit(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var resident = _context.Residents.Find(id);

            if (resident == null)
            {
                return NotFound();
            }

            return View(resident);
        }

        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var resident =
                _context.Residents.Find(id);

            if (resident == null)
            {
                return NotFound();
            }

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

            return RedirectToAction("Index");
        }

        public IActionResult Approve(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var resident = _context.Residents.Find(id);

            if (resident == null)
            {
                return NotFound();
            }

            resident.IsApproved = true;

            _context.SaveChanges();

            TempData["Success"] = "Resident Approved Successfully";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Edit(Resident resident)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            _context.Residents.Update(resident);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Create(Resident resident)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            resident.UserId = 1;
            resident.CreatedAt = DateTime.Now;

            _context.Residents.Add(resident);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}