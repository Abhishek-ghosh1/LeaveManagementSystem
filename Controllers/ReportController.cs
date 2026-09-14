using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Leave_Management_System.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly ApplicationDbContext _db;

        public ReportController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var viewModel = new ReportViewModel();

            // Calculate date range - default to current month
            DateTime today = DateTime.Today;
            DateTime calculatedStartDate = new DateTime(today.Year, today.Month, 1);
            DateTime calculatedEndDate = calculatedStartDate.AddMonths(1).AddDays(-1);

            // Override with custom dates if provided
            if (startDate.HasValue)
            {
                calculatedStartDate = startDate.Value;
            }
            if (endDate.HasValue)
            {
                calculatedEndDate = endDate.Value;
            }

            viewModel.StartDate = calculatedStartDate;
            viewModel.EndDate = calculatedEndDate;

            // Get leave data for the specified date range
            var leaveQuery = _db.Leave
                .Include(l => l.Employee)
                .Include(l => l.LeaveMaster)
                .Where(l => l.StartDate >= calculatedStartDate && l.StartDate <= calculatedEndDate);

            // If user is not admin, show only their own leaves
            var currentUser = _db.Users.Include(u => u.RoleMaster).FirstOrDefault(u => u.Id == userId);
            bool isAdmin = currentUser?.RoleMaster?.RoleName?.ToLower() == "admin";

            if (!isAdmin)
            {
                leaveQuery = leaveQuery.Where(l => l.EmpUserID == userId);
            }

            var leaves = leaveQuery.OrderByDescending(l => l.StartDate).ToList();

            // Map to report items
            viewModel.LeaveReports = leaves.Select(l => new LeaveReportItem
            {
                Id = l.Id,
                ReferenceNo = l.ReferenceNo,
                EmployeeName = l.Employee?.Name ?? "Unknown",
                LeaveReason = l.LeaveMaster?.LeaveName ?? "Unknown",
                StartDate = l.StartDate,
                JoinDate = l.JoinDate,
                NoofLeaves = l.NoofLeaves,
                Status = l.Status,
                CreatedBy = _db.Users.FirstOrDefault(u => u.Id == l.CreatedBy)?.Name ?? "Unknown",
                CreatedDatetime = l.CreatedDatetime
            }).ToList();
           
            return View(viewModel);
        }
    }
}