using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leave_Management_System.Models
{
    public class LeaveMatrix
    {
        public int Id { get; set; }

        public int RoleMasterId { get; set; }

        [ForeignKey("RoleMasterId")]
        public virtual RoleMaster RoleMasterParent { get; set; }

        [Required(ErrorMessage = "Level is required.")]
        public int Level { get; set; }

        [Required(ErrorMessage = "To role is required.")]
        public int ToRole { get; set; }

        [ForeignKey("ToRole")]
        public virtual RoleMaster RoleMaster { get; set; }  
  
        public int CreatedBy { get; set; }

        public DateTime CreatedDatetime { get; set; }

        public int UpdatedBy { get; set; }

        public DateTime UpdatedDatetime { get; set; }

        public string Status { get; set; } = "ACTIVE";

    }
 }

