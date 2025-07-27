using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
//using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

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
        //[HttpPost]
        //public IActionResult ScanInventory1(string inventoryId)
        //{
        //    if (string.IsNullOrEmpty(inventoryId))
        //    {
        //        TempData["Message"] = "Inventory ID is required.";
        //        return RedirectToAction("Scan");
        //    }

        //    var inventoryData = _context.Inventories.Where(i => i.InvId == inventoryId).FirstOrDefault();

        //    if (inventoryData == null)
        //    {
        //        TempData["Message"] = "Data not found for the given Inventory ID.";
        //        return RedirectToAction("Scan");
        //    }

        //    PopulateRobotDropdown();
        //    return View("Index", inventoryData);
        //}

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

            // Ini penting untuk keperluan input hidden di view (SubmitTransaction)
            ViewBag.InvId = inventoryData.Id;

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
            var userIdStr = HttpContext.Session.GetString("UserId");
            long userId;

            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out userId) || userId == 0)
            {
                TempData["Message"] = "Session UserId tidak valid.";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
            }

            // Validasi keberadaan user di DB
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
                ("2", new TimeSpan(21, 0, 0), new TimeSpan(5, 0, 0)),
                //("3", new TimeSpan(21, 0, 0), new TimeSpan(5, 0, 0))
            };

            string shift = shiftDefinitions
                .FirstOrDefault(s =>
                    s.Start <= s.End
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

            // Gunakan parameter InvId langsung
            var totalTrans = _context.Transactions.Count(t => t.InvId == InvId);

            // Hitung total OK dan POLESH
            var totalOk = _context.Transactions.Count(t => t.InvId == InvId && t.Status == "OK");
            var totalPolesh = _context.Transactions.Count(t => t.InvId == InvId && t.Status == "POLESH");

            // Hitung persentase OK dan POLESH
            double percentOk = totalTrans == 0 ? 0 : ((double)totalOk / totalTrans) * 100;
            double percentPolesh = totalTrans == 0 ? 0 : ((double)totalPolesh / totalTrans) * 100;

            // Kirim ke View (meskipun setelah ini redirect, jadi ViewBag ini tidak terpakai di sini)
            ViewBag.TotalOk = totalOk;
            ViewBag.TotalPolesh = totalPolesh;
            ViewBag.PercentOk = percentOk.ToString("F1");
            ViewBag.PercentPolesh = percentPolesh.ToString("F1");
            return RedirectToAction("Index", new { inventoryId = InventoryId });
        }


    }
}