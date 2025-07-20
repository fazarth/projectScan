using Microsoft.AspNetCore.Mvc;

namespace LoginMVCApp.Controllers
{
    public class CheckerController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Checker") return RedirectToAction("AccessDenied", "Account");

            return View();
        }
    }
}
