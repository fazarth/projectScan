using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
//using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LoginMVCApp.Controllers
{
    public class Checker1Controller : Controller
    {
        private readonly AppDbContext _context;
        public Checker1Controller(AppDbContext context)
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
        public IActionResult ScanInventory1(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                TempData["Message"] = "Inventory ID is required.";
                return RedirectToAction("Scan");
            }

            var inventoryData = _context.Inventories.Where(i => i.InvId == inventoryId).FirstOrDefault();

            if (inventoryData == null)
            {
                TempData["Message"] = "Data not found for the given Inventory ID.";
                return RedirectToAction("Scan");
            }

            PopulateRobotDropdown();
            return View("Index", inventoryData);
        }

        private void PopulateRobotDropdown()
        {
            var lineIdString = HttpContext.Session.GetString("LineId");
            if (string.IsNullOrEmpty(lineIdString)) return;

            long lineId = long.Parse(lineIdString);

            var robots = _context.Robots
                .Where(r => r.LineId == lineId)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nama
                }).ToList();

            ViewBag.RobotList = robots;
        }
    }
}