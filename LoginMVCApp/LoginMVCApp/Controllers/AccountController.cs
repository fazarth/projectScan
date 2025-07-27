using Microsoft.AspNetCore.Mvc;
using LoginMVCApp.Data;
using LoginMVCApp.Models;
using System.Linq;

namespace LoginMVCApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        [ValidateAntiForgeryToken]
        public IActionResult Index()
        {
            //penjagaan akses
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Email")))
                return RedirectToAction("Login", "Account");

            HttpContext.Session.GetString("FullName");
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Email")))
            {
                // Sudah login → arahkan ke dashboard
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(Users users)
        {
            if (string.IsNullOrWhiteSpace(users.Email) || string.IsNullOrWhiteSpace(users.Password))
            {
                ViewBag.Error = "Email dan Password wajib diisi!";
                return View(users);
            }
            var user = _context.Users.FirstOrDefault(u => u.Email == users.Email);

            if (user == null)
            {
                // Email tidak ditemukan
                ViewBag.Error = "Email tidak ditemukan!";
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(users.Password, user.Password))
            {
                // Password salah
                ViewBag.Error = "Password salah!";
                return View();
            }

            if (!user.IsActive)
            {
                // Akun tidak aktif
                ViewBag.Error = "Akun Anda tidak aktif. Silakan hubungi administrator.";
                return View();
            }

            // Jika semua pengecekan berhasil, simpan data ke session
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Role", user.Role);

            // Redirect berdasarkan role
            switch (user.Role)
            {
                case "Admin":
                    return RedirectToAction("Index", "Home");
                case "Checker":
                    return RedirectToAction("Index", "Checker");
                case "Poles":
                    return RedirectToAction("Index", "Poles");
                default:
                    return RedirectToAction("AccessDenied", "Account");
            }

            // Jika tidak ada yang valid, tampilkan pesan kesalahan
            ViewBag.Error = "Terjadi kesalahan, silakan coba lagi.";
            return View();

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // hapus session login
            return RedirectToAction("Login");
        }
        [HttpGet]
        [ValidateAntiForgeryToken]
        public IActionResult Welcome()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Email")))
                return RedirectToAction("Login", "Account");
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
