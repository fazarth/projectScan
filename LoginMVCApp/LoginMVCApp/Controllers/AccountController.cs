﻿using Microsoft.AspNetCore.Mvc;
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Login", "Account");

            HttpContext.Session.GetString("FullName");
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
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
            if (string.IsNullOrWhiteSpace(users.Username) || string.IsNullOrWhiteSpace(users.Password))
            {
                ViewBag.Error = "Username dan Password wajib diisi!";
                return View(users);
            }
            var user = _context.Users.FirstOrDefault(u => u.Username == users.Username);

            if (user != null && BCrypt.Net.BCrypt.Verify(users.Password, user.Password))
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName ?? "");
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Username atau Password salah!";
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Login", "Account");
            return View();
        }

        //Register
        //[HttpGet]
        //public IActionResult Register()
        //{
        //    var role = HttpContext.Session.GetString("Role");
        //    if (role != "Admin")
        //        return RedirectToAction("AccessDenied");

        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Register(Users model)
        //{
        //    var role = HttpContext.Session.GetString("Role");
        //    if (role != "Admin")
        //        return RedirectToAction("AccessDenied");

        //    if (!ModelState.IsValid)
        //        return View(model);

        //    var existing = _context.Users.FirstOrDefault(u => u.Username == model.Username || u.Email == model.Email);
        //    if (existing != null)
        //    {
        //        ModelState.AddModelError("Username", "Username atau email sudah digunakan");
        //        return View(model);
        //    }

        //    var newUser = new Users
        //    {
        //        FullName = model.FullName,
        //        Username = model.Username,
        //        Email = model.Email,
        //        PhoneNumber = model.PhoneNumber,
        //        Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
        //        Role = model.Role,
        //        IsActive = true,
        //        CreatedAt = DateTime.Now,
        //        CreatedBy = HttpContext.Session.GetString("Username")
        //    };

        //    _context.Users.Add(newUser);
        //    _context.SaveChanges();

        //    TempData["success"] = "Akun berhasil dibuat.";
        //    return RedirectToAction("Index", "Home");
        //}
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
