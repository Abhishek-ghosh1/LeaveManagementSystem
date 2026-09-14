namespace Leave_Management_System.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int? TotalLeaves { get; set; }
        public int? TotalLeavesUsed { get; set; }
        public int? LeavesLeft { get; set; }
        public int ApprovedLeaves { get; set; }
        public int RejectedLeaves { get; set; }
        public int PendingLeavesForApproval { get; set; }
        public string UserName { get; set; }

        // Chart data properties

        public List<string> LeaveStatusLabels { get; set; } = new List<string>();
        public List<int> LeaveStatusData { get; set; } = new List<int>();

        public List<string> LeaveTypeLabels { get; set; } = new List<string>();
        public List<int> LeaveTypeData { get; set; } = new List<int>();

        public List<string> LeaveBalanceLabels { get; set; } = new List<string>();
        public List<int> LeaveBalanceData { get; set; } = new List<int>();

        // Calendar events data
        public List<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    }

    public class CalendarEvent
    {
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
    }
}
