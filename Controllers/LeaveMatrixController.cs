using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Leave_Management_System.Controllers
{
    //[SessionAuth]
    [Authorize]

    public class LeaveMatrixController : BaseController
    {

        private readonly ApplicationDbContext _db;

        public LeaveMatrixController(ApplicationDbContext db)
        {
            _db = db;
        }


        [HttpPost]
        public IActionResult List(string roleMasterId)
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "0");
                int skip = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");

                // Decrypt roleMasterId if provided
                int? decryptedRoleMasterId = null;
                if (!string.IsNullOrEmpty(roleMasterId))
                {
                    try
                    {
                        decryptedRoleMasterId = int.Parse(DecryptString(roleMasterId));
                    }
                    catch
                    {
                        return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = "Invalid role master ID format." });
                    }
                }

                var query = _db.LeaveMatrix
                                .Include(x => x.RoleMaster)
                                .Where(x => x.RoleMaster.Status == "ACTIVE")
                                .AsQueryable();

                // Filter by role master if ID is provided
                if (decryptedRoleMasterId.HasValue)
                {
                    query = query.Where(x => x.RoleMasterId == decryptedRoleMasterId.Value);
                }
                
                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    query = query.Where(x => x.RoleMaster.RoleName.ToLower().Contains(searchValue));
                }

                int totalRecords = query.Count();

                var roles = _db.RoleMaster
                    .Where(r => r.Status == "ACTIVE")
                    .ToList();

                var rawData = query
                    .AsEnumerable()
                    .Skip(skip)
                    .Take(pageSize)
                    .Select((x, index) => new
                    {
                        SerialNo = skip + index + 1,
                        Level = x.Level.ToString() != null ? x.Level.ToString() : "N/A",
                        Torole = x.RoleMaster?.RoleName ?? "N/A",
                        Status = x.Status == "ACTIVE" ? "<span class='badge bg-success'>Active</span>" : "<span class='badge bg-danger'>Inactive</span>",
                        Id = EncryptString(x.Id.ToString())
                    })
                    .ToList();

                var returnObj = Json(new
                {
                    draw = draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = rawData
                });

                return returnObj;
            }
            catch (Exception ex)
            {
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = ex.Message });
            }
        }


        public IActionResult Index(string id)
        {

            ViewBag.RoleMasterId = id;

            int decryptedId;
            try
            {
                decryptedId = int.Parse(DecryptString(id));
            }
            catch
            {
                return BadRequest("Invalid ID format.");
            }

            ViewBag.Name = _db.RoleMaster.Where(x => x.Id == decryptedId).Select(x => x.RoleName).FirstOrDefault();

            return View();
        }

        public IActionResult Create( string id)
        {
            try
            {
                int decryptedId;
                try
                {
                    decryptedId = int.Parse(DecryptString(id));
                }
                catch
                {
                    return BadRequest("Invalid ID format.");
                }

                ViewBag.Name = _db.RoleMaster.Where(x => x.Id == decryptedId).Select(x => x.RoleName).FirstOrDefault();

                ViewBag.RoleMasterId = decryptedId;

                var roles = _db.RoleMaster
                              .Where(x => x.Status == "ACTIVE")
                              .Select(x => new { x.Id, x.RoleName })
                              .ToList();

                ViewBag.Roles = new SelectList(roles, "Id", "RoleName");

                var existingLevels = _db.LeaveMatrix
                    .Where(s => s.RoleMasterId == decryptedId)
                    .Select(s => s.Level)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();

                int nextAvailableLevel = 1;
                while (existingLevels.Contains(nextAvailableLevel) && nextAvailableLevel <= 30)
                {
                    nextAvailableLevel++;
                }

                // 👇 send single value
                ViewBag.NextLevel = nextAvailableLevel;
                return View();
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveMatrix leavematrix, int RoleMasterId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            try
            {
               
                    // Check if level already exists
                    var existingLevel = await _db.LeaveMatrix
                        .FirstOrDefaultAsync(x => x.Level == leavematrix.Level && x.RoleMasterId == RoleMasterId);
                    
                    if (existingLevel != null)
                    {
                        return Json(new { success = false, message = "This level already exists. Please choose a different level." });
                    }

                    leavematrix.RoleMasterId = RoleMasterId;
                    leavematrix.CreatedBy = userId;
                    leavematrix.CreatedDatetime = DateTime.Now;
                    leavematrix.Status = "ACTIVE";

                    _db.Add(leavematrix);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "Leave Matrix Successfully Created.",id = EncryptString(RoleMasterId.ToString()) });
              
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred while creating the Leave Matrix: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                int decryptedId;
                try
                {
                    decryptedId = int.Parse(DecryptString(id));
                }
                catch
                {
                    return BadRequest("Invalid ID format.");
                }

                var LeaveMatrix = await _db.LeaveMatrix.FindAsync(decryptedId);
                if (LeaveMatrix == null)
                {
                    return NotFound();
                }

                ViewBag.Name = _db.RoleMaster.Where(x => x.Id == LeaveMatrix.RoleMasterId).Select(x => x.RoleName).FirstOrDefault();

                ViewBag.Level = LeaveMatrix.Level;

                var roles = _db.RoleMaster
                             .Where(x => x.Status == "ACTIVE")
                             .Select(x => new { x.Id, x.RoleName })
                             .ToList();

                ViewBag.Roles = new SelectList(roles, "Id", "RoleName", LeaveMatrix.ToRole);
                ViewBag.DecryptedId = decryptedId;
                return View(LeaveMatrix);
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeaveMatrix model)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            
            try
            {
               
                    var matrixedit = await _db.LeaveMatrix.FindAsync(model.Id);
                    if (matrixedit == null)
                    {
                        return Json(new { success = false, message = "Leave Matrix Not Found." });
                    }                                 

                    matrixedit.ToRole = model.ToRole;
                    matrixedit.Status = model.Status;
               
                    matrixedit.UpdatedBy = userId;
                    matrixedit.UpdatedDatetime = DateTime.Now;

                    _db.LeaveMatrix.Update(matrixedit);
                    await _db.SaveChangesAsync();
                    
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Leave Matrix updated successfully" , id = EncryptString(matrixedit.RoleMasterId.ToString()) });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Unable to save changes. Please try again. Error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                int decryptedId;
                try
                {
                    decryptedId = int.Parse(DecryptString(id));
                }
                catch
                {
                    return BadRequest("Invalid ID format.");
                }

                var matrixdelete = await _db.LeaveMatrix
                    .Include(rm => rm.RoleMaster)
                    .FirstOrDefaultAsync(m => m.Id == decryptedId);

                if (matrixdelete == null)
                {
                    return NotFound("Leave Matrix not found.");
                }

                ViewBag.Name = _db.RoleMaster.Where(x => x.Id == matrixdelete.RoleMasterId).Select(x => x.RoleName).FirstOrDefault();

                var roles = _db.RoleMaster
                      .Where(r => r.Status == "ACTIVE")
                      .ToDictionary(r => r.Id, r => r.RoleName);

                ViewData["Roles"] = roles;

                var createdByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == matrixdelete.CreatedBy);
                var updatedByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == matrixdelete.UpdatedBy);
                              
            
                ViewBag.CreatedBy = createdByUser?.Name ?? "Unknown";
                ViewBag.UpdatedBy = updatedByUser?.Name ?? "Unknown";

                return View(matrixdelete);
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Invalid request." });
                }

                int decryptedId;
                try
                {
                    decryptedId = int.Parse(DecryptString(id));
                }
                catch
                {
                    return Json(new { success = false, message = "Invalid ID format." });
                }

                var matrix = await _db.LeaveMatrix.FindAsync(decryptedId);
                if (matrix == null)
                {
                    return Json(new { success = false, message = "Leave Matrix Not Found" });
                }

                int CurrentLevel = matrix.Level;
                var listMatrix = _db.LeaveMatrix.Where(m => m.Level > CurrentLevel && m.RoleMasterId == matrix.RoleMasterId).ToList();

                if (listMatrix.Any())
                {
                    return Json(new { success = false, message = "Please Delete Higher Levels First." });
                }

                _db.LeaveMatrix.Remove(matrix);
                await _db.SaveChangesAsync();
                
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Leave Matrix Deleted Successfully" });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Deletion failed due to this Leave matrix is being referenced by other records." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "An error occurred while deleting the Leave matrix.", error = ex.Message });
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
