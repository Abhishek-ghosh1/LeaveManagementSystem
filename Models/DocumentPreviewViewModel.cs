using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Leave_Management_System.Models
{
    public class DocumentPreviewViewModel
    {
        public string FileName { get; set; }
        public string Path { get; set; }
    }
}
