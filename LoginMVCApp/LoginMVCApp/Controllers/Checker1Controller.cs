using LoginMVCApp.Data;
using LoginMVCApp.Models;
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

            if (string.IsNullOrEmpty(inventoryId))
            {
                ViewBag.TotalOk = 0;
                ViewBag.TotalPolesh = 0;
                ViewBag.PercentOk = 0;
                ViewBag.PercentPolesh = 0;
                return View(); // belum scan
            }

            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
            if (inventory == null)
            {
                // Reset jika inventory tidak ditemukan
                ViewBag.TotalOk = 0;
                ViewBag.TotalPolesh = 0;
                ViewBag.PercentOk = 0;
                ViewBag.PercentPolesh = 0;
                return View();
            }

            HttpContext.Session.SetString("Project", inventory.Project ?? "");
            HttpContext.Session.SetString("Warna", inventory.Warna ?? "");

            var invDbId = inventory.Id;
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
                .Where(t => t.InvId == invDbId && t.CreatedAt >= shiftStart && t.CreatedAt < shiftEnd);

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

            if (InvId == 0 || RobotId == 0 || string.IsNullOrEmpty(status))
            {
                TempData["Message"] = "Data tidak lengkap!";
                return RedirectToAction("Index", new { inventoryId = InventoryId });
            }

            var transaction = new Transactions
            {
                InvId = InvId,
                Barcode = InventoryId,
                RobotId = RobotId,
                UserId = userId,
                LineId = lineId,
                Role = role,
                Status = status,
                Qty = 1,
                Shift = shift,
                CreatedAt = createdAt
                //CreatedAt = DateTime.Now
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
        public IActionResult PrintQrPdf(string invId, string project, string color, string robot, int jumlahQr = 1)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (string.IsNullOrEmpty(invId) || string.IsNullOrEmpty(project) ||
                string.IsNullOrEmpty(color) || string.IsNullOrEmpty(robot) || jumlahQr <= 0)
            {
                TempData["Message"] = "Data tidak lengkap atau tidak valid untuk generate QR/PDF.";
                return RedirectToAction("Index", new { inventoryId = invId });
            }

            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == invId);
            if (inventory == null)
            {
                TempData["Message"] = "Inventory ID tidak ditemukan di database.";
                return RedirectToAction("Index", new { inventoryId = invId });
            }
            var lineId = HttpContext.Session.GetString("LineId");
            var userGroup = HttpContext.Session.GetString("UserGroup");

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

            var pdfStream = new MemoryStream();
            Document.Create(container =>
            {
                for (int i = 0; i < jumlahQr; i += 6)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.Content().Column(mainColumn =>
                        {
                            mainColumn.Item().Row(row =>
                            {
                                for (int j = 0; j < 6 && (i + j) < jumlahQr; j++)
                                {
                                    int nomorUrut = startNumber + i + j;
                                    var tanggal = DateTime.Now.ToString("yyyyMMdd");
                                    string qrDataString = $"{invId}{project}{color}{tanggal}{robot}{lineId}{userGroup}{nomorUrut}";
                                    QRCombine += qrDataString + "\n";

                                    using var qrGenerator = new QRCodeGenerator();
                                    using var qrCodeData = qrGenerator.CreateQrCode(qrDataString, QRCodeGenerator.ECCLevel.Q);
                                    var qrCodePngByte = new PngByteQRCode(qrCodeData);
                                    byte[] qrImageBytes = qrCodePngByte.GetGraphic(10);

                                    row.RelativeItem().AlignCenter().Container()
                                    .Width(28.35f)
                                    .Height(28.35f)
                                    .Image(qrImageBytes);
                                        }
                                    });
                            });

                        });
                    }
            }).GeneratePdf(pdfStream);

            pdfStream.Position = 0;
            string fileName = $"QR_Codes_{invId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdfStream.ToArray(), "application/pdf", fileName);
        }
    }

}
