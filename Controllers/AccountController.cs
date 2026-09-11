using Microsoft.AspNetCore.Mvc;
using Leave_Management_System.Models;
using Leave_Management_System.Data;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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
           
                // Check if user is already authenticated
            if (User.Identity?.IsAuthenticated == true)
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
                    .Include(u => u.RoleMaster)
                    .FirstOrDefaultAsync(u => u.PinNo == model.PinNo && u.Status == "ACTIVE");

                if (user != null)
                {
                    var result = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);

                    if (result == PasswordVerificationResult.Success)
                    {
                        
                        var claims = new List<Claim>
                                            {
                                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                                new Claim(ClaimTypes.Name, user.Name),
                                                new Claim(ClaimTypes.Role, user.RoleMaster?.RoleName ?? "Unknown")
                                            };

                        var identity = new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);

                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,principal);

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid User ID or Password");
            }

            return View(model);
        }

        //public IActionResult Logout()
        public async Task<IActionResult> Logout()
        {
            // HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }
    }

}
