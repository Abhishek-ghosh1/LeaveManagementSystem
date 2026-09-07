using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Leave_Management_System.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_System.Controllers
{
    [SessionAuth]
    public class Team_ProjectController : BaseController
    {
        private readonly ApplicationDbContext _db;

        public Team_ProjectController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===================== INDEX =====================
        public IActionResult Index()
        {
            return View();
        }

        // ===================== LIST (DATATABLE) =====================
        [HttpPost]
        public IActionResult List()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "0");
            int skip = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");

            var query = _db.Team_Project
                .OrderByDescending(x => x.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToLower();

                query = query.Where(x =>
                    x.Name.ToLower().Contains(searchValue) ||
                    x.Status.ToLower().Contains(searchValue)
                );
            }

            int totalRecords = query.Count();

            var data = query
                .Skip(skip)
                .Take(pageSize)
                .AsEnumerable()
                .Select((x, index) => new
                {
                    serialNo = skip + index + 1,
                    id = EncryptString(x.Id.ToString()),
                    Name = x.Name,
                    status = x.Status,
                    createdDatetime = x.CreatedDatetime.ToString("dd-MMM-yyyy")
                })
                .ToList();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            });
        }

        // ===================== CREATE =====================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team_Project model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                model.CreatedBy = userId.Value;
                model.CreatedDatetime = DateTime.Now;
                model.Status = "ACTIVE";

                _db.Team_Project.Add(model);
                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Created successfully"
                });
            }
            catch (Exception ex)
            {
             
                var errorMessage = ex.InnerException?.Message ?? ex.Message;

                return Json(new { success = false, message = errorMessage });
            }
        }

        // ===================== EDIT =====================
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

            var data = await _db.Team_Project.FindAsync(decryptedId);

            if (data == null)
                return NotFound();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Team_Project model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
               
                var existing = await _db.Team_Project.FindAsync(model.Id);

                if (existing == null)
                    return Json(new { success = false, message = "Role not found" });

                existing.Name = model.Name;
                existing.Status = model.Status;

                existing.UpdatedBy = userId.Value;
                existing.UpdatedDatetime = DateTime.Now;
           
                _db.Team_Project.Update(existing);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync(); 

                return Json(new { success = true, message = "Updated successfully" });
                          
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

        // ===================== DETAILS =====================
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

            var data = await _db.Team_Project
                .FirstOrDefaultAsync(x => x.Id == decryptedId);

            if (data == null)
                return NotFound();

            var createdByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == data.CreatedBy);
            var updatedByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == data.UpdatedBy);

            ViewBag.CreatedBy = createdByUser?.Name ?? "Not Updated";
            ViewBag.UpdatedBy = updatedByUser?.Name ?? "Not Updated";

            return View(data);
        }

        // ===================== DELETE =====================
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

                var data = await _db.Team_Project.FindAsync(decryptedId);
                if (data == null)
                    return Json(new { success = false, message = "Data not found" });

                _db.Team_Project.Remove(data);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        // Encryption Method
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

        // Decryption Method
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