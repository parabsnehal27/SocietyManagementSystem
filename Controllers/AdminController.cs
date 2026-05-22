using Microsoft.AspNetCore.Mvc;

namespace SocietyManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
