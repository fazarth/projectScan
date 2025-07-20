using LoginMVCApp.Data;
using LoginMVCApp.Models;
using LoginMVCApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
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

        // create new data users
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Users users)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("AccessDenied");

            // Jika CreatedBy kosong dari form, isi dari Session
            if (string.IsNullOrEmpty(users.CreatedBy))
                users.CreatedBy = HttpContext.Session.GetString("Username") ?? "system";

            users.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
                return View(users);

            // Cek apakah username sudah ada
            if (_context.Users.Any(u => u.Username == users.Username))
            {
                ModelState.AddModelError("Username", "Username sudah digunakan");
                return View(users);
            }

            // Hash password
            users.Password = BCrypt.Net.BCrypt.HashPassword(users.Password);

            try
            {
                _context.Users.Add(users);
                _context.SaveChanges();
                TempData["success"] = "User Berhasil Ditambahkan.";
                ModelState.Clear();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                ModelState.AddModelError("", "Gagal Menyimpan User: " + ex.Message);
                return View(users);
            }
        }

        public IActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, EditUserViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id != model.Id) return NotFound();

            var existingUser = _context.Users.FirstOrDefault(u => u.Id == id);
            if (existingUser == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingUser.FullName = model.FullName;
                existingUser.Username = model.Username;
                existingUser.Email = model.Email;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Role = model.Role;
                existingUser.IsActive = model.IsActive;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                _context.SaveChanges();
                TempData["success"] = "User Berhasil Diperbarui.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public IActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (id == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
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
    }
}
