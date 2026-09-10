using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;


namespace Leave_Management_System.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            //// Check if user is authenticated
            //if (HttpContext.Session.GetString("UserId") == null)
            //{
            //    return RedirectToAction("Login", "Account");
            //}
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var viewModel = new DashboardViewModel
            {
                TotalUsers = _db.Users.Count(),
                TotalLeaves = _db.Users.Where(x => x.Id == userId).Select(x=> x.TotalNoofLeaves).FirstOrDefault(),
                ApprovedLeaves = _db.Leave.Count(l => l.Status == "APPROVED"),
                PendingLeaves = _db.Leave.Count(l => l.Status == "PENDING"),
                RejectedLeaves = _db.Leave.Count(l => l.Status == "REJECTED"),
                UserName = User.Identity?.Name ?? "User"
            };

            return View(viewModel);
        }



















        public IActionResult Privacy()
        {
            //// Check if user is authenticated
            //if (HttpContext.Session.GetString("UserId") == null)
            //{
            //    return RedirectToAction("Login", "Account");
            //}

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }
}
