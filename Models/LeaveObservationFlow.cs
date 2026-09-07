using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Leave_Management_System.Models
{
    public class LeaveObservationFlow
    {
        [Key]
        public int Id { get; set; }

        public int LeaveId { get; set; }
        [ForeignKey("LeaveId")]
        public virtual Leave Leave { get; set; }

        public int LeaveObservationDesiredFlowId { get; set; }
        [ForeignKey("LeaveObservationDesiredFlowId")]
        public LeaveObservationDesiredFlow LeaveObservationDesiredFlow { get; set; }

        public int? RoleId { get; set; }
        [ForeignKey("RoleId")]
        public RoleMaster RoleMaster { get; set; }
      
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual Users Users { get; set; }
   
        public DateTime? ActionTakenDatetime { get; set; }

        public string? Status { get; set; } = "N";

        public string? Status_to { get; set; }

        public string? ActionTaken { get; set; }

        public string? Document { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime CreatedDatetime { get; set; }

        public int Level { get; set; }

        public int? MannualEntry { get; set; }

    }
}


