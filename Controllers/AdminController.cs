using Microsoft.AspNetCore.Mvc;

namespace SocietyManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            // Check Session
            if (HttpContext.Session.GetString("AdminSession") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }
    }
}