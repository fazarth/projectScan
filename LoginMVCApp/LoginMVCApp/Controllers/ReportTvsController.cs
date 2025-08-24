using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LoginMVCApp.Data;
using LoginMVCApp.Models;

namespace LoginMVCApp.Controllers
{
    public class ReportTvsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportTvsController(AppDbContext context)
        {
            _context = context;
        }

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
