using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;
using Rotativa.AspNetCore;
using ClosedXML.Excel;
using System.IO;
namespace SocietyManagementSystem.Controllers
{
    public class ComplaintsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComplaintsController(ApplicationDbContext context)
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

            var complaints =
                _context.Complaints.AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                complaints = complaints.Where(c =>

                    c.Title.Contains(search)

                    ||

                    c.Category.Contains(search)

                    ||

                    c.Status.Contains(search)

                    ||

                    c.Priority.Contains(search)

                );
            }

            return View(
                complaints.ToList()
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

            var complaints =
                _context.Complaints.ToList();

            return new ViewAsPdf(
                "ComplaintsPdf",
                complaints
            )
            {
                FileName = "ComplaintsReport.pdf",

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

            var complaints =
                _context.Complaints.ToList();

            using (var workbook =
                new XLWorkbook())
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

                        "ComplaintsReport.xlsx"
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

        // SAVE COMPLAINT
        [HttpPost]
        public IActionResult Create(Complaint complaint)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            complaint.ResidentId = 2;

            complaint.Status = "Pending";

            complaint.CreatedAt = DateTime.Now;

            _context.Complaints.Add(complaint);

            _context.SaveChanges();

            TempData["Success"] = "Complaint Added Successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Resolve(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var complaint = _context.Complaints.Find(id);

            if (complaint == null)
            {
                return NotFound();
            }

            complaint.Status = "Resolved";

            complaint.ResolvedAt = DateTime.Now;

            complaint.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = "Complaint Resolved Successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var complaint = _context.Complaints.Find(id);

            if (complaint == null)
            {
                return NotFound();
            }

            _context.Complaints.Remove(complaint);

            _context.SaveChanges();

            TempData["Success"] = "Complaint Deleted Successfully";

            return RedirectToAction("Index");
        }


    }
}