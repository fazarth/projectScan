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
    public class InventoriesController : Controller
    {
        private readonly AppDbContext _context;

        public InventoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Inventories
        public async Task<IActionResult> Index()
        {
              return _context.Inventories != null ? 
                          View(await _context.Inventories.ToListAsync()) :
                          Problem("Entity set 'AppDbContext.Inventories'  is null.");
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var Inventories = await _context.Inventories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (Inventories == null)
            {
                return NotFound();
            }

            return View(Inventories);
        }

        // GET: Inventories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Project,InvId,Warna,Tipe,PartNo,PartName,Barcode,CreatedAt,CreatedBy")] Inventories Inventories)
        {
            if (ModelState.IsValid)
            {
                _context.Add(Inventories);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(Inventories);
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var Inventories = await _context.Inventories.FindAsync(id);
            if (Inventories == null)
            {
                return NotFound();
            }
            return View(Inventories);
        }

        // POST: Inventories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Project,InvId,Warna,Tipe,PartNo,PartName,Barcode,CreatedAt,CreatedBy")] Inventories Inventories)
        {
            if (id != Inventories.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(Inventories);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoriesExists(Inventories.Id))
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
            return View(Inventories);
        }

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var Inventories = await _context.Inventories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (Inventories == null)
            {
                return NotFound();
            }

            return View(Inventories);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.Inventories == null)
            {
                return Problem("Entity set 'AppDbContext.Inventories'  is null.");
            }
            var Inventories = await _context.Inventories.FindAsync(id);
            if (Inventories != null)
            {
                _context.Inventories.Remove(Inventories);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InventoriesExists(long id)
        {
          return (_context.Inventories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
