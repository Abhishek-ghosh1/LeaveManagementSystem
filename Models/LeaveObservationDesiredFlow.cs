using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Leave_Management_System.Models
{
    public class LeaveObservationDesiredFlow
    {
        public int Id { get; set; }

        public int LeaveId { get; set; }
        [ForeignKey("LeaveId")]
        public virtual Leave Leave { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual Users Users { get; set; }

        public int? RoleId { get; set; }
        [ForeignKey("RoleId")]
        public RoleMaster RoleMaster { get; set; }   
        public string Status { get; set; }      
        public int Level { get; set; }


    }
}




