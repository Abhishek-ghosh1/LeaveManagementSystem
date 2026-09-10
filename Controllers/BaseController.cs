using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Leave_Management_System.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Set ViewBag.UserName for all views
            //ViewBag.UserName = HttpContext.Session.GetString("UserName");
            //ViewBag.UserId = HttpContext.Session.GetInt32("UserId");

            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            base.OnActionExecuting(context);
        }
    }
}
