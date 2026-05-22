using Microsoft.AspNetCore.Mvc;

namespace SocietyManagementSystem.Controllers
{
    public class ResidentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
