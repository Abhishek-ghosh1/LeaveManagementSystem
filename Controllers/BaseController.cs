using Leave_Management_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text;

namespace Leave_Management_System.Controllers
{
    public class BaseController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BaseController(ApplicationDbContext db)
        {
            _db = db;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {         

            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.UserRole = User.FindFirstValue(ClaimTypes.Role);

            // Set User Profile Picture
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var user = _db.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user != null && !string.IsNullOrEmpty(user.Image))
                {
                    ViewBag.UserProfilePic = user.Image;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
