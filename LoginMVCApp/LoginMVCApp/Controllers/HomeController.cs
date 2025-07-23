using LoginMVCApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LoginMVCApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("Email");
            var fullName = HttpContext.Session.GetString("FullName");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(email))
            {
                TempData["error"] = "Sesi Anda telah habis. Silakan login ulang.";
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpGet]
        [ValidateAntiForgeryToken]
        public IActionResult Privacy()
        {
            return View();
        }
        [HttpGet]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}