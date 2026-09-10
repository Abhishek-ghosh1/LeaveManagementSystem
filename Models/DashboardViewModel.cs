namespace Leave_Management_System.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int? TotalLeaves { get; set; }
        public int ApprovedLeaves { get; set; }
        public int PendingLeaves { get; set; }
        public int RejectedLeaves { get; set; }
        public string UserName { get; set; }
    }
}
