using Microsoft.AspNetCore.Mvc;
using Leave_Management_System.Models;
using Leave_Management_System.Data;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;

namespace Leave_Management_System.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ApplicationDbContext _db;

        private readonly PasswordHasher<Users> _passwordHasher;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
            _passwordHasher = new PasswordHasher<Users>();

        }

        [HttpGet]
        public IActionResult Login()
        {
            // Check if user is already logged in
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login model)
        {
            if (ModelState.IsValid)
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.PinNo == model.PinNo && u.Status == "ACTIVE");

                if (user != null)
                {
                    var result = _passwordHasher.VerifyHashedPassword(user,user.Password,model.Password);

                    if (result == PasswordVerificationResult.Success)
                    {
                        HttpContext.Session.SetString("UserName", user.Name);
                        HttpContext.Session.SetInt32("UserId", user.Id);

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid User ID or Password");
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

}
