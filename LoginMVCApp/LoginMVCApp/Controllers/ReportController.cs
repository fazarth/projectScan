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

        public async Task<IActionResult> DailyReport(DateTime? startDate, DateTime? endDate, string? shift)
        {
            var query = _context.Transactions
                .Include(t => t.Inventory)
                .Include(t => t.NgCategory)
                .Include(t => t.Robot)
                .Include(t => t.Line)
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

            // Grouping data 
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

            ViewBag.ShiftList = new SelectList(
                new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "Shift 1" },
                    new SelectListItem { Value = "2", Text = "Shift 2" }
                },
                "Value", "Text", ViewBag.Shift
           );

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
                    Date = t.CreatedAt.Date,
                    Line = t.LineId,
                    Shift = t.Shift,
                    Robot = t.Robot.Nama,
                    Part = t.Inventory.PartName,
                    Project = t.Inventory.Project,
                    Type = t.Inventory.Tipe,
                    InvId = t.Inventory.InvId,
                    Color = t.Inventory.Warna
                })
                .Select(g => new DailyReportDto
                {
                    Date = g.Key.Date,
                    Line = g.Key.Line.ToString(),
                    Shift = g.Key.Shift,
                    Robot = g.Key.Robot,
                    Part = g.Key.Part,
                    Project = g.Key.Project,
                    Type = g.Key.Type,
                    InvId = g.Key.InvId,
                    Color = g.Key.Color,

                    QtyOk = g.Count(x => x.Status == "OK"),
                    QtyNg = g.Count(x => x.Status == "NG" || x.Status == "POLESH"),
                    QtyNgPolish = g.Count(x => x.Status == "OK" && x.Role == "Poles"),
                    QtyNgTouchUp = g.Count(x => x.Status == "NG"),

                    // --- mapping DetailNg ---
                    DetailNg = g
                        .GroupBy(x => x.NgCategory != null ? x.NgCategory.SubCategory : "UNKNOWN")
                        .ToDictionary(x => x.Key, x => x.Count()),
                })
                .ToList();

            var ngColumns = new Dictionary<string, List<string>>
                {
                    { "BINTIK", new List<string>{ "BINTIK CAT", "KOTORAN LUAR" } },
                    { "KEBA", new List<string>{ "SERAT BENANG" } },
                    { "HAJIKI", new List<string>{ "OIL", "AIR / BLISTER" } },
                    { "SCRATCH", new List<string>{ "GORES", "EX-MIGAKI" } },
                    { "NG ROBOT", new List<string>{ "SAGING / LELEH", "ABSORB", "TIPIS", "ORANGE PEEL", "MOTLING / BELANG", "BEDA WARNA", "OVER MASKING", "GALER", "POPPING", "LIPTING", "HANDLING" } },
                    { "NG INJECTION", new List<string>{ "OVER CUT", "FLOW MARK", "SILVER / NYEREP", "CRACK / RETAK", "PIN PATAH", "BURRY", "BUBBLE", "VACUM" } },
                    { "LAIN-LAIN", new List<string>{ "NYAMUK", "RAMBUT" } }
                };

            // --- Buat Excel ---
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Daily Report");

                int headerRow1 = 4; // row pertama untuk kategori besar
                int headerRow2 = 5; // row kedua untuk detail

                // --- Header Awal (info umum) ---
                string[] headers = { "TANGGAL","SHIFT","LINE","NAMA ROBOT","PART","TYPE","CUST","INV ID","WARNA","TOTAL PROD","SOZAI","MIGAKI","OK","NG POLISH","NG TOUCH UP","NG MIGAKI" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(headerRow1, i + 2).Value = headers[i];
                    ws.Range(headerRow1, i + 2, headerRow2, i + 2).Merge(); // merge 2 baris ke bawah
                    ws.Cell(headerRow1, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(headerRow1, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Cell(headerRow1, i + 2).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                int col = headers.Length + 2;

                // --- Header NG ---
                foreach (var group in ngColumns)
                {
                    int startCol = col;
                    foreach (var sub in group.Value)
                    {
                        ws.Cell(headerRow2, col).Value = sub;
                        ws.Cell(headerRow2, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        col++;
                    }
                    // Merge kategori
                    ws.Range(headerRow1, startCol, headerRow1, col - 1).Merge();
                    ws.Cell(headerRow1, startCol).Value = group.Key;
                    ws.Cell(headerRow1, startCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell("R4").Style.Fill.BackgroundColor = XLColor.FromHtml("#66CCFF");
                    ws.Cell("T4").Style.Fill.BackgroundColor = XLColor.FromHtml("#663399");
                    ws.Cell("U4").Style.Fill.BackgroundColor = XLColor.FromHtml("#CC6600");
                    ws.Cell("W4").Style.Fill.BackgroundColor = XLColor.FromHtml("#0000FF");
                    ws.Cell("Y4").Style.Fill.BackgroundColor = XLColor.FromHtml("#00FF00");
                    ws.Cell("AJ4").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFA500");
                    ws.Cell("AR4").Style.Fill.BackgroundColor = XLColor.FromHtml("#FF99CC");
                    ws.Range("R5:AS5").Style.Fill.BackgroundColor = XLColor.Yellow;
                            //ws.Cell(headerRow2, startCol).Style.Fill.BackgroundColor = XLColor.Green;
                }

                // --- Isi Data ---
                ws.Cell("K2").FormulaA1 = "=K3/$K$3";
                ws.Cell("K2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("K3").Style.Fill.BackgroundColor = XLColor.FromHtml("#66CCFF");
                ws.Cell("K3").FormulaA1 = "=SUBTOTAL(9,K6:K4515)";
                ws.Cell("L2").FormulaA1 = "=L3/$K$3";
                ws.Cell("L2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("L3").Style.Fill.BackgroundColor = XLColor.FromHtml("#FF99CC");
                ws.Cell("L3").FormulaA1 = "=SUBTOTAL(9,L6:L4515)";
                ws.Cell("M2").FormulaA1 = "=M3/$K$3";
                ws.Cell("M2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("M3").Style.Fill.BackgroundColor = XLColor.FromHtml("#FF99CC");
                ws.Cell("M3").FormulaA1 = "=SUBTOTAL(9,M6:M4515)";
                ws.Cell("N2").FormulaA1 = "=N3/$K$3";
                ws.Cell("N2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("N3").Style.Fill.BackgroundColor = XLColor.Yellow;
                ws.Cell("N3").FormulaA1 = "=SUBTOTAL(9,N6:N4515)";
                ws.Cell("O2").FormulaA1 = "=O3/$K$3";
                ws.Cell("O2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("O3").Style.Fill.BackgroundColor = XLColor.Yellow;
                ws.Cell("O3").FormulaA1 = "=SUBTOTAL(9,O6:O4515)";
                ws.Cell("P2").FormulaA1 = "=P3/$K$3";
                ws.Cell("P2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("P3").Style.Fill.BackgroundColor = XLColor.Yellow;
                ws.Cell("P3").FormulaA1 = "=SUBTOTAL(9,P6:P4515)";
                ws.Cell("Q2").FormulaA1 = "=Q3/$K$3";
                ws.Cell("Q2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("Q3").Style.Fill.BackgroundColor = XLColor.Yellow;
                ws.Cell("Q3").FormulaA1 = "=SUBTOTAL(9,Q6:Q4515)";
                
                ws.Cell("R2").FormulaA1 = "=R3/$K$3";
                ws.Cell("R2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("R3").FormulaA1 = "=SUBTOTAL(9,R6:R4515)";
                ws.Cell("S2").FormulaA1 = "=S3/$K$3";
                ws.Cell("S2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("S3").FormulaA1 = "=SUBTOTAL(9,S6:S4515)";
                ws.Cell("T2").FormulaA1 = "=T3/$K$3";
                ws.Cell("T2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("T3").FormulaA1 = "=SUBTOTAL(9,T6:T4515)";
                ws.Cell("U2").FormulaA1 = "=U3/$K$3";
                ws.Cell("U2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("U3").FormulaA1 = "=SUBTOTAL(9,U6:U4515)";
                ws.Cell("V2").FormulaA1 = "=V3/$K$3";
                ws.Cell("V2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("V3").FormulaA1 = "=SUBTOTAL(9,V6:V4515)";
                ws.Cell("W2").FormulaA1 = "=W3/$K$3";
                ws.Cell("W2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("W3").FormulaA1 = "=SUBTOTAL(9,W6:W4515)";
                ws.Cell("X2").FormulaA1 = "=X3/$K$3";
                ws.Cell("X2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("X3").FormulaA1 = "=SUBTOTAL(9,X6:X4515)";
                ws.Cell("Y2").FormulaA1 = "=Y3/$K$3";
                ws.Cell("Y2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("Y3").FormulaA1 = "=SUBTOTAL(9,Y6:Y4515)";
                ws.Cell("Z2").FormulaA1 = "=Z3/$K$3";
                ws.Cell("Z2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("Z3").FormulaA1 = "=SUBTOTAL(9,Z6:Z4515)";
                ws.Cell("AA2").FormulaA1 = "=AA3/$K$3";
                ws.Cell("AA2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AA3").FormulaA1 = "=SUBTOTAL(9,AA6:AA4515)";
                ws.Cell("AB2").FormulaA1 = "=AB3/$K$3";
                ws.Cell("AB2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AB3").FormulaA1 = "=SUBTOTAL(9,AB6:AB4515)";
                ws.Cell("AC2").FormulaA1 = "=AC3/$K$3";
                ws.Cell("AC2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AC3").FormulaA1 = "=SUBTOTAL(9,AC6:AC4515)";
                ws.Cell("AD2").FormulaA1 = "=AD3/$K$3";
                ws.Cell("AD2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AD3").FormulaA1 = "=SUBTOTAL(9,AD6:AD4515)";
                ws.Cell("AE2").FormulaA1 = "=AE3/$K$3";
                ws.Cell("AE2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AE3").FormulaA1 = "=SUBTOTAL(9,AE6:AE4515)";
                ws.Cell("AF2").FormulaA1 = "=AF3/$K$3";
                ws.Cell("AF2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AF3").FormulaA1 = "=SUBTOTAL(9,AF6:AF4515)";
                ws.Cell("AG2").FormulaA1 = "=AG3/$K$3";
                ws.Cell("AG2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AG3").FormulaA1 = "=SUBTOTAL(9,AG6:AG4515)";
                ws.Cell("AH2").FormulaA1 = "=AH3/$K$3";
                ws.Cell("AH2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AH3").FormulaA1 = "=SUBTOTAL(9,AH6:AH4515)";
                ws.Cell("AI2").FormulaA1 = "=AI3/$K$3";
                ws.Cell("AI2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AI3").FormulaA1 = "=SUBTOTAL(9,AI6:AI4515)";
                ws.Cell("AJ2").FormulaA1 = "=AJ3/$K$3";
                ws.Cell("AJ2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AJ3").FormulaA1 = "=SUBTOTAL(9,AJ6:AJ4515)";
                ws.Cell("AK2").FormulaA1 = "=AK3/$K$3";
                ws.Cell("AK2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AK3").FormulaA1 = "=SUBTOTAL(9,AK6:AK4515)";
                ws.Cell("AL2").FormulaA1 = "=AL3/$K$3";
                ws.Cell("AL2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AL3").FormulaA1 = "=SUBTOTAL(9,AL6:AL4515)";
                ws.Cell("AM2").FormulaA1 = "=AM3/$K$3";
                ws.Cell("AM2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AM3").FormulaA1 = "=SUBTOTAL(9,AM6:AM4515)";
                ws.Cell("AN2").FormulaA1 = "=AN3/$K$3";
                ws.Cell("AN2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AN3").FormulaA1 = "=SUBTOTAL(9,AN6:AN4515)";
                ws.Cell("AO2").FormulaA1 = "=AO3/$K$3";
                ws.Cell("AO2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AO3").FormulaA1 = "=SUBTOTAL(9,AO6:AO4515)";
                ws.Cell("AP2").FormulaA1 = "=AP3/$K$3";
                ws.Cell("AP2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AP3").FormulaA1 = "=SUBTOTAL(9,AP6:AP4515)";
                ws.Cell("AQ2").FormulaA1 = "=AQ3/$K$3";
                ws.Cell("AQ2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AQ3").FormulaA1 = "=SUBTOTAL(9,AQ6:AQ4515)";
                ws.Cell("AR2").FormulaA1 = "=AR3/$K$3";
                ws.Cell("AR2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AR3").FormulaA1 = "=SUBTOTAL(9,AR6:AR4515)";
                ws.Cell("AS2").FormulaA1 = "=AS3/$K$3";
                ws.Cell("AS2").Style.NumberFormat.Format = "0.00%";
                ws.Cell("AS3").FormulaA1 = "=SUBTOTAL(9,AS6:AS4515)";


                int row = headerRow2 + 1;
                foreach (var item in grouped)
                {
                    ws.Cell(row, 2).Value = item.Date.ToString("dd/MM/yyyy");
                    ws.Cell(row, 3).Value = item.Shift;
                    ws.Cell(row, 4).Value = item.Line;
                    ws.Cell(row, 5).Value = item.Robot;
                    ws.Cell(row, 6).Value = item.Part;
                    ws.Cell(row, 7).Value = item.Type;
                    ws.Cell(row, 8).FormulaA1 = $"=LEFT(I{row},3)";
                    ws.Cell(row, 9).Value = item.InvId;
                    ws.Cell(row, 10).Value = item.Color;
                    ws.Cell(row, 11).Value = item.TotalProd;
                    ws.Cell(row, 12).FormulaA1 = $"=K{row}-M{row}";
                    ws.Cell(row, 13).Value = item.Migaki;
                    ws.Cell(row, 14).FormulaA1 = $"=K{row}-O{row}-P{row}-Q{row}";
                    ws.Cell(row, 15).Value = item.QtyNgPolish;
                    ws.Cell(row, 16).Value = item.QtyNgTouchUp;
                    ws.Cell(row, 17).Value = item.QtyNgMigaki;

                    int dataCol = headers.Length + 2;
                    foreach (var group in ngColumns)
                    {
                        foreach (var sub in group.Value)
                        {
                            ws.Cell(row, dataCol).Value = item.DetailNg.ContainsKey(sub) ? item.DetailNg[sub] : "-";
                            dataCol++;
                        }
                    }

                    row++;
                }

                ws.Columns().AdjustToContents();

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
