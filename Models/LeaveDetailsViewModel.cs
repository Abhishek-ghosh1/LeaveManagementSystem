using Leave_Management_System.Models;
using System.Collections.Generic;

namespace Leave_Management_System.Models
{
    public class LeaveDetailsViewModel
    {
        public Leave Leave { get; set; }
        public int? CurrentUserId { get; set; }
        public string CurrentUserName { get; set; }
        public string CurrentUserRole { get; set; }
        public LeaveObservationFlow PendingFlow { get; set; }
        public string PendingApproverNames { get; set; }
        public bool IsPendingApprover { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string LeaveReasonName { get; set; }
        public List<LeaveObservationFlow> LeaveFlows { get; set; }
        public List<Users> Users { get; set; }
    }
}
