using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LoginMVCApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");
            return View(_context.Users.ToList());
        }

        [HttpGet("Users/Create", Name = "UserCreateForm")]
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            PopulateLines();
            return View();
        }

        [HttpPost("Users/Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Users users)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (_context.Users.Any(u => u.Email == users.Email))
            {
                ModelState.AddModelError("Email", "Email sudah digunakan");
            }

            if (!ModelState.IsValid)
            {
                PopulateLines();
                return View(users);
            }

            users.CreatedBy = HttpContext.Session.GetString("Email") ?? "system";
            users.CreatedAt = DateTime.Now;
            users.UpdatedAt = DateTime.Now;
            users.Password = BCrypt.Net.BCrypt.HashPassword(users.Password);

            try
            {
                _context.Users.Add(users);
                _context.SaveChanges();
                TempData["success"] = "User Berhasil Ditambahkan.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Gagal Menyimpan: " + ex.Message);
                PopulateLines();
                return View(users);
            }
        }

        public IActionResult Details(long? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult Edit(long? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                LineId = user.LineId,
                IsActive = user.IsActive
            };

            ViewBag.Lines = new SelectList(_context.Lines, "Id", "Name", user.LineId);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(long id, EditUserViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id != model.Id) return NotFound();

            var existingUser = _context.Users.FirstOrDefault(u => u.Id == id);
            if (existingUser == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Role = model.Role;
                existingUser.LineId = model.LineId;
                existingUser.IsActive = model.IsActive;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                _context.SaveChanges();
                TempData["success"] = "User Berhasil Diperbarui.";
                return RedirectToAction("Index");
            }

            // Ini penting saat kembali ke View setelah gagal validasi
            ViewBag.Lines = new SelectList(_context.Lines.ToList(), "Id", "Name", model.LineId);
            return View(model);
        }

        public IActionResult Delete(long? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(long id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");

            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateLines()
        {
            var lines = _context.Lines
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Nama
                }).ToList();

            ViewBag.Lines = new SelectList(lines, "Value", "Text");
        }

    }
}
