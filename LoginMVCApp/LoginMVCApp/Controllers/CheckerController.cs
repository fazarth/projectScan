using LoginMVCApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginMVCApp.Controllers
{
    public class CheckerController : Controller
    {
        private readonly AppDbContext _context;
        public CheckerController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Checker") return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        // POST: Checker/ScanInventory
        [HttpPost]
        public IActionResult ScanInventory(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                TempData["Message"] = "Inventory ID is required.";
                return RedirectToAction("Scan");
            }

            var inventoryData = _context.Inventories
                .Where(i => i.InvId == inventoryId)
                .FirstOrDefault();

            if (inventoryData == null)
            {
                TempData["Message"] = "Data not found for the given Inventory ID.";
                return RedirectToAction("Scan");
            }

            return View("Index", inventoryData);
        }
    }
}
