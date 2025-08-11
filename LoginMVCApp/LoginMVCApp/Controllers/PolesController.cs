using LoginMVCApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
using System.Diagnostics.Tracing;
using Microsoft.CodeAnalysis.Operations;

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

        //var inventory = _context.Transactions.FirstOrDefault(i => i.Barcode == inventoryId);
        var transaction = (from t in _context.Transactions
                           join i in _context.Inventories on t.InvId equals i.Id into inventoryJoin
                           from i in inventoryJoin.DefaultIfEmpty()
                           where t.Barcode == inventoryId
                           select new
                           {
                               Transaction = t,
                               Inventory = i
                           }).FirstOrDefault();
        if (transaction == null)
        {
            TempData["Message"] = "Inventory tidak ditemukan.";
            PopulateRobotDropdown();
            return View();
        }

        HttpContext.Session.SetString("Project", transaction.Inventory.Project ?? "");
        HttpContext.Session.SetString("Warna", transaction.Inventory.Warna ?? "");

        var invDbId = transaction.Inventory.Id;
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
        var viewModel = new InventoryTransactionViewModel
        {
            Inventory = transaction.Inventory,
            Transaction = transaction?.Transaction
        };

        return View(viewModel);
        //return View(inventory);
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
    public IActionResult ScanTransaction(string inventoryId)
    {
        PopulateNgCategories();

        var inventory = (from t in _context.Transactions
                         join i in _context.Inventories on t.InvId equals i.Id into inventoryJoin
                         from i in inventoryJoin.DefaultIfEmpty()
                         where t.Barcode == inventoryId
                         select new InventoryTransactionViewModel
                         {
                             Transaction = t,
                             Inventory = i
                         }).FirstOrDefault();

        var lineIdString = HttpContext.Session.GetString("LineId");

        if (inventory == null)
        {
            TempData["Message"] = "Inventory tidak ditemukan";
            return RedirectToAction(nameof(Index), new { inventoryId });
        }

        if (lineIdString == null)
        {
            TempData["Message"] = "Line tidak terbaca";
            return RedirectToAction(nameof(Index), new { inventoryId });
        }

        if (inventory.Transaction.Role == "Checker")
        {
            if (inventory.Transaction.Status == "PRINTED")
            {
                TempData["Message"] = "Data ini berstatus PRINTED dan belum di proses oleh Checker";
                return RedirectToAction("Index");
            } else if (inventory.Transaction.Status == "OK")
            {
                TempData["Message"] = "Data ini berstatus OK dari Checker";
                return RedirectToAction("Index");
            }
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

        var totalOk = _context.Transactions.Count(t => t.InvId == inventory.Inventory.Id && t.Status == "OK");
        var totalNG = _context.Transactions.Count(t => t.InvId == inventory.Inventory.Id && t.Status == "NG");
        var totalTrans = totalOk + totalNG;

        ViewBag.TotalOk = totalOk;
        ViewBag.TotalPolesh = totalNG;
        ViewBag.PercentOk = totalTrans == 0 ? 0 : Math.Round((double)totalOk / totalTrans * 100, 1);
        ViewBag.PercentPolesh = totalTrans == 0 ? 0 : Math.Round((double)totalNG / totalTrans * 100, 1);

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


        ViewBag.InvId = inventory.Inventory.Id;
        ViewBag.RobotList = robotList;
        ViewBag.Categories = categories;
        ViewBag.SubCategories = subCategories;
        return RedirectToAction(nameof(Index), new { inventoryId });
    }

    [HttpPost]
    public IActionResult SubmitTransaction(long InvId, string InventoryId, string status, long RobotId, long? NgDetailId, bool isReturn, bool isOppositeShit)
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

        var now = DateTime.Now;
        var time = now.TimeOfDay;

        var shiftDefinitions = new List<(string ShiftName, TimeSpan Start, TimeSpan End)>
        {
            ("1", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0)),
            ("1", new TimeSpan(16, 0, 1), new TimeSpan(20, 59, 59)),
            ("2", new TimeSpan(21, 0, 0), new TimeSpan(23, 59, 59)),
            ("2", new TimeSpan(0, 0, 0), new TimeSpan(7, 59, 59)),
        };

        var matchedShift = shiftDefinitions
            .FirstOrDefault(s => s.Start <= s.End
                ? time >= s.Start && time <= s.End
                : time >= s.Start || time <= s.End);

        string shift = matchedShift.ShiftName ?? "Unknown";

        // Koreksi CreatedAt jika shift 2 dini hari
        DateTime createdAt = (shift == "2" && now.Hour < 8)
            ? now.AddDays(-1)
            : now;

        if (InvId == 0 || string.IsNullOrEmpty(status))
        {
            TempData["Message"] = "Data tidak lengkap!";
            return RedirectToAction("Index", new { inventoryId = InventoryId });
        }

        if (status == "NG" && (NgDetailId == null || NgDetailId == 0))
        {
            TempData["Message"] = "Untuk status 'NG', detail NG harus dipilih!";
            return RedirectToAction("Index", new { inventoryId = InventoryId });
        }

        if (role.ToLower() == "poles" && status == "POLESH")
        {
            var start = createdAt;
            var end = createdAt.AddDays(1);

            var allowedPolesh = _context.Transactions.Count(t =>
                t.InvId == InvId &&
                t.Status == "POLESH" &&
                t.Role.ToLower() == "checker" &&
                t.Shift == shift &&
                t.CreatedAt >= start && t.CreatedAt < end
            );

            var currentPolesh = _context.Transactions.Count(t =>
                t.InvId == InvId &&
                t.Status == "POLESH" &&
                t.Role.ToLower() == "poles" &&
                t.Shift == shift &&
                t.CreatedAt >= start && t.CreatedAt < end
            );

            if (currentPolesh >= allowedPolesh)
            {
                TempData["Message"] = $"POLES sudah maksimal ({allowedPolesh}) untuk barcode ini di shift saat ini.";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
            }
        }

        var existingTransaction = _context.Transactions
                .Where(t => t.Barcode == InventoryId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();


        if (existingTransaction != null)
        {
            bool updated = false;
            if (!isReturn)
            {
                // Pengecekan status yang sama dan status yang memerlukan konfirmasi
                if (status == existingTransaction.Status && existingTransaction.NgDetailId == NgDetailId)
                {
                    TempData["ErrorMessage"] = $"Tidak dapat menggunakan status {status} karena sama dengan status sebelumnya.";
                    TempData["ExistingStatus"] = existingTransaction.Status;
                    TempData["InventoryId"] = InventoryId;
                    return RedirectToAction("Index", new { inventoryId = InventoryId });
                }

                if (existingTransaction.Status == "NG" || (existingTransaction.Status == "NG" && existingTransaction.NgDetailId == NgDetailId) || (existingTransaction.Status == "OK" && existingTransaction.Role == "Poles")) 
                {
                    TempData["ShowReturnConfirmation"] = "true";
                    TempData["ExistingStatus"] = existingTransaction.Status;
                    TempData["NewStatus"] = status;
                    TempData["InventoryId"] = InventoryId;
                    if (NgDetailId.HasValue)
                    {
                        TempData["NgDetailId"] = NgDetailId.Value.ToString();
                    }
                    return RedirectToAction("Index", new { inventoryId = InventoryId });
                }
            }

            if (isOppositeShit)
            {
                existingTransaction.Shift = existingTransaction.Shift;
                existingTransaction.OppositeShift = existingTransaction.Shift == "1" ? "2" : "1"; // Menentukan OppositeShift
                updated = true;
            }

            if (existingTransaction.Status != status)
            {
                existingTransaction.Status = status;
                updated = true;
            }
            if (existingTransaction.LineId != lineId)
            {
                existingTransaction.LineId = lineId;
                updated = true;
            }
            if (existingTransaction.Role != role)
            {
                existingTransaction.Role = role;
                updated = true;
            }
            if (existingTransaction.Is_Return != isReturn)
            {
                if (isReturn)
                {
                    existingTransaction.Is_Return = isReturn;
                    updated = true;
                }
            }
            if (existingTransaction.NgDetailId != NgDetailId)
            {
                existingTransaction.NgDetailId = NgDetailId;
                updated = true;
            }

            if (updated)
            {
                existingTransaction.CreatedAt = DateTime.Now;
                _context.Transactions.Update(existingTransaction);
                _context.SaveChanges();
                TempData["Message"] = "Transaksi berhasil diperbarui.";
            }
        }
        //else
        //{
        //    // Insert new transaction
        //    var transaction = new Transactions
        //    {
        //        InvId = InvId,
        //        Barcode = InventoryId,
        //        RobotId = RobotId,
        //        UserId = userId,
        //        LineId = lineId,
        //        Role = role,
        //        Status = status,
        //        NgDetailId = null,
        //        Qty = 1,
        //        Shift = shift,
        //        OppositeShift = "0",
        //        CreatedAt = DateTime.Now,
        //        Is_Return = isReturn
        //    };

        //    _context.Transactions.Add(transaction);
        //    _context.SaveChanges();
        //    TempData["Message"] = "Transaksi berhasil ditambahkan.";
        //}

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
