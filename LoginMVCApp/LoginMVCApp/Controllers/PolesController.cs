using LoginMVCApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;

public class PolesController : Controller
{
    private readonly AppDbContext _context;

    public PolesController(AppDbContext context)
    {
        _context = context;
    }
    private void PopulateNgCategories()
    {
        ViewBag.Categories = _context.Ng_Categories.Select(c => c.Category).Distinct().ToList();

        ViewBag.SubCategories = _context.Ng_Categories
            .Where(c => c.Category != null && c.SubCategory != null)
            .AsEnumerable()
            .GroupBy(c => c.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new { id = x.Id, name = x.SubCategory }).ToList()
            );
    }

    public IActionResult Index(string inventoryId)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Poles") return RedirectToAction("AccessDenied", "Account");

        PopulateNgCategories();

        if (string.IsNullOrEmpty(inventoryId)) return View(); // belum scan

        var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
        if (inventory == null)
        {
            TempData["Message"] = "Inventory tidak ditemukan.";
            PopulateRobotDropdown();
            return View();
        }

        var invDbId = inventory.Id;
        var start = DateTime.Today;
        var end = start.AddDays(1);

        var todayTrans = _context.Transactions
            .Where(t => t.InvId == invDbId && t.CreatedAt >= start && t.CreatedAt < end);

        var totalOk = todayTrans.Count(t => t.Status == "OK");
        var totalNG = todayTrans.Count(t => t.Status == "NG");
        var totalTrans = totalOk + totalNG;

        var totalPoleshByChecker = todayTrans.Count(t => t.Status == "POLESH" && t.Role == "Checker");
        var totalPoleshByPoles = todayTrans.Count(t => t.Status == "POLESH" && t.Role == "Poles");

        ViewBag.TotalOk = totalOk;
        ViewBag.TotalPolesh = totalNG;
        ViewBag.PercentOk = totalTrans == 0 ? 0 : Math.Round((double)totalOk / totalTrans * 100, 1);
        ViewBag.PercentPolesh = totalTrans == 0 ? 0 : Math.Round((double)totalNG / totalTrans * 100, 1);

        ViewBag.TotalPoleshExpected = totalPoleshByChecker;
        ViewBag.TotalPoleshProcessed = totalPoleshByPoles;
        ViewBag.PoleshRemaining = totalPoleshByChecker - totalPoleshByPoles;

        //Get NG detail
        var ngDetailsForDisplay = _context.Transactions
       .Where(t => t.InvId == invDbId && t.CreatedAt >= start && t.CreatedAt < end &&
                  (t.Status == "NG" || t.Status == "POLESH")) // Ambil status NG/POLESH dari transaksi
       .Where(t => t.NgDetailId != null) // Hanya yang punya NgDetailId
       .Join(
           _context.Ng_Categories,
           trans => trans.NgDetailId,
           ngCat => ngCat.Id,
           (trans, ngCat) => new { Transaction = trans, Category = ngCat }
       )
       .GroupBy(x => new { x.Category.Category, x.Category.SubCategory, x.Transaction.Role }) // Group by Category, SubCategory, dan Role
       .Select(g => new NgSummaryViewModel // Buat ViewModel baru untuk display
       {
           Category = g.Key.Category,
           SubCategory = g.Key.SubCategory,
           Role = g.Key.Role,
           Count = g.Count() // Hitung jumlah per kategori/sub-kategori/role
       })
       .ToList();

        ViewBag.NgDetailsForDisplay = ngDetailsForDisplay;

        var selectedRobotId = HttpContext.Session.GetString("SelectedRobotId");
        PopulateRobotDropdown(selectedRobotId);

        ViewBag.InvId = invDbId;
        return View(inventory);
    }

    [HttpGet]
    public IActionResult ScanTransaction()
    {
        PopulateNgCategories();
        PopulateRobotDropdown();
        ViewBag.RobotList = new List<SelectListItem>(); // default kosong
        return View();
    }

    [HttpPost]
    public IActionResult ScanTransaction(string barcode)
    {
        PopulateNgCategories();

        var inventory = _context.Inventories.FirstOrDefault(x => x.InvId == barcode);
        var lineIdString = HttpContext.Session.GetString("LineId");

        if (inventory == null && lineIdString == null)
        {
            TempData["Message"] = "Inventory tidak ditemukan.";
            ViewBag.RobotList = new List<SelectListItem>();
            return View();
        }

        long lineId = long.Parse(lineIdString);
        var robotList = _context.Robots
            .Where(r => r.LineId == lineId)
            .Select(r => new SelectListItem
            {
                Text = r.Nama,
                Value = r.Id.ToString()
            })
            .ToList();

        var categories = _context.Ng_Categories.Select(c => c.Category).Distinct().ToList();

        var subCategories = _context.Ng_Categories
            .Where(c => c.Category != null && c.SubCategory != null)
            .AsEnumerable() // penting! pindahkan ke memory
            .ToList()
            .GroupBy(c => c.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new { id = x.Id, name = x.SubCategory }).ToList()
            );


        ViewBag.InvId = inventory.Id;
        ViewBag.RobotList = robotList;
        ViewBag.Categories = categories;
        ViewBag.SubCategories = subCategories;

        ViewBag.TotalOk = _context.Transactions.Count(x => x.Barcode == inventory.InvId && x.Status == "OK");
        ViewBag.TotalPolesh = _context.Transactions.Count(x => x.Barcode == inventory.InvId && x.Status == "NG");

        var total = ViewBag.TotalOk + ViewBag.TotalPolesh;
        ViewBag.PercentOk = total > 0 ? (ViewBag.TotalOk * 100 / total) : 0;
        ViewBag.PercentPolesh = total > 0 ? (ViewBag.TotalPolesh * 100 / total) : 0;

        return View("Index", inventory);
    }

    [HttpPost]
    //public IActionResult SubmitTransaction(Transactions detail)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        _context.Transactions.Add(detail);
    //        _context.SaveChanges();
    //        return RedirectToAction("ScanTransaction");
    //    }

    //    return View("Index");
    //}

    public IActionResult SubmitTransaction(long InvId, string InventoryId, string status, long RobotId, long? NgDetailId)
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

        if (status == "NG" && (NgDetailId == null || NgDetailId == 0))
        {
            TempData["Message"] = "Untuk status 'POLESH', detail NG harus dipilih!";
            return RedirectToAction("Index", new { inventoryId = InventoryId });
        }

        // --- LOGIKA VALIDASI KUOTA POLESH ---
        if (status == "POLESH")
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);

            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == InventoryId);
            if (inventory == null)
            {
                TempData["Message"] = "Inventory tidak ditemukan saat validasi kuota.";
                return RedirectToAction("Index");
            }
            var invDbIdForQuota = inventory.Id; 

            // Hitung total POLESH oleh Checker untuk InvId pada hari ini
            var totalPoleshByChecker = _context.Transactions
                .Count(t => t.InvId == invDbIdForQuota && t.CreatedAt >= start && t.CreatedAt < end && t.Status == "POLESH" && t.Role == "Checker"); // Pastikan Status='POLESH' jika itu yang dicatat checker

            // Hitung total POLESH yang sudah di-insert oleh Role "Poles" untuk InvId pada hari ini
            var totalPoleshByPoles = _context.Transactions
                .Count(t => t.InvId == invDbIdForQuota && t.CreatedAt >= start && t.CreatedAt < end && t.Status == "POLESH" && t.Role == "Poles"); // Pastikan Status='POLESH'

            // Cek apakah kuota masih tersedia
            if (totalPoleshByPoles >= totalPoleshByChecker)
            {
                TempData["Message"] = $"Kuota POLESH untuk inventory '{InventoryId}' hari ini sudah habis. Total Checker: {totalPoleshByChecker}, Total Poles: {totalPoleshByPoles}.";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
            }
        }

        var transaction = new Transactions
        {
            InvId = InvId,
            Barcode = InventoryId,
            RobotId = RobotId,
            UserId = userId,
            LineId = lineId,
            NgDetailId = NgDetailId,
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
