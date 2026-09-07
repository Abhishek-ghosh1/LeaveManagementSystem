using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Leave_Management_System.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int RoleId { get; set; }
        public string PinNo { get; set; }
        public string Password { get; set; }
      
        [Required]
        public string Status { get; set; } = "ACTIVE";

        public string? Team_ProjectId { get; set; }

        public int? TotalNoofLeaves { get; set; }

        public int? UsedLeaves { get; set; }

        public int? LeftLeaves { get; set; }

        public int? LeaveSpendingForApproval { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedDatetime { get; set; }

        [AllowNull]
        public int? UpdatedBy { get; set; }

        [AllowNull]
        public DateTime? UpdatedDatetime { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem> Roles { get; set; } = new List<SelectListItem>();

        [NotMapped]
        public IEnumerable<SelectListItem> Team_Projects { get; set; } = new List<SelectListItem>();

        [NotMapped]
        public List<int> Team_ProjectIds { get; set; } = new List<int>();
    }
}
