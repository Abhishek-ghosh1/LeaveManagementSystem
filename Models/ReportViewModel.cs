using System.ComponentModel.DataAnnotations;

namespace Leave_Management_System.Models
{
    public class ReportViewModel
    {
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? StartDate { get; set; }
        
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? EndDate { get; set; }

        public List<LeaveReportItem> LeaveReports { get; set; } = new List<LeaveReportItem>();
        
    }

    public class LeaveReportItem
    {
        public int Id { get; set; }
        public string ReferenceNo { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveReason { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime JoinDate { get; set; }
        public int NoofLeaves { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDatetime { get; set; }
    }
}