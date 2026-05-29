using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System.Linq;
using Rotativa.AspNetCore;
namespace SocietyManagementSystem.Controllers
{
    public class NoticesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NoticesController(ApplicationDbContext context)
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

            var notices =
                _context.Notices.AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                notices = notices.Where(n =>

                    n.Title.Contains(search)

                    ||

                    n.Description.Contains(search)

                    ||

                    n.NoticeType.Contains(search)
                );
            }

            return View(
                notices.ToList()
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

            var notices =
                _context.Notices.ToList();

            return new ViewAsPdf(
                "NoticesPdf",
                notices
            )
            {
                FileName = "NoticesReport.pdf",

                PageOrientation =
                    Rotativa.AspNetCore.Options.Orientation.Landscape,

                PageSize =
                    Rotativa.AspNetCore.Options.Size.A4
            };
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

        public IActionResult Edit(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var notice =
                _context.Notices.Find(id);

            if (notice == null)
            {
                return NotFound();
            }

            return View(notice);
        }

        // SAVE NOTICE
        [HttpPost]
        public IActionResult Create(Notice notice)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            notice.PostedBy = 1;

            notice.PostedDate = DateTime.Now;

            notice.IsActive = true;

            _context.Notices.Add(notice);

            _context.SaveChanges();

            TempData["Success"] = "Notice Added Successfully";

            return RedirectToAction("Index");
        }



        [HttpPost]
        public IActionResult Edit(Notice notice)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var existingNotice =
    _context.Notices.Find(
        notice.NoticeId
    );

            if (existingNotice == null)
            {
                return NotFound();
            }

            existingNotice.Title =
                notice.Title;

            existingNotice.Description =
                notice.Description;

            existingNotice.NoticeType =
                notice.NoticeType;

            existingNotice.ExpiryDate =
                notice.ExpiryDate;


            _context.SaveChanges();

            TempData["Success"] =
                "Notice Updated Successfully";

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            var notice = _context.Notices.Find(id);

            if (notice == null)
            {
                return NotFound();
            }

            _context.Notices.Remove(notice);

            _context.SaveChanges();

            TempData["Success"] = "Notice Deleted Successfully";

            return RedirectToAction("Index");
        }
    }
}