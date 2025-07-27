using Microsoft.AspNetCore.Mvc;

namespace LoginMVCApp.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
