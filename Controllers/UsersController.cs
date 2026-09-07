using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Leave_Management_System.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

namespace Leave_Management_System.Controllers
{
    [SessionAuth]
    public class UsersController : BaseController
    {
        private readonly ApplicationDbContext _db;
        
        private readonly PasswordHasher<Users> _passwordHasher;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
            _passwordHasher = new PasswordHasher<Users>();
        }

        public IActionResult Index()
        {
            var users = _db.Users.OrderByDescending(x => x.Id).ToList();
            return View(users);
        }

        [HttpPost]
        public IActionResult List()
        {
            var draw = Request.Form["draw"].FirstOrDefault();

            var searchName = Request.Form["name"].FirstOrDefault()?.ToLower();
            var searchUserId = Request.Form["userId"].FirstOrDefault()?.ToLower();
            var status = Request.Form["status"].FirstOrDefault();

            int pageSize = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            int skip = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");

            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(x => x.Name.ToLower().Contains(searchName));

            if (!string.IsNullOrEmpty(searchUserId))
                query = query.Where(x => x.PinNo.ToLower().Contains(searchUserId));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status == status);

            int totalRecords = query.Count();

            var data = query
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(pageSize)
                .AsEnumerable()
                .Select((x, i) => new
                {
                    SerialNo = skip + i + 1,
                    Name = x.Name,
                    PinNo = x.PinNo,
                    Status = x.Status == "ACTIVE"
                        ? "<span class='badge bg-success'>Active</span>"
                        : "<span class='badge bg-danger'>Inactive</span>",
                    CreatedDate = x.CreatedDatetime.ToString("dd-MMM-yyyy"),
                    Id = EncryptString(x.Id.ToString())
                }).ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data
            });
        }

        public IActionResult Create()
        {
            var model = new Users();

            model.Roles = _db.RoleMaster.
                Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.RoleName
                }).ToList();

            model.Team_Projects = _db.Team_Project
       .Select(x => new SelectListItem
       {
           Value = x.Id.ToString(),
           Text = x.Name
       }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Users model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (model.Team_ProjectIds != null && model.Team_ProjectIds.Any())
                {
                    model.Team_ProjectId = string.Join(",", model.Team_ProjectIds);
                }

                model.CreatedBy = userId.Value;
                model.CreatedDatetime = DateTime.Now;
                model.Status = model.Status ?? "ACTIVE";
                model.Password = _passwordHasher.HashPassword(model, model.Password);

                _db.Users.Add(model);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = "User created successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var errorMessage = ex.InnerException?.Message ?? ex.Message;

                return Json(new { success = false, message = errorMessage });
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            int decryptedId;

            try
            {
                decryptedId = int.Parse(DecryptString(id));
            }
            catch
            {
                return BadRequest("Invalid ID");
            }

            var user = await _db.Users.FindAsync(decryptedId);
            if (user == null)
                return NotFound();

            user.Roles = _db.RoleMaster.
                Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.RoleName
                }).ToList();

          
         
            user.Team_Projects = _db.Team_Project
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                }).ToList();

         
            if (!string.IsNullOrEmpty(user.Team_ProjectId))
            {
                user.Team_ProjectIds = user.Team_ProjectId
                    .Split(',')
                    .Select(int.Parse)
                    .ToList();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, Users model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var user = await _db.Users.FindAsync(Id);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });
                if (model.Team_ProjectIds != null && model.Team_ProjectIds.Any())
                {
                    user.Team_ProjectId = string.Join(",", model.Team_ProjectIds);
                }
                else
                {
                    user.Team_ProjectId = null;
                }

                user.Name = model.Name;
                user.PinNo = model.PinNo;
                user.Status = model.Status;
               
                user.RoleId = model.RoleId;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.Password = _passwordHasher.HashPassword(model,model.Password);

                }

                user.UpdatedBy = userId.Value;
                user.UpdatedDatetime = DateTime.Now;

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return Json(new
                {
                    success = false,
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message
                });
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            int decryptedId;

            try
            {
                decryptedId = int.Parse(DecryptString(id));
            }
            catch
            {
                return BadRequest("Invalid ID");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == decryptedId);

            if (user == null)
                return NotFound();

            var createdByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == user.CreatedBy);

            var updatedByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == user.UpdatedBy);

            var roleName = await _db.RoleMaster.FirstOrDefaultAsync(m => m.Id == user.RoleId);

         
            ViewBag.CreatedBy = createdByUser?.Name ?? "Not Updated";

            ViewBag.UpdatedBy = updatedByUser?.Name ?? "Not Updated";

            ViewBag.RoleName = roleName?.RoleName ?? "Role Not Found";

            List<string> TeamProjectNames = new List<string>();

            if (!string.IsNullOrEmpty(user.Team_ProjectId))
            {
                var ids = user.Team_ProjectId.Split(',').Select(int.Parse).ToList();

                TeamProjectNames = _db.Team_Project.Where(x => ids.Contains(x.Id)).Select(x => x.Name).ToList();
            }

            ViewBag.Team_ProjectName = TeamProjectNames;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (string.IsNullOrEmpty(id))
                    return Json(new { success = false, message = "Invalid request." });

                int decryptedId;

                try
                {
                    decryptedId = int.Parse(DecryptString(id));
                }
                catch
                {
                    return Json(new { success = false, message = "Invalid ID." });
                }

                var user = await _db.Users.FindAsync(decryptedId);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                _db.Users.Remove(user);

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "User deleted permanently." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            try
            {
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(plainBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error encrypting the string.", ex);
            }
        }

        private static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                return System.Text.Encoding.UTF8.GetString(cipherBytes);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Error decrypting the string. Invalid format.", ex);
            }
        }
    }
}