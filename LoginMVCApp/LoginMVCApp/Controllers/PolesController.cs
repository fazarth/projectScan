using LoginMVCApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginMVCApp.Controllers
{
    public class PolesController : Controller
    {
        private readonly AppDbContext _context;
        public PolesController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Poles") return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        // POST: Poles/ScanInventory
        [HttpPost]
        public IActionResult ScanInventoryPoles(string inventoryId)
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
