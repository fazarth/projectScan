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
        public IActionResult Index(string inventoryId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Checker") return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrEmpty(inventoryId)) return View();

            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
            if (inventory == null) return View();

            PopulateRobotDropdown();

            return View(inventory);
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

        [HttpPost]
        public IActionResult SubmitTransaction(long InvId, string InventoryId, string status, long RobotId)
        {
            var userId = long.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            var lineId = long.Parse(HttpContext.Session.GetString("LineId") ?? "0");
            var role = HttpContext.Session.GetString("Role") ?? "Checker";
            var shift = HttpContext.Session.GetString("Shift") ?? "1";

            if (InvId == 0 || RobotId == 0 || string.IsNullOrEmpty(status))
            {
                TempData["Message"] = "Data tidak lengkap!";
                return RedirectToAction("Index");
            }

            var transaction = new Transactions
            {
                InvId = InvId,
                RobotId = RobotId,
                UserId = userId,
                LineId = lineId,
                Role = role,
                Status = status,
                Qty = 1,
                Shift = shift,
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            // hitung total OK & POLESH
            var totalTrans = _context.Transactions.Count(t => t.InvId == InvId);
            var totalOk = _context.Transactions.Count(t => t.InvId == InvId && t.Status == "OK");

            double percentOk = totalTrans == 0 ? 0 : ((double)totalOk / totalTrans) * 100;

            TempData["PercentOK"] = percentOk.ToString("F1"); // 1 digit desimal

            return RedirectToAction("Index", new { inventoryId = InventoryId });
        }

    }
}