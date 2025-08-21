using LoginMVCApp.Data;
using LoginMVCApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;


namespace LoginMVCApp.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;
        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return _context.Transactions != null ?
                          View(await _context.Transactions.ToListAsync()) :
                          Problem("Entity set 'AppDbContext.Transactions'  is null.");
        }

        public async Task<IActionResult> DailyReport(DateTime? startDate, DateTime? endDate, string? shift, int page = 1, int pageSize = 10)
        {
            var query = _context.Transactions
                .Include(t => t.Inventory)
                .Include(t => t.NgCategory)
                .Include(t => t.Robot)
                .Include(t => t.Line)
                .Where(t => t.Status != "PRINTED")
                .AsQueryable();

            // filter tanggal
            if (startDate.HasValue && endDate.HasValue)
            {
                var start = startDate.Value.Date;
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1); // sampai 23:59:59.9999999

                query = query.Where(t => t.CreatedAt >= start && t.CreatedAt <= end);
            }

            // filter shift
            // seharusnya
            if (!string.IsNullOrEmpty(shift))
            {
                query = query.Where(t => t.Shift == shift);
                ViewBag.Shift = shift; // biar tetap ke-select di dropdown
            }


            // total data
            var totalItems = await query.CountAsync();

            // ambil data sesuai page
            var data = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.StartIndex = (page - 1) * pageSize;

            ViewBag.ShiftList = new SelectList(
                new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "Shift 1" },
                    new SelectListItem { Value = "2", Text = "Shift 2" }
                },
                "Value", "Text", ViewBag.Shift
            );
            return View("~/Views/Report/DailyReport.cshtml", data);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDailyReport(DateTime? startDate, DateTime? endDate, string shift)
        {
            var query = _context.Transactions
                .Include(t => t.Inventory)
                .Include(t => t.NgCategory)
                .Include(t => t.Robot)
                .Include(t => t.Line)
                .Where(t => t.Status != "PRINTED")
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);
            if (!string.IsNullOrEmpty(shift))
                query = query.Where(t => t.Shift == shift);

            var data = await query.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Daily Report");

                // Header
                worksheet.Cell(1, 1).Value = "No";
                worksheet.Cell(1, 2).Value = "Inventory Id";
                worksheet.Cell(1, 3).Value = "Robot";
                worksheet.Cell(1, 4).Value = "Part";
                worksheet.Cell(1, 5).Value = "Project";
                worksheet.Cell(1, 6).Value = "Type";
                worksheet.Cell(1, 7).Value = "Color";
                worksheet.Cell(1, 8).Value = "Status";
                worksheet.Cell(1, 9).Value = "Detail NG";

                // Data
                int row = 2;
                int no = 1;
                foreach (var t in data)
                {
                    worksheet.Cell(row, 1).Value = no++;
                    worksheet.Cell(row, 2).Value = t.Barcode ?? "-";
                    worksheet.Cell(row, 3).Value = t.Robot?.Nama ?? "-";
                    worksheet.Cell(row, 4).Value = t.Inventory.PartName ?? "-";
                    worksheet.Cell(row, 5).Value = t.Inventory.Project ?? "-";
                    worksheet.Cell(row, 6).Value = t.Inventory.Tipe ?? "-";
                    worksheet.Cell(row, 7).Value = t.Inventory.Warna?? "-";
                    worksheet.Cell(row, 8).Value = t.Status ?? "-";
                    worksheet.Cell(row, 9).Value = t.NgCategory?.SubCategory?? "-";
                    row++;
                }

                string fileName = $"DailyReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
    }
}
