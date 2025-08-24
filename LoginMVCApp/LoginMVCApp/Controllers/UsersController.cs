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

            // Jika CreatedBy kosong dari form, isi dari Session
            if (string.IsNullOrEmpty(users.CreatedBy))
                users.CreatedBy = HttpContext.Session.GetString("FullName") ?? "system";

            users.CreatedAt = DateTime.Now;
            users.UpdatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                PopulateLines(users.LineId);
                return View(users);
            }

            // Cek apakah Email sudah ada
            if (_context.Users.Any(u => u.Email == users.Email))
            {
                ModelState.AddModelError("Email", "Email sudah digunakan");
            }

            //users.CreatedBy = HttpContext.Session.GetString("Email") ?? "system";
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
                PopulateLines(users.LineId);
                return View(users);
            }
        }

        //Nambah role harus edit ini di DB, isi role nya di dalam IN () setelah report
//          ALTER TABLE dbo.users
//          DROP CONSTRAINT CK__users__role__571DF1D5;

//          ALTER TABLE dbo.users
//          ADD CONSTRAINT CK__users__role__571DF1D5
//          CHECK(role IN ('Poles', 'Checker', 'Admin', 'Report'));

        public IActionResult Details(long? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.Include(u => u.Line).FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult Edit(long? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            //var user = _context.Users.Find(id);
            var user = _context.Users
                       .Include(u => u.Line)
                       .FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                LineId = user.LineId,
                IsActive = user.IsActive,
                UserGroup = user.UserGroup
            };

            PopulateLines(user.LineId);
            //ViewBag.Lines = new SelectList(_context.Lines, "Id", "Name", user.LineId);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(long id, EditUserViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id != model.Id) return NotFound();

            var existingUser = _context.Users.Include(u => u.Line).FirstOrDefault(u => u.Id == id);
            if (existingUser == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Role = model.Role;
                existingUser.LineId = model.LineId;
                existingUser.IsActive = model.IsActive;
                existingUser.UserGroup = model.UserGroup;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                TempData["success"] = "User Berhasil Diperbarui.";
                return RedirectToAction("Index");
            }

            PopulateLines(existingUser.LineId);
            return View(model);
        }

        public IActionResult Delete(long? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.Include(u => u.Line).FirstOrDefault(u => u.Id == id);
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

        private void PopulateLines(long? selectedId = null)
        {
            ViewBag.Lines = new SelectList(_context.Lines, "Id", "Nama", selectedId);
        }

    }
}