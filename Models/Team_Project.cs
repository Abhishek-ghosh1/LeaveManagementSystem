using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Leave_Management_System.Models
{
    public class Team_Project
    {
        public int Id { get; set; }
     
        public string Name { get; set; }
    
        public int CreatedBy { get; set; }

        public DateTime CreatedDatetime { get; set; }

        public int UpdatedBy { get; set; }

        public DateTime UpdatedDatetime { get; set; }

        public string Status { get; set; } = "ACTIVE";

    }
}


