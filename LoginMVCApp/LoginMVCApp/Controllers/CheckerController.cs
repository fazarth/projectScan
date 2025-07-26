using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
//using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            PopulateRobotDropdown();
            return View();
        }

        // POST: Checker/ScanInventory
        //[HttpPost]
        //public IActionResult ScanInventory(string inventoryId)
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
        public IActionResult ScanInventory(string inventoryId)
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

            var userId = long.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            var lineId = _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.LineId)
                .FirstOrDefault();

            var robots = _context.Robots.Where(r => r.LineId == lineId).Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nama
            }).ToList();

            var inventoryData = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);

            var vm = new CheckerViewModel
            {
                Inventory = inventoryData,
                RobotList = robots
            };

            return View("Index", vm);
        }


        [HttpPost]
        public IActionResult SubmitTransaction(long InvId, string InventoryId, string status, long RobotId)
        {
            var userId = long.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            var lineId = long.Parse(HttpContext.Session.GetString("LineId") ?? "0");
            var role = HttpContext.Session.GetString("Role") ?? "checker";
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

            if (status == "POLESH")
                return RedirectToAction("Index", "Poles", new { inventoryId = InventoryId });

            TempData["Message"] = "Data OK berhasil disimpan.";
            return RedirectToAction("Index");
        }


        private void PopulateRobotDropdown()
        {
            var lineId = long.Parse(HttpContext.Session.GetString("LineId") ?? "0");

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
