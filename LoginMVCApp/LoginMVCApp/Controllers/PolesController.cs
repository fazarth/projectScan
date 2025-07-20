using Microsoft.AspNetCore.Mvc;

namespace LoginMVCApp.Controllers
{
    public class PolesController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Poles") return RedirectToAction("AccessDenied", "Account");

            return View();
        }
    }
}
