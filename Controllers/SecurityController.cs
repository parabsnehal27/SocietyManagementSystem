using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocietyManagementSystem.Data;
using SocietyManagementSystem.Models;
using System;
using System.Linq;

namespace SocietyManagementSystem.Controllers
{
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SecurityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index(string search)
        {
            var guards = _context.SecurityGuards
                .Include(s => s.User)
                .Where(s => s.User.Role == "Security")
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                guards = guards.Where(s =>

                    s.User.FullName.Contains(search)

                    ||

                    s.ShiftTiming.Contains(search)

                    ||

                    s.Address.Contains(search)
                );
            }

            return View(guards.ToList());
        }

        // =========================
        // CREATE GET
        // =========================
        public IActionResult Create()
        {
            LoadSecurityUsers();

            return View();
        }

        // =========================
        // CREATE POST
        // =========================
        [HttpPost]
        public IActionResult Create(SecurityGuard security)
        {
            LoadSecurityUsers();

            // USER VALIDATION
            if (security.UserId <= 0)
            {
                TempData["Error"] =
                    "Please select employee";

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

                !security.AadhaarNumber.All(char.IsDigit))
            {
                TempData["Error"] =
                    "Enter valid 12 digit Aadhaar Number";

                return View(security);
            }

            // SALARY VALIDATION
            if (security.Salary <= 0)
            {
                TempData["Error"] =
                    "Salary must be greater than 0";

                return View(security);
            }

            // DUPLICATE USER CHECK
            var alreadyAssigned =
                _context.SecurityGuards.Any(s =>

                    s.UserId == security.UserId
                );

            if (alreadyAssigned)
            {
                TempData["Error"] =
                    "This employee is already assigned";

                return View(security);
            }

            // AUTO DATES
            security.CreatedAt =
                DateTime.Now;

            security.JoiningDate =
                DateTime.Now;

            _context.SecurityGuards.Add(security);

            _context.SaveChanges();

            TempData["Success"] =
                "Security Guard Added Successfully";

            return RedirectToAction("Index");
        }

        // =========================
        // EDIT GET
        // =========================
        public IActionResult Edit(int id)
        {
            LoadSecurityUsers();

            var guard = _context.SecurityGuards
                .FirstOrDefault(s => s.GuardId == id);

            if (guard == null)
            {
                return NotFound();
            }

            return View(guard);
        }

        // =========================
        // EDIT POST
        // =========================
        [HttpPost]
        public IActionResult Edit(SecurityGuard security)
        {
            LoadSecurityUsers();

            if (!ModelState.IsValid)
            {
                return View(security);
            }

            var existingGuard =
                _context.SecurityGuards
                .FirstOrDefault(s =>
                    s.GuardId == security.GuardId);

            if (existingGuard == null)
            {
                return NotFound();
            }

            existingGuard.UserId =
                security.UserId;

            existingGuard.ShiftTiming =
                security.ShiftTiming;

            existingGuard.Address =
                security.Address;

            existingGuard.AadhaarNumber =
                security.AadhaarNumber;

            existingGuard.Salary =
                security.Salary;

            _context.SaveChanges();

            TempData["Success"] =
                "Security Guard Updated Successfully";

            return RedirectToAction("Index");
        }

        // =========================
        // LOAD SECURITY USERS
        // =========================
        private void LoadSecurityUsers()
        {
            ViewBag.Users = _context.Users
                .Where(u => u.Role == "Security")
                .ToList();
        }
    }
}