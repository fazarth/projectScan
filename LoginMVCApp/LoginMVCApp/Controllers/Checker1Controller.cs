using LoginMVCApp.Data;
using LoginMVCApp.Models;
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

            if (string.IsNullOrEmpty(inventoryId)) return View(); // belum scan

            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
            if (inventory == null) return View();

            var invDbId = inventory.Id;
            var start = DateTime.Today;
            var end = start.AddDays(1);

            var todayTrans = _context.Transactions
                .Where(t => t.InvId == invDbId && t.CreatedAt >= start && t.CreatedAt < end);

            var totalOk = todayTrans.Count(t => t.Status == "OK");
            var totalPolesh = todayTrans.Count(t => t.Status == "POLESH");
            var totalTrans = totalOk + totalPolesh;

            ViewBag.TotalOk = totalOk;
            ViewBag.TotalPolesh = totalPolesh;
            ViewBag.PercentOk = totalTrans == 0 ? 0 : Math.Round((double)totalOk / totalTrans * 100, 1);
            ViewBag.PercentPolesh = totalTrans == 0 ? 0 : Math.Round((double)totalPolesh / totalTrans * 100, 1);

            var selectedRobotId = HttpContext.Session.GetString("SelectedRobotId");
            PopulateRobotDropdown(selectedRobotId);

            ViewBag.InvId = invDbId;
            return View(inventory);
        }

        [HttpPost]
        public IActionResult ScanInventory1(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                TempData["Message"] = "Inventory ID is required.";
                return RedirectToAction("Scan");
            }

            var inventoryData = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
            if (inventoryData == null)
            {
                TempData["Message"] = "Data not found for the given Inventory ID.";
                return RedirectToAction("Scan");
            }

            // PRG pattern: redirect ke Index supaya proses perhitungan OK/POLESH tetap konsisten
            return RedirectToAction(nameof(Index), new { inventoryId });
        }

        [HttpPost]
        public IActionResult SubmitTransaction(long InvId, string InventoryId, string status, long RobotId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId) || userId == 0)
            {
                TempData["Message"] = "Session UserId tidak valid.";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
            }

            if (!_context.Users.Any(u => u.Id == userId))
            {
                TempData["Message"] = "User tidak ditemukan di database.";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
            }

            var lineId = long.Parse(HttpContext.Session.GetString("LineId") ?? "0");
            var role = HttpContext.Session.GetString("Role") ?? "Checker";
            var now = DateTime.Now.TimeOfDay;

            var shiftDefinitions = new List<(string ShiftName, TimeSpan Start, TimeSpan End)>
            {
                ("1", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0)),
                ("1", new TimeSpan(16, 0, 1), new TimeSpan(20, 59, 59)),
                ("2", new TimeSpan(21, 0, 0), new TimeSpan(23, 59, 59)),
                ("2", new TimeSpan(0, 0, 0), new TimeSpan(7, 59, 59)),
            };

            string shift = shiftDefinitions
                .FirstOrDefault(s => s.Start <= s.End
                    ? now >= s.Start && now <= s.End
                    : now >= s.Start || now <= s.End
                ).ShiftName ?? "Unknown";

            if (InvId == 0 || RobotId == 0 || string.IsNullOrEmpty(status))
            {
                TempData["Message"] = "Data tidak lengkap!";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
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

            HttpContext.Session.SetString("SelectedRobotId", RobotId.ToString());

            // Redirect agar Index hitung ulang
            return RedirectToAction(nameof(Index), new { inventoryId = InventoryId });
        }

        private void PopulateRobotDropdown(string? selectedRobotId = null)
        {
            var lineIdString = HttpContext.Session.GetString("LineId");
            if (string.IsNullOrEmpty(lineIdString)) return;

            long lineId = long.Parse(lineIdString);

            var robots = _context.Robots
                .Where(r => r.LineId == lineId)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nama,
                    Selected = selectedRobotId != null && selectedRobotId == r.Id.ToString()
                })
                .ToList();

            ViewBag.RobotList = robots;
        }
    }
}
