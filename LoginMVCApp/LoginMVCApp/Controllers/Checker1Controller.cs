using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics.Metrics;
using System.Drawing.Imaging;

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

            string dataType = TempData["DataType"] as string;

            // Ambil data Inventory berdasarkan InventoryId
            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);

            // Ambil data Transaction yang sesuai dengan Barcode
            var transaction = (from t in _context.Transactions
                               join i in _context.Inventories on t.InvId equals i.Id
                               where t.Barcode == inventoryId
                               select new
                               {
                                   Transaction = t,
                                   Inventory = i
                               }).FirstOrDefault();

            var result = "";

            // Tentukan data yang ditemukan (Inventory atau Transaction)
            if (inventory != null)
            {
                result = inventory.InvId;
                ViewBag.Project = inventory.Project ?? "";
                ViewBag.Warna = inventory.Warna ?? "";
            }
            else if (transaction != null)
            {
                result = transaction.Inventory.InvId;
                HttpContext.Session.SetString("Project", transaction.Inventory.Project ?? "");
                HttpContext.Session.SetString("Warna", transaction.Inventory.Warna ?? "");
            }

            var invDbId = result;
            var start = DateTime.Today;
            var end = start.AddDays(1);
            long invDbIdInv = inventory?.Id ?? transaction.Inventory.Id;
            var now = DateTime.Now;
            var time = now.TimeOfDay;

            // Definisi shift
            var shiftDefinitions = new List<(string ShiftName, TimeSpan Start, TimeSpan End)>
            {
                ("1", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0)),
                ("1", new TimeSpan(16, 0, 1), new TimeSpan(20, 59, 59)),
                ("2", new TimeSpan(21, 0, 0), new TimeSpan(23, 59, 59)),
                ("2", new TimeSpan(0, 0, 0), new TimeSpan(7, 59, 59)),
            };

            // Cari shift sekarang
            var matchedShift = shiftDefinitions.FirstOrDefault(s =>
                s.Start <= s.End ? time >= s.Start && time <= s.End : time >= s.Start || time <= s.End);

            string currentShift = matchedShift.ShiftName ?? "Unknown";

            // Tentukan tanggal shift, khusus shift 2 subuh mundur sehari
            DateTime shiftDate = (currentShift == "2" && now.Hour < 8)
                ? now.Date.AddDays(-1)
                : now.Date;

            // Batas shiftStart dan shiftEnd
            DateTime shiftStart, shiftEnd;

            if (currentShift == "1")
            {
                shiftStart = shiftDate.AddHours(8);        // 08:00
                shiftEnd = shiftDate.AddHours(21);         // 20:59:59
            }
            else
            {
                shiftStart = shiftDate.AddHours(21);       // 21:00 malam
                shiftEnd = shiftDate.AddDays(1).AddHours(8); // 07:59 esok hari
            }

            // Ganti query total transaksi agar pakai waktu shift
            var todayTrans = _context.Transactions
                .Where(t => t.InvId == invDbIdInv && t.CreatedAt >= shiftStart && t.CreatedAt < shiftEnd);

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
            ViewBag.invDbIdInv = invDbIdInv;
            ViewBag.DataType = dataType;

            // Membuat ViewModel dan mengirimkan ke view
            var viewModel = new InventoryTransactionViewModel
            {
                Inventory = inventory,  // Menyertakan data Inventory
                Transaction = transaction?.Transaction // Menyertakan data Transaction jika ditemukan
            };

            return View(viewModel);  // Mengirimkan ViewModel ke view
        }




        //[HttpPost]
        //public IActionResult ScanInventory1(string inventoryId)
        //{
        //    if (string.IsNullOrEmpty(inventoryId))
        //    {
        //        TempData["Message"] = "Inventory ID is required.";
        //        return RedirectToAction("Scan");
        //    }

        //    var inventoryData = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
        //    if (inventoryData == null)
        //    {
        //        TempData["Message"] = "Data not found for the given Inventory ID.";
        //        return RedirectToAction("Scan");
        //    }

        //    return RedirectToAction(nameof(Index), new { inventoryId });
        //}

        [HttpPost]
        public IActionResult ScanInventory1(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                TempData["Message"] = "Inventory ID is required.";
                return RedirectToAction("Scan");
            }

            var inventoryData = _context.Inventories
                .FirstOrDefault(i => i.InvId == inventoryId);

            if (inventoryData != null)
            {
                TempData["DataType"] = "inventory";
                return RedirectToAction(nameof(Index), new { inventoryId });
            }
            else
            {
                var transactionData = _context.Transactions
                    .FirstOrDefault(t => t.Barcode == inventoryId);

                if (transactionData != null)
                {
                    TempData["DataType"] = "transaction";
                    return RedirectToAction(nameof(Index), new { inventoryId });
                }
                else
                {
                    TempData["Message"] = "Data not found for the given Inventory ID.";
                    return RedirectToAction("Scan");
                }
            }
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
            var now = DateTime.Now;
            var time = now.TimeOfDay;

            // Menentukan shift berdasarkan waktu
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

            // Validasi input
            //if (InvId == 0 || RobotId == 0 || string.IsNullOrEmpty(status))
            //{
            //    TempData["Message"] = "Data tidak lengkap!";
            //    return RedirectToAction("Index", new { inventoryId = InventoryId });
            //}

            // ✅ Cek apakah sudah ada transaksi yang sama
            var existingTransaction = _context.Transactions
                .Where(t => t.Barcode == InventoryId&& t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();

            if (existingTransaction != null)
            {
                // ❌ Kalau semua data sama, jangan update
                if (existingTransaction.Status == status &&
                    existingTransaction.RobotId == RobotId &&
                    existingTransaction.Barcode == InventoryId &&
                    existingTransaction.LineId == lineId &&
                    existingTransaction.Role == role &&
                    existingTransaction.Shift == shift)
                {
                    // Tidak perlu update
                    TempData["Message"] = "Transaksi sudah tersimpan dengan data yang sama.";
                    return RedirectToAction("Index", new { inventoryId = InventoryId });
                }

                // 🔁 Kalau ada yang beda, update field yang berbeda
                bool updated = false;

                if (existingTransaction.Status != status)
                {
                    existingTransaction.Status = status;
                    updated = true;
                }
                if (existingTransaction.RobotId != RobotId)
                {
                    existingTransaction.RobotId = RobotId;
                    updated = true;
                }
                //if (existingTransaction.Barcode != InventoryId)
                //{
                //    existingTransaction.Barcode = InventoryId;
                //    updated = true;
                //}
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
                if (existingTransaction.Shift != shift)
                {
                    existingTransaction.Shift = shift;
                    updated = true;
                }

                if (updated)
                {
                    existingTransaction.CreatedAt = DateTime.Now;
                    _context.Transactions.Update(existingTransaction);
                    _context.SaveChanges();
                    TempData["Message"] = "Transaksi berhasil diperbarui.";
                }

                HttpContext.Session.SetString("SelectedRobotId", RobotId.ToString());
                return RedirectToAction(nameof(Index), new { inventoryId = InventoryId });
            }

            // ➕ Insert baru
            var transaction = new Transactions
            {
                InvId = InvId,
                Barcode = InventoryId,
                RobotId = RobotId,
                UserId = userId,
                LineId = lineId,
                Role = role,
                Status = status,
                NgDetailId = null, 
                Qty = 1,
                Shift = shift,
                OppositeShift = false,  
                CreatedAt = DateTime.Now,
                Is_Return = false  
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            HttpContext.Session.SetString("SelectedRobotId", RobotId.ToString());
            TempData["Message"] = "Transaksi berhasil ditambahkan.";
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
        public IActionResult PrintQrPdf(long invId, long inventoryId, string project, string color, string robot, int robotId, int jumlahQr = 1)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (invId <= 0 || string.IsNullOrEmpty(project) ||
                string.IsNullOrEmpty(color) || string.IsNullOrEmpty(robot) || jumlahQr <= 0)
            {
                TempData["Message"] = "Data tidak lengkap atau tidak valid untuk generate QR/PDF.";
                return RedirectToAction("Index", new { inventoryId = invId });
            }

            var inventory = _context.Inventories.FirstOrDefault(i => i.Id == invId);
            if (inventory == null)
            {
                TempData["Message"] = "Inventory ID tidak ditemukan di database.";
                return RedirectToAction("Index", new { inventoryId = invId });
            }

            var lineId = HttpContext.Session.GetString("LineId");
            var userGroup = HttpContext.Session.GetString("UserGroup");
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetString("UserId");

            var QRCombine = "";

            // Ambil counter terakhir dari DB
            var counter = _context.Qr_Counter.FirstOrDefault(q => q.Id == 1);
            if (counter == null)
            {
                counter = new Qr_Counter { Id = 1, LastNumber = 0 };
                _context.Qr_Counter.Add(counter);
                _context.SaveChanges();
            }

            // Tambahkan jumlah QR yang akan di-generate
            int startNumber = counter.LastNumber + 1;
            counter.LastNumber += jumlahQr; // update total
            _context.SaveChanges(); // simpan ke DB

            // Buat PDF QR dan simpan ke transaction
            var pdfStream = new MemoryStream();
            for (int i = 0; i < jumlahQr; i++)
            {
                int nomorUrut = startNumber + i;
                var tanggal = DateTime.Now.ToString("yyyyMMdd");
                string rawQRDataString = $"{inventory.InvId}{project}{color}{tanggal}{robot}{lineId}{userGroup}{nomorUrut}";
                string originalString = rawQRDataString;
                string qrDataString = originalString.Replace(" ", "");

                QRCombine += qrDataString + "\n";

                // Generate QR Code Image
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrDataString, QRCodeGenerator.ECCLevel.Q);
                var qrCodePngByte = new PngByteQRCode(qrCodeData);
                byte[] qrImageBytes = qrCodePngByte.GetGraphic(10);

                // Simpan ke transaksi
                var transaction = new Transactions
                {
                    InvId = invId,
                    Barcode = qrDataString,
                    LineId = long.Parse(lineId),
                    RobotId = robotId,
                    UserId = long.Parse(userId),
                    Role = role,
                    Status = "PRINTED",
                    NgDetailId = null,
                    Qty = 1,
                    Shift = "",
                    OppositeShift = false,
                    CreatedAt = DateTime.Now,
                    Is_Return = false
                };
                _context.Transactions.Add(transaction);
                _context.SaveChanges(); // Simpan ke database
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.Content().Column(mainColumn =>
                    {
                        for (int i = 0; i < jumlahQr; i++)
                        {
                            int nomorUrut = startNumber + i;
                            var tanggal = DateTime.Now.ToString("yyyyMMdd");
                            string qrDataString = $"{invId}{project}{color}{tanggal}{robot}{lineId}{userGroup}{nomorUrut}";
                            QRCombine += qrDataString + "\n";
                            using var qrGenerator = new QRCodeGenerator();
                            using var qrCodeData = qrGenerator.CreateQrCode(qrDataString, QRCodeGenerator.ECCLevel.Q);
                            var qrCodePngByte = new PngByteQRCode(qrCodeData);
                            byte[] qrImageBytes = qrCodePngByte.GetGraphic(10);

                            mainColumn.Item().Column(itemColumn =>
                            {
                                itemColumn.Item()
                                    .AlignCenter()
                                    .Width(150)
                                    .Height(150)
                                    .Image(qrImageBytes); // Hapus FitWidth(), atur ukuran langsung
                            });
                        }
                    });
                });
            }).GeneratePdf(pdfStream);

            pdfStream.Position = 0;
            string fileName = $"QR_Codes_{invId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdfStream.ToArray(), "application/pdf", fileName);
        }
    }
}
