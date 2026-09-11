using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Leave_Management_System.Models
{

    public class Leave
    {

        public int Id { get; set; }

        public string? ReferenceNo { get; set; }
        public string? IsDraft { get; set; }
        public int? Sl { get; set; }

        public int EmpUserID { get; set; }

        [ForeignKey("EmpUserID")]
        public Users Employee { get; set; }

        public int LeaveReasonId { get; set; }

        [ForeignKey("LeaveReasonId")]
        public LeaveMaster LeaveMaster { get; set; }

        public string LeaveDescription { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime JoinDate { get; set; } = DateTime.Now;
        public int NoofLeaves { get; set; }
        public string? UploadPDFFile { get; set; }

        public string? OtherRelatedDocs { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDatetime { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDatetime { get; set; }

        public string Status { get; set; } = "ACTIVE";


        [NotMapped]
        public IEnumerable<SelectListItem> Employees { get; set; } = new List<SelectListItem>();

        [NotMapped]
        public IEnumerable<SelectListItem> LeaveReasons { get; set; } = new List<SelectListItem>();
    }
}
