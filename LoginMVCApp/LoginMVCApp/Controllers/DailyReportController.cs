using Microsoft.AspNetCore.Mvc;

namespace LoginMVCApp.Controllers
{
    public class DailyReportController : Controller
    {
        public IActionResult DailyReport()
        {
            return View("~/Views/Report/DailyReport.cshtml");
        }
    }
}
