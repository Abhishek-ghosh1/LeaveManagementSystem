using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var viewModel = new DashboardViewModel
            {
                TotalUsers = _db.Users.Count(),

                TotalLeaves = _db.Users.Where(x => x.Id == userId).Select(x=> x.TotalNoofLeaves).FirstOrDefault(),

                TotalLeavesUsed = _db.Users.Where(x => x.Id == userId).Select(x => x.UsedLeaves).FirstOrDefault(),

                LeavesLeft = _db.Users.Where(x => x.Id == userId).Select(x => x.LeftLeaves).FirstOrDefault(),

                ApprovedLeaves = _db.Leave.Where(l => l.EmpUserID == userId).Count( l => l.Status == "Approved"),

                RejectedLeaves = _db.Leave.Where(l => l.EmpUserID == userId).Count(l => l.Status == "Rejected"),

                PendingLeavesForApproval = _db.Leave.Where(l => l.EmpUserID == userId).Count(l => l.Status == "ACTIVE"),

                UserName = User.Identity?.Name ?? "User"
            };

            // Populate chart data
            // Leave Status Distribution

            viewModel.LeaveStatusLabels = new List<string> { "Approved", "Rejected", "Pending" };

            viewModel.LeaveStatusData = new List<int>
            { 
                viewModel.ApprovedLeaves,
                viewModel.RejectedLeaves,
                viewModel.PendingLeavesForApproval 
            };

            // Leave Types Distribution
            var leaveTypes = _db.Leave
                .Where(l => l.EmpUserID == userId)
                .GroupBy(l => l.LeaveMaster.LeaveName)
                .Select(g => new { LeaveName = g.Key, Count = g.Count() })
                .ToList();

            viewModel.LeaveTypeLabels = leaveTypes.Select(l => l.LeaveName).ToList();
            viewModel.LeaveTypeData = leaveTypes.Select(l => l.Count).ToList();

            // Leave Balance
            viewModel.LeaveBalanceLabels = new List<string> { "Total Leaves", "Used Leaves", "Remaining Leaves" };

            viewModel.LeaveBalanceData = new List<int>
            {
                viewModel.TotalLeaves ?? 0,
                viewModel.TotalLeavesUsed ?? 0,
                viewModel.LeavesLeft ?? 0
            };

            // Populate calendar events with leave data
            var userLeaves = _db.Leave.Include(l => l.LeaveMaster)
                .Where(l => l.EmpUserID == userId && (l.Status == "Approved" || l.Status == "ACTIVE"))
                .ToList();

            foreach (var leave in userLeaves)
            {
                string color = leave.Status == "Approved" ? "#28a745" : "#ffc107";
                string title = $"{leave.LeaveMaster.LeaveName} - {leave.Status}";

                viewModel.CalendarEvents.Add(new CalendarEvent
                {
                    Title = title,
                    Start = leave.StartDate.ToString("yyyy-MM-dd"),
                    End = leave.StartDate.AddDays(leave.NoofLeaves - 1).ToString("yyyy-MM-dd"),
                    BackgroundColor = color,
                    BorderColor = color
                });
            }

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
