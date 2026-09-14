using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Leave_Management_System.Controllers
{
    //[SessionAuth]
    [Authorize]

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
                query = query.Where(x => x.Name.ToLower().Contains(searchName) ||
                                        (x.ContactNo != null && x.ContactNo.ToString().Contains(searchName)) ||
                                        (x.Email != null && x.Email.ToLower().Contains(searchName)));

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
                    ContactNo = x.ContactNo,
                    Email = x.Email,
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
        public async Task<IActionResult> Create(Users model, IFormFile ProfilePic)
        {
           
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (model.Team_ProjectIds != null && model.Team_ProjectIds.Any())
                {
                    model.Team_ProjectId = string.Join(",", model.Team_ProjectIds);
                }
                model.TotalNoofLeaves = model.TotalNoofLeaves;
                model.ContactNo = model.ContactNo;
                model.Email = model.Email;
                model.Address = model.Address;
                model.CreatedBy = Convert.ToInt32(userId);
                model.CreatedDatetime = DateTime.Now;
                model.Status = model.Status ?? "ACTIVE";
                model.Password = _passwordHasher.HashPassword(model, model.Password);

                // Handle image upload
                if (ProfilePic != null && ProfilePic.Length > 0)
                {
                    string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    string ext = Path.GetExtension(ProfilePic.FileName).ToLower();
                    
                    if (!allowedImageExtensions.Contains(ext))
                    {
                        return Json(new { success = false, message = $"Invalid file type '{ext}'. Only images (.jpg, .jpeg, .png, .gif) are allowed." });
                    }

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Users");
                    EnsureDirectoryExists(uploadFolder);

                    var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_UserImage{ext}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfilePic.CopyToAsync(stream);
                    }

                    model.Image = fileName;
                }

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
        public async Task<IActionResult> Edit(int Id, Users model, IFormFile ProfilePicFile)
        {
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
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
                user.TotalNoofLeaves = model.TotalNoofLeaves;
                user.Name = model.Name;
                user.PinNo = model.PinNo;
                user.ContactNo = model.ContactNo;
                user.Email = model.Email;
                user.Address = model.Address;
                user.Status = model.Status;
               
                user.RoleId = model.RoleId;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.Password = _passwordHasher.HashPassword(model,model.Password);

                }

                // Handle image upload
                if (ProfilePicFile != null && ProfilePicFile.Length > 0)
                {
                    string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    string ext = Path.GetExtension(ProfilePicFile.FileName).ToLower();
                    
                    if (!allowedImageExtensions.Contains(ext))
                    {
                        return Json(new { success = false, message = $"Invalid file type '{ext}'. Only images (.jpg, .jpeg, .png, .gif) are allowed." });
                    }

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Users");
                    EnsureDirectoryExists(uploadFolder);

                    var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_UserImage{ext}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfilePicFile.CopyToAsync(stream);
                    }

                    user.Image = fileName;
                }

                user.UpdatedBy = Convert.ToInt32(userId);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
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

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private string GenerateRandomNumber()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }
    }
}