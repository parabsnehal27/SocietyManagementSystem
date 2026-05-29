using Microsoft.AspNetCore.Mvc;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System;
using System.Linq;

namespace SocietyManagementSystem.Controllers
{
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SecurityController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index(string search)
        {
            var guards =
                _context.SecurityGuards
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                guards = guards.Where(s =>

                    s.EmployeeCode.Contains(search)

                    ||

                    s.ShiftTiming.Contains(search)

                    ||

                    (s.Address != null &&
                     s.Address.Contains(search))
                );
            }

            return View(
                guards.ToList()
            );
        }

        // =========================
        // CREATE GET
        // =========================
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // CREATE POST
        // =========================
        [HttpPost]
        public IActionResult Create(
            SecurityGuard security)
        {
            // EMPLOYEE CODE VALIDATION
            if (string.IsNullOrWhiteSpace(
                security.EmployeeCode))
            {
                TempData["Error"] =
                    "Employee Code is required";

                return View(security);
            }

            // SHIFT VALIDATION
            if (string.IsNullOrWhiteSpace(
                security.ShiftTiming))
            {
                TempData["Error"] =
                    "Please select shift timing";

                return View(security);
            }

            // ADDRESS VALIDATION
            if (string.IsNullOrWhiteSpace(
                security.Address))
            {
                TempData["Error"] =
                    "Address is required";

                return View(security);
            }

            // AADHAAR VALIDATION
            if (string.IsNullOrWhiteSpace(
                security.AadhaarNumber)

                ||

                security.AadhaarNumber.Length != 12

                ||

                !security.AadhaarNumber.All(
                    char.IsDigit))
            {
                TempData["Error"] =
                    "Enter valid 12 digit Aadhaar Number";

                return View(security);
            }

            // SALARY VALIDATION
            if (security.Salary == null ||
                security.Salary <= 0)
            {
                TempData["Error"] =
                    "Salary must be greater than 0";

                return View(security);
            }

            // DUPLICATE EMPLOYEE CODE
            var codeExists =
                _context.SecurityGuards.Any(s =>

                    s.EmployeeCode ==
                    security.EmployeeCode
                );

            if (codeExists)
            {
                TempData["Error"] =
                    "Employee Code already exists";

                return View(security);
            }

            // IMPORTANT:
            // Use existing UserId from Users table
            security.UserId =
    _context.SecurityGuards
    .Max(s => s.UserId) + 1;

            security.CreatedAt =
                DateTime.Now;

            security.JoiningDate =
                DateTime.Now;

            _context.SecurityGuards.Add(
                security);

            _context.SaveChanges();

            TempData["Success"] =
                "Security Guard Added Successfully";

            return RedirectToAction("Index");
        }
    }
}