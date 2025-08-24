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

        public IActionResult Index()
        {            
             return View("Index");
        }

        public async Task<IActionResult> DailyReport(DateTime? startDate, DateTime? endDate, string? shift)
        {
            var query = _context.Transactions
                .Include(t => t.Inventory)
                .Include(t => t.NgCategory)
                .Include(t => t.Robot)
                .Where(t => t.Status != "PRINTED")
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                var start = startDate.Value.Date;
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.CreatedAt >= start && t.CreatedAt <= end);
            }

            if (!string.IsNullOrEmpty(shift))
            {
                query = query.Where(t => t.Shift == shift);
                ViewBag.Shift = shift;
            }

            var data = await query.ToListAsync();

            // Grouping data seperti di SS kedua
            var grouped = data
                .GroupBy(t => new {
                    Robot = t.Robot.Nama,
                    Part = t.Inventory.PartName,
                    Project = t.Inventory.Project,
                    Type = t.Inventory.Tipe,
                    Color = t.Inventory.Warna
                })
                .Select(g => new DailyReportDto
                {
                    Robot = g.Key.Robot,
                    Part = g.Key.Part,
                    Project = g.Key.Project,
                    Type = g.Key.Type,
                    Color = g.Key.Color,
                    QtyOk = g.Count(x => x.Status == "OK"),
                    QtyNg = g.Count(x => x.Status == "NG" || x.Status == "POLESH"),
                    DetailNg = g.Where(x => x.Status != "OK")
                                .GroupBy(x => x.NgCategory?.SubCategory ?? x.Status)
                                .ToDictionary(
                                    x => x.Key,
                                    x => x.Count()
                                )
                })
                .ToList();

            return View("~/Views/Report/DailyReport.cshtml", grouped);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDailyReport(DateTime? startDate, DateTime? endDate, string shift)
        {
            var query = _context.Transactions
                .Include(t => t.Inventory)
                .Include(t => t.NgCategory)
                .Include(t => t.Robot)
                .Where(t => t.Status != "PRINTED")
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);
            if (!string.IsNullOrEmpty(shift))
                query = query.Where(t => t.Shift == shift);

            var data = await query.ToListAsync();

            // --- Grouping ---
            var grouped = data
                .GroupBy(t => new {
                    Robot = t.Robot.Nama,
                    Part = t.Inventory.PartName,
                    Project = t.Inventory.Project,
                    Type = t.Inventory.Tipe,
                    Color = t.Inventory.Warna
                })
                .Select(g => new DailyReportDto
                {
                    Robot = g.Key.Robot,
                    Part = g.Key.Part,
                    Project = g.Key.Project,
                    Type = g.Key.Type,
                    Color = g.Key.Color,
                    QtyOk = g.Count(x => x.Status == "OK"),
                    QtyNg = g.Count(x => x.Status == "NG" || x.Status == "POLESH"),
                    DetailNg = g.Where(x => x.Status != "OK")
                                .GroupBy(x => x.NgCategory?.SubCategory ?? x.Status)
                                .ToDictionary(
                                    x => x.Key,
                                    x => x.Count()
                                )
                })
                .ToList();

            // --- Buat Excel ---
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Daily Report");

                // Header
                worksheet.Cell(1, 1).Value = "ROBOT";
                worksheet.Cell(1, 2).Value = "PART";
                worksheet.Cell(1, 3).Value = "PROJECT";
                worksheet.Cell(1, 4).Value = "TYPE";
                worksheet.Cell(1, 5).Value = "COLOR";
                worksheet.Cell(1, 6).Value = "QTY OK";
                worksheet.Cell(1, 7).Value = "% OK";
                worksheet.Cell(1, 8).Value = "QTY NG";
                worksheet.Cell(1, 9).Value = "DETAIL NG";

                int row = 2;
                foreach (var item in grouped)
                {
                    worksheet.Cell(row, 1).Value = item.Robot;
                    worksheet.Cell(row, 2).Value = item.Part;
                    worksheet.Cell(row, 3).Value = item.Project;
                    worksheet.Cell(row, 4).Value = item.Type;
                    worksheet.Cell(row, 5).Value = item.Color;
                    worksheet.Cell(row, 6).Value = item.QtyOk;
                    worksheet.Cell(row, 7).Value = $"{item.PercentOk:0.0}%";
                    worksheet.Cell(row, 8).Value = item.QtyNg;

                    // Detail NG dalam 1 cell (multiline)
                    if (item.DetailNg.Count > 0)
                    {
                        worksheet.Cell(row, 9).Value = string.Join(Environment.NewLine,
                            item.DetailNg.Select(d => $"{d.Key}: {d.Value} pcs ({((double)d.Value / (item.QtyOk + item.QtyNg) * 100):0.0}%)"));
                        worksheet.Cell(row, 9).Style.Alignment.WrapText = true;
                    }

                    row++;
                }

                // Auto-fit
                worksheet.Columns().AdjustToContents();

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
