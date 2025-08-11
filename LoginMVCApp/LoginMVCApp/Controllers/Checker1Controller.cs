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
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

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

            var inventory = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
            var transaction = (from t in _context.Transactions
                               join i in _context.Inventories on t.InvId equals i.Id into inventoryJoin
                               from i in inventoryJoin.DefaultIfEmpty()
                               where t.Barcode == inventoryId
                               select new
                               {
                                   Transaction = t,
                                   Inventory = i
                               }).FirstOrDefault();

            var result = "";
            if (inventory != null)
            {
                result = inventory.InvId;
                ViewBag.Project = inventory.Project ?? "";
                ViewBag.Warna = inventory.Warna ?? "";
            }
            else if (transaction != null)
            {
                result = transaction.Inventory.InvId;
                ViewBag.Project = transaction.Inventory.Project ?? "";
                ViewBag.Warna = transaction.Inventory.Warna ?? "";
            }

            var invDbId = result;
            var start = DateTime.Today;
            var end = start.AddDays(1);
            long invDbIdInv = inventory?.Id ?? transaction.Inventory.Id;
            var now = DateTime.Now;
            var time = now.TimeOfDay;

            var shiftDefinitions = new List<(string ShiftName, TimeSpan Start, TimeSpan End)>
            {
                ("1", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0)),
                ("1", new TimeSpan(16, 0, 1), new TimeSpan(20, 59, 59)),
                ("2", new TimeSpan(21, 0, 0), new TimeSpan(23, 59, 59)),
                ("2", new TimeSpan(0, 0, 0), new TimeSpan(7, 59, 59)),
            };

            var matchedShift = shiftDefinitions.FirstOrDefault(s =>
                s.Start <= s.End ? time >= s.Start && time <= s.End : time >= s.Start || time <= s.End);

            string currentShift = matchedShift.ShiftName ?? "Unknown";
            DateTime shiftDate = (currentShift == "2" && now.Hour < 8) ? now.Date.AddDays(-1) : now.Date;

            DateTime shiftStart, shiftEnd;
            if (currentShift == "1")
            {
                shiftStart = shiftDate.AddHours(8);
                shiftEnd = shiftDate.AddHours(21);
            }
            else
            {
                shiftStart = shiftDate.AddHours(21);
                shiftEnd = shiftDate.AddDays(1).AddHours(8);
            }

            var todayTrans = _context.Transactions
                .Where(t => t.InvId == invDbIdInv && t.CreatedAt >= shiftStart && t.CreatedAt < shiftEnd);

            var totalOk = todayTrans.Count(t => t.Status == "OK");
            var totalPolesh = todayTrans.Count(t => t.Status == "POLESH");
            var totalTrans = totalOk + totalPolesh;

            ViewBag.TotalOk = totalOk;
            ViewBag.TotalPolesh = totalPolesh;
            ViewBag.PercentOk = totalTrans == 0 ? 0 : Math.Round((double)totalOk / totalTrans * 100, 1);
            ViewBag.PercentPolesh = totalTrans == 0 ? 0 : Math.Round((double)totalPolesh / totalTrans * 100, 1);

            var ngDetailsForDisplay = _context.Transactions
                .Where(t => t.InvId == invDbIdInv && t.CreatedAt >= start && t.CreatedAt < end &&
                           (t.Status == "NG" || t.Status == "POLESH"))
                .Where(t => t.NgDetailId != null)
                .Join(
                    _context.Ng_Categories,
                    trans => trans.NgDetailId,
                    ngCat => ngCat.Id,
                    (trans, ngCat) => new { Transaction = trans, Category = ngCat }
                )
                .GroupBy(x => new { x.Category.Category, x.Category.SubCategory, x.Transaction.Role })
                .Select(g => new NgSummaryViewModel
                {
                    Category = g.Key.Category,
                    SubCategory = g.Key.SubCategory,
                    Role = g.Key.Role,
                    Count = g.Count()
                })
                .ToList();

            ViewBag.NgDetailsForDisplay = ngDetailsForDisplay;

            var selectedRobotId = HttpContext.Session.GetString("SelectedRobotId");
            PopulateRobotDropdown(selectedRobotId);

            ViewBag.InvId = invDbId;
            ViewBag.invDbIdInv = invDbIdInv;
            ViewBag.DataType = dataType;

            var viewModel = new InventoryTransactionViewModel
            {
                Inventory = inventory,
                Transaction = transaction?.Transaction
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult ScanInventory1(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId))
            {
                TempData["Message"] = "Isi ID Inventory terlebih dahulu";
                return RedirectToAction("Scan");
            }

            var inventoryData = _context.Inventories.FirstOrDefault(i => i.InvId == inventoryId);
            if (inventoryData != null)
            {
                TempData["DataType"] = "inventory";
                return RedirectToAction(nameof(Index), new { inventoryId });
            }
            else
            {
                var transactionData = _context.Transactions.FirstOrDefault(t => t.Barcode == inventoryId);
                if (transactionData != null)
                {
                    TempData["DataType"] = "transaction";
                    return RedirectToAction(nameof(Index), new { inventoryId });
                }
                else
                {
                    TempData["Message"] = "Data inventory tidak ada";
                    return RedirectToAction("Index");
                }
            }
        }

        [HttpPost]
        public IActionResult SubmitTransaction(long InvId, string InventoryId, string status, long RobotId, bool isReturn, bool isOppositeShit)
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
            DateTime createdAt = (shift == "2" && now.Hour < 8) ? now.AddDays(-1) : now;

            // Cek transaksi yang sudah ada dengan Barcode, Role, dan Status yang sama
            var existingTransaction = _context.Transactions
                .Where(t => t.Barcode == InventoryId && t.Role == role)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();

            if (existingTransaction != null)
            {
                bool updated = false;

                if (!isReturn)
                {
                    // Pengecekan status yang sama dan status yang memerlukan konfirmasi
                    if (status == existingTransaction.Status)
                    {
                        TempData["ErrorMessage"] = $"Tidak dapat menggunakan status {status} karena sama dengan status sebelumnya.";
                        TempData["ExistingStatus"] = existingTransaction.Status;
                        TempData["InventoryId"] = InventoryId;
                        return RedirectToAction("Index", new { inventoryId = InventoryId });
                    }

                    if (existingTransaction.Status == "OK" || existingTransaction.Status == "POLESH" || existingTransaction.Status == "NG")
                    {
                        TempData["ShowReturnConfirmation"] = "true";
                        TempData["ExistingStatus"] = existingTransaction.Status;
                        TempData["NewStatus"] = status;
                        TempData["InventoryId"] = InventoryId;
                        return RedirectToAction("Index", new { inventoryId = InventoryId });
                    }
                }

                if (isOppositeShit)
                {
                    existingTransaction.Shift = existingTransaction.Shift;
                    existingTransaction.OppositeShift = existingTransaction.Shift == "1" ? "2" : "1";
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
                if (existingTransaction.Shift != shift)
                {
                    existingTransaction.Shift = shift;
                    updated = true;
                }
                if (existingTransaction.Is_Return != isReturn)
                {
                    existingTransaction.Is_Return = isReturn;
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
            else
            {
                // Menambahkan transaksi baru
                var transaction = new Transactions
                {
                    InvId = InvId,
                    Barcode = InventoryId,
                    RobotId = RobotId,
                    UserId = userId,
                    LineId = lineId,
                    Role = role,
                    Status = status,
                    NgDetailId = 0,
                    Qty = 1,
                    Shift = shift,
                    OppositeShift = "0",
                    CreatedAt = DateTime.Now,
                    Is_Return = isReturn
                };

                _context.Transactions.Add(transaction);
                _context.SaveChanges();
                TempData["Message"] = "Transaksi berhasil ditambahkan.";
            }

            HttpContext.Session.SetString("SelectedRobotId", RobotId.ToString());
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

            // Input validation
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

            // Get current year and month for counter
            var currentYearMonth = DateTime.Now.ToString("yyyyMM");

            // Retrieve or create counter for this InvId and YearMonth
            var counter = _context.Qr_Counter
                .FirstOrDefault(q => q.InvId == inventory.InvId && q.YearMonth == currentYearMonth);

            if (counter == null)
            {
                int newId = _context.Qr_Counter.Any() ? _context.Qr_Counter.Max(q => q.Id) + 1 : 1;
                counter = new Qr_Counter
                {
                    Id = newId,
                    InvId = inventory.InvId,
                    YearMonth = currentYearMonth,
                    LastNumber = 0
                };
                _context.Qr_Counter.Add(counter);
                _context.SaveChanges();
            }

            int startNumber = counter.LastNumber + 1;
            counter.LastNumber += jumlahQr; // Update counter
            _context.SaveChanges(); // Save to DB

            var pdfStream = new MemoryStream();
            List<string> QRList = new List<string>();

            // Generate QR codes
            for (int i = 0; i < jumlahQr; i++)
            {
                int nomorUrut = startNumber + i;
                var tanggal = DateTime.Now.ToString("yyyyMMdd");
                string rawQRDataString = $"{inventory.InvId}{project}{color}{tanggal}{robot}{lineId}{userGroup}{nomorUrut}"; // Pad nomorUrut to 6 digits
                string qrDataString = rawQRDataString.Replace(" ", "");

                QRList.Add(qrDataString);

                // Generate QR code image
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrDataString, QRCodeGenerator.ECCLevel.Q);
                var qrCodePngByte = new PngByteQRCode(qrCodeData);
                byte[] qrImageBytes = qrCodePngByte.GetGraphic(10);

                // Save transaction
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
                    OppositeShift = "0",
                    CreatedAt = DateTime.Now,
                    Is_Return = false
                };
                _context.Transactions.Add(transaction);
                _context.SaveChanges();
            }

            // Generate PDF
            float qrWidth = 73f;
            float qrHeight = 73f;
            float spaceX = 15f;   // jarak antar QR horizontal
            float spaceY = 1f;  // jarak antar QR vertical

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
                            mainColumn.Item()
                                .PaddingLeft(10f)
                                .PaddingTop(-24f)
                                .PaddingBottom(spaceY) // jarak antar baris
                                .Row(row =>
                                {
                                    for (int j = 0; j < 6 && (i + j) < jumlahQr; j++)
                                    {
                                        string QRCombine = QRList[i + j];
                                        using var qrGenerator = new QRCodeGenerator();
                                        using var qrCodeData = qrGenerator.CreateQrCode(QRCombine, QRCodeGenerator.ECCLevel.Q);
                                        var qrCodePngByte = new PngByteQRCode(qrCodeData);
                                        byte[] qrImageBytes = qrCodePngByte.GetGraphic(10);

                                        row.ConstantItem(qrWidth + spaceX) // total lebar termasuk jarak
                                            .AlignCenter()
                                            .Container()
                                            .PaddingRight(spaceX)
                                            .Width(qrWidth)   // lebar QR di dalam
                                            .Height(qrHeight) // tinggi QR di dalam
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