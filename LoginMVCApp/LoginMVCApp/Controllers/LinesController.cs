using LoginMVCApp.Data;
using LoginMVCApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

public class LinesController : Controller
{
    private readonly AppDbContext _context;

    public LinesController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Create()
    {
        ViewBag.Lines = new SelectList(_context.Lines, "Id", "Name"); // Mengambil data Line dari database
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Users user)
    {
        if (ModelState.IsValid)
        {
            // Logika penyimpanan user baru
            _context.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Jika gagal validasi, isi ulang dropdown dan tampilkan kembali form
        ViewBag.Lines = new SelectList(_context.Lines, "Id", "Name", user.LineId);
        return View(user);
    }
}
