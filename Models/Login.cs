using System.ComponentModel.DataAnnotations;

namespace Leave_Management_System.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Pin No is required")]
        [Display(Name = "Pin No")]
        public string PinNo { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
