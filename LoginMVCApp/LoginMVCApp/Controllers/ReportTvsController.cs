using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.Helpers;
using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoginMVCApp.Controllers
{
    public class ReportTvsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportTvsController(AppDbContext context)
        {
            _context = context;
        }

        //public async Task<IActionResult> Dashboard()
        //{
        //    // line & shift user (bisa dari ViewBag/Claims/Session, aku kasih default 1,1 dulu)
        //    int lineId = Convert.ToInt32(User.FindFirst("line_id")?.Value ?? "1");
        //    string shift = User.FindFirst("shift")?.Value ?? "1";

        //    var today = DateTime.Today;
        //    var tomorrow = today.AddDays(1);

        //    // ambil report terakhir utk line tsb
        //    var rpt = await _context.ReportTv
        //        .AsNoTracking()
        //        .Where(r => r.Line == lineId)
        //        .OrderByDescending(r => r.Id)
        //        .FirstOrDefaultAsync();

        //    var vm = new DashboardViewModel
        //    {
        //        LineId = lineId,
        //        Shift = shift,
        //        Tanggal = today,
        //        ProductionPlan = rpt?.ProductionPlan ?? 0,
        //        ProductivityPct = rpt?.Productivity ?? 0,
        //        StraightPassPct = rpt?.StraightPass ?? 0
        //    };

        //    // Target per jam (anggap 8 jam)
        //    vm.TargetPerJam = vm.JamKerja > 0 ?
        //        (int)Math.Round((decimal)vm.ProductionPlan / vm.JamKerja, MidpointRounding.AwayFromZero)
        //        : 0;

        //    int jamBerjalan = ShiftTime.GetJamBerjalan(DateTime.Now, shift, vm.JamKerja);
        //    vm.TargetCumulativeSaatIni = vm.TargetPerJam * jamBerjalan;

        //    // Filter transaksi per-hari & shift & line
        //    var qBase = _context.Transactions.AsNoTracking()
        //        .Where(t => t.LineId == lineId
        //                    && t.CreatedAt >= today
        //                    && t.CreatedAt < tomorrow
        //                    && t.Shift == shift);

        //    // Achieve
        //    vm.AchieveCount = await qBase.CountAsync();
        //    vm.AchievePct = vm.TargetCumulativeSaatIni > 0
        //        ? Math.Round((decimal)vm.AchieveCount * 100m / vm.TargetCumulativeSaatIni, 1)
        //        : 0;

        //    // Checker breakdown
        //    var qChecker = qBase.Where(t => t.Role == "Checker");
        //    vm.CheckerOk = await qChecker.CountAsync(t => t.Status == "OK");
        //    vm.CheckerPolesh = await qChecker.CountAsync(t => t.Status == "POLESH");
        //    var checkerTotal = vm.CheckerOk + vm.CheckerPolesh;
        //    vm.CheckerOkPct = checkerTotal > 0 ? Math.Round(vm.CheckerOk * 100m / checkerTotal, 1) : 0;
        //    vm.CheckerPoleshPct = 100m - vm.CheckerOkPct;

        //    // Poles panel kanan
        //    var qPoles = qBase.Where(t => t.Role == "Poles");
        //    vm.PolesOk = await qPoles.CountAsync(t => t.Status == "OK");
        //    vm.PolesNg = await qPoles.CountAsync(t => t.Status == "NG" || t.Status == "REPAIR");

        //    // Row bawah
        //    vm.SpQcOk = vm.CheckerOk;
        //    vm.PolQcOk = vm.PolesOk;
        //    vm.Repair = await qBase.CountAsync(t => t.Status == "NG" || t.Status == "REPAIR");

        //    return View(vm);
        //}

        // GET: ReportTvs
        public async Task<IActionResult> Index()
        {
              return _context.ReportTv != null ? 
                          View(await _context.ReportTv.ToListAsync()) :
                          Problem("Entity set 'AppDbContext.ReportTv'  is null.");
        }

        // GET: ReportTvs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ReportTv == null)
            {
                return NotFound();
            }

            var reportTv = await _context.ReportTv
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reportTv == null)
            {
                return NotFound();
            }

            return View(reportTv);
        }

        // GET: ReportTvs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ReportTvs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductionPlan,StraightPass,TamuStraight,TamuPolesOk,TamuPolesRepair,Line")] ReportTv reportTv)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reportTv);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reportTv);
        }

        // GET: ReportTvs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.ReportTv == null)
            {
                return NotFound();
            }

            var reportTv = await _context.ReportTv.FindAsync(id);
            if (reportTv == null)
            {
                return NotFound();
            }
            return View(reportTv);
        }

        // POST: ReportTvs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductionPlan,StraightPass,TamuStraight,TamuPolesOk,TamuPolesRepair,Line")] ReportTv reportTv)
        {
            if (id != reportTv.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reportTv);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReportTvExists(reportTv.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reportTv);
        }

        // GET: ReportTvs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.ReportTv == null)
            {
                return NotFound();
            }

            var reportTv = await _context.ReportTv
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reportTv == null)
            {
                return NotFound();
            }

            return View(reportTv);
        }

        // POST: ReportTvs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.ReportTv == null)
            {
                return Problem("Entity set 'AppDbContext.ReportTv'  is null.");
            }
            var reportTv = await _context.ReportTv.FindAsync(id);
            if (reportTv != null)
            {
                _context.ReportTv.Remove(reportTv);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReportTvExists(int id)
        {
          return (_context.ReportTv?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
