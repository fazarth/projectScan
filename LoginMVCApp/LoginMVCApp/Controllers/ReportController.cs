using LoginMVCApp.Data;
using LoginMVCApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
                .Where(t => t.Status != "PRINTED")
                .AsQueryable();

            // filter tanggal
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);
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
    }
}
