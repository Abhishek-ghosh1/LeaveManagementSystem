using Leave_Management_System.Data;
using Leave_Management_System.Models;
using Leave_Management_System.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_System.Controllers
{
    [SessionAuth]
    public class LeaveController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public LeaveController(ApplicationDbContext db)
        {
            _db = db;
        }


        [HttpPost]
        [ActionName("List")]
        public IActionResult List()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "0");
            int skip = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");

            var query = _db.Leave
                .Include(x => x.Employee).Include(x => x.LeaveMaster)
                .OrderByDescending(x => x.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.Trim().ToLower();
                query = query.Where(x =>
                    (x.ReferenceNo != null && x.ReferenceNo.ToLower().Contains(searchValue)) ||
                   
                    (x.LeaveDescription != null && x.LeaveDescription.ToLower().Contains(searchValue)) ||
                   
                    (x.Employee != null && x.Employee.Name != null && x.Employee.Name.ToLower().Contains(searchValue)) ||

                    (x.LeaveMaster != null && x.LeaveMaster.LeaveName != null && x.LeaveMaster.LeaveName.ToLower().Contains(searchValue))

                );
            }


            int totalRecords = query.Count();

            // First get the data without the nested query
            var rawDataQuery = query
                .AsEnumerable()
                .Skip(skip)
                .Take(pageSize)
                .Select((x, index) => new
                {
                    SerialNo = skip + index + 1,
                    employeeName = x.Employee?.Name ?? "Unknown",
                    ReferenceNo = x.ReferenceNo,
                    leaveReason = x.LeaveMaster?.LeaveName ?? "Unknown",
                    startDate = x.StartDate.ToString("dd-MMM-yyyy"),
                    joinDate = x.JoinDate.ToString("dd-MMM-yyyy"),
                    Status = x.Status,
                    Draft = x.IsDraft,
                    Files = x.UploadPDFFile,
                    CreatedById = x.CreatedBy,
                    UpdatedById = x.UpdatedBy,
                    LeaveId = x.Id,
                    CreatedDatetime = x.CreatedDatetime,
                    UpdatedDatetime = x.UpdatedDatetime,
                    CreatedDateTime = x.CreatedDatetime.ToString("MM/dd/yyyy hh:mm tt"),
                    Id = EncryptString(x.Id.ToString()),
                })
                .ToList();

            // Then fetch the user names separately
            var userIds = rawDataQuery.Select(x => x.CreatedById).Distinct().ToList();
            var updatedUserIds = rawDataQuery.Where(x => x.UpdatedById.HasValue).Select(x => x.UpdatedById.Value).Distinct().ToList();
            userIds.AddRange(updatedUserIds);
            
            var userNames = _db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name);

            // Final projection with user names and pending users
            var rawData = rawDataQuery.Select(x => new
            {
                SerialNo = x.SerialNo,
                employeeName = x.employeeName,
                ReferenceNo = x.ReferenceNo,
                leaveReason = x.leaveReason,
                startDate = x.startDate,
                joinDate = x.joinDate,
                Status = x.Status,
                Files = x.Files,
                Draft = x.Draft,
                CreatedBy = userNames.TryGetValue(x.CreatedById, out var name) ? name : "Unknown",
                UpdatedBy = x.UpdatedById.HasValue ? userNames.TryGetValue(x.UpdatedById.Value, out var updatedName) ? updatedName : "Unknown" : "Not Updated",
                CreatedDateTime = x.CreatedDateTime,
                UpdatedDateTime = x.UpdatedDatetime.HasValue ? x.UpdatedDatetime.Value.ToString("MM/dd/yyyy hh:mm tt") : "Not Updated",
                Id = x.Id,
                // Simple pending with - get users for this leave
                pendingWith = string.Join(" ", _db.LeaveObservationFlow
    .Where(lo => lo.LeaveId == x.LeaveId && lo.Status == "Pending")
    .Select(lo => new
    {
        UserName = _db.Users
            .Where(u => u.Id == lo.UserId)
            .Select(u => u.Name)
            .FirstOrDefault() ?? "Unknown",

        RoleName = _db.RoleMaster
            .Where(r => r.Id == lo.RoleId)
            .Select(r => r.RoleName)
            .FirstOrDefault() ?? "No Role"
    })
    .Select(x => $"<span class='badge bg-danger'>{x.UserName} ({x.RoleName})</span>")
)
            }).ToList();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = rawData
            });
        }

        public IActionResult Index()
        {
           
            return View();
        }

        public IActionResult Create()
        {
            var model = new Leave();

            model.Employees = _db.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .ToList();

            model.LeaveReasons = _db.LeaveMaster
               .Select(u => new SelectListItem
               {
                   Value = u.Id.ToString(),
                   Text = u.LeaveName
               })
               .ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Leave model, List<IFormFile> PDFFile, List<IFormFile> OtherDocs, string submitType = "submit")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Set draft status based on submit type
                model.IsDraft = submitType == "draft" ? "Yes" : "No";
              
                string[] allowedOtherDocsExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".txt" };
                string[] allowedPdfExtensions = { ".pdf" };

                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Leave");
                EnsureDirectoryExists(uploadFolder);

                // ---- Handle PDF Files ----
                if (PDFFile != null && PDFFile.Any())
                {
                    var fileNames = new List<string>();
                    foreach (var file in PDFFile)
                    {
                        if (file?.Length > 0)
                        {
                            string ext = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedPdfExtensions.Contains(ext))
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Invalid file type '{ext}' for PDFFile. Only PDF is allowed."
                                });
                            }

                            var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_LeavePdfFile{ext}";
                            var filePath = Path.Combine(uploadFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            fileNames.Add(fileName);
                        }
                    }
                    model.UploadPDFFile = string.Join(",", fileNames);
                }

                if (OtherDocs != null && OtherDocs.Any())
                {
                    var fileNames = new List<string>();
                    foreach (var file in OtherDocs)
                    {
                        if (file?.Length > 0)
                        {
                            string ext = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedOtherDocsExtensions.Contains(ext))
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Invalid file type '{ext}' for Other Documents. Allowed types: {string.Join(", ", allowedOtherDocsExtensions)}"
                                });
                            }

                            var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_LeaveOtherDocsFile{ext}";
                            var filePath = Path.Combine(uploadFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            fileNames.Add(fileName);
                        }
                    }
                    model.OtherRelatedDocs = string.Join(",", fileNames);
                }
                // Common properties
                model.CreatedBy = model.EmpUserID;
                model.CreatedDatetime = DateTime.Now;

                _db.Leave.Add(model);
                await _db.SaveChangesAsync();

                if (model.IsDraft == "No")
                {

                    //Insert Into Desired Flow

                    var (IsDesiredFlowInserted, errorMessage) = await InsertDesiredFlow(model.Id, userId.Value);

                    if (!IsDesiredFlowInserted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = errorMessage });
                    }

                    //Insert First Level in LeaveObservationFlow
                    var (IsFirstLevelInserted, firstLevelErrorMessage) = await InsertFirstLevelToObservationFlow(model.Id, userId.Value);

                    if (!IsFirstLevelInserted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = firstLevelErrorMessage });
                    }

                }            

                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = submitType == "draft" ? "Leave Draft Saved Successfully." : "Leave Record Submitted Successfully."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new
                {
                    success = false,
                    message = "Error! An error occurred. Please try again later.",
                    detail = ex.Message
                });
            }
        }

        public async Task<IActionResult> Edit(string id)
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

            var Leave = await _db.Leave.FindAsync(decryptedId);
            if (Leave == null)
            {
                return NotFound();
            }

            ViewBag.DecryptedId = decryptedId;

            // ✅ Bind dropdown to SAME model
            Leave.Employees = _db.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .ToList();

            Leave.LeaveReasons = _db.LeaveMaster
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.LeaveName
            })
            .ToList();

            return View(Leave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, Leave model, List<IFormFile> PDFFile, List<IFormFile> OtherDocs, string submitType = "submit")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var Leave = await _db.Leave.FindAsync(Id);
                if (Leave == null)
                    return Json(new { success = false, message = "Data not found." });

                // Set draft status based on submit type
                Leave.IsDraft = submitType == "draft" ? "Yes" : "No";
                Leave.Status = submitType == "draft" ? "DRAFT" : "ACTIVE";

                string[] allowedOtherDocsExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".txt" };
                string[] allowedPdfExtensions = { ".pdf" };

                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Leave");
                EnsureDirectoryExists(uploadFolder);

                if (PDFFile != null && PDFFile.Any())
                {
                    var fileNames = new List<string>();
                    foreach (var file in PDFFile)
                    {
                        if (file?.Length > 0)
                        {
                            string ext = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedPdfExtensions.Contains(ext))
                                return Json(new { success = false, message = $"Invalid file type '{ext}' for PDFFile." });

                            var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_LeavePdfFile{ext}";
                            var filePath = Path.Combine(uploadFolder, fileName);
                            using var stream = new FileStream(filePath, FileMode.Create);
                            await file.CopyToAsync(stream);
                            fileNames.Add(fileName);
                        }
                    }
                    Leave.UploadPDFFile = string.Join(",", fileNames);
                }

                if (OtherDocs != null && OtherDocs.Any())
                {
                    var fileNames = new List<string>();
                    foreach (var file in OtherDocs)
                    {
                        if (file?.Length > 0)
                        {
                            string ext = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedOtherDocsExtensions.Contains(ext))
                                return Json(new { success = false, message = $"Invalid file type '{ext}' for OriginalFile." });

                            var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_EdocCertificateOtherDocsFile{ext}";
                            var filePath = Path.Combine(uploadFolder, fileName);
                            using var stream = new FileStream(filePath, FileMode.Create);
                            await file.CopyToAsync(stream);
                            fileNames.Add(fileName);
                        }
                    }
                    Leave.OtherRelatedDocs = string.Join(",", fileNames);
                }

                Leave.LeaveReasonId = model.LeaveReasonId;
                Leave.LeaveDescription = model.LeaveDescription;
                Leave.StartDate = model.StartDate;
                Leave.JoinDate = model.JoinDate;            
                Leave.UpdatedBy = model.EmpUserID;
                Leave.UpdatedDatetime = DateTime.Now;

                _db.Leave.Update(Leave);

                await _db.SaveChangesAsync();
             
                if (model.IsDraft == "No")
                {  
                    //Insert Into Desired Flow

                    var (IsDesiredFlowInserted, errorMessage) = await InsertDesiredFlow(Leave.Id, userId.Value);

                    if (!IsDesiredFlowInserted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = errorMessage });
                    }

                    //Insert First Level in LeaveObservationFlow
                    var (IsFirstLevelInserted, firstLevelErrorMessage) = await InsertFirstLevelToObservationFlow(Leave.Id, userId.Value);

                    if (!IsFirstLevelInserted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = firstLevelErrorMessage });
                    }
                }
               

                await transaction.CommitAsync();

                return Json(new { success = true, message = submitType == "draft" ? "Leave Draft Updated Successfully." : "Leave Data Updated." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error! Please try again later.", detail = ex.Message });
            }
        }

        private async Task<(bool Success, string ErrorMessage)> InsertDesiredFlow(int LeaveId, int UserId)
        {
            try
            {
                if (LeaveId <= 0 || UserId <= 0)
                {
                    return (false, "Invalid LeaveId or UserId.");
                }

                var userdata = await _db.Users
                    .FirstOrDefaultAsync(x => x.Id == UserId);

                if (userdata == null)
                {
                    return (false, "User not found.");
                }

                var role = _db.RoleMaster.Where(x => x.Id == userdata.RoleId).FirstOrDefault();

                if (role == null)
                {
                    return (false, "Role not found for user.");
                }
                // Get matrix flow data
                var matrixdata = await _db.LeaveMatrix
                    .Where(x => x.RoleMasterId == role.Id)
                    .OrderBy(x => x.Level)
                    .ToListAsync();

                if (!matrixdata.Any())
                {
                    return (false, "No leave matrix configuration found for this role.");
                }

                foreach (var item in matrixdata)
                {
                    var users = await _db.Users
                        .Where(x => x.RoleId == item.ToRole
                                 && x.Team_ProjectId == userdata.Team_ProjectId)
                        .ToListAsync();

                    if (!users.Any())
                    {
                        var roleName = await _db.RoleMaster
                            .Where(x => x.Id == item.ToRole)
                            .Select(x => x.RoleName)
                            .FirstOrDefaultAsync();

                        return (false, $"No approvers found for Level {item.Level}. Please ensure users with Role '{roleName ?? item.ToRole.ToString()}' exist in your team/project.");
                    }

                    foreach (var user in users)
                    {
                        LeaveObservationDesiredFlow desiredflow =
                            new LeaveObservationDesiredFlow
                            {
                                LeaveId = LeaveId,

                                // Approver User
                                UserId = user.Id,

                                // Approver Role
                                RoleId = user.RoleId,

                                // Matrix Level
                                Level = item.Level,

                                Status = "Pending"
                            };

                        await _db.LeaveObservationDesiredFlow.AddAsync(desiredflow);
                    }
                }

                await _db.SaveChangesAsync();

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"Error inserting desired flow: {ex.Message}");
            }
        }

        //Insert First Level in [LeaveObservationFlow] and call it in Create post and edit post 
        private async Task<(bool Success, string ErrorMessage)> InsertFirstLevelToObservationFlow(int LeaveId, int UserId)
        {
            try
            {
                if (LeaveId <= 0 || UserId <= 0)
                {
                    return (false, "Invalid LeaveId or UserId.");
                }

                // Get the first level from LeaveObservationDesiredFlow
                var firstLevelDesiredFlow = await _db.LeaveObservationDesiredFlow
                    .Where(x => x.LeaveId == LeaveId && x.Level == 1)
                    .FirstOrDefaultAsync();

                if (firstLevelDesiredFlow == null)
                {
                    return (false, "No first level found in desired flow.");
                }

                // Insert into LeaveObservationFlow
                LeaveObservationFlow observationFlow = new LeaveObservationFlow
                {
                    LeaveId = LeaveId,
                    LeaveObservationDesiredFlowId = firstLevelDesiredFlow.Id,
                    RoleId = firstLevelDesiredFlow.RoleId,
                    UserId = firstLevelDesiredFlow.UserId,
                    Level = firstLevelDesiredFlow.Level,
                    Status = "Pending",
                    Status_to = "N",
                    ActionTakenDatetime = DateTime.Now,
                    CreatedDatetime = DateTime.Now,
                    MannualEntry = 0
                };

                await _db.LeaveObservationFlow.AddAsync(observationFlow);
                await _db.SaveChangesAsync();

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"Error inserting first level to observation flow: {ex.Message}");
            }
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int LeaveId, int ObservationFlowId, int CurrentUserId, string Status_to, string ActionTaken, IFormFile Document)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var observationFlow = await _db.LeaveObservationFlow
                    .Include(x => x.Leave)
                    .FirstOrDefaultAsync(x => x.Id == ObservationFlowId && x.LeaveId == LeaveId);

                if (observationFlow == null)
                    return Json(new { success = false, message = "Observation flow not found." });

                if (observationFlow.UserId != userId)
                    return Json(new { success = false, message = "You are not authorized to approve this leave." });

                // Handle document upload
                string documentPath = null;
                if (Document != null && Document.Length > 0)
                {
                    string[] allowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                    string ext = Path.GetExtension(Document.FileName).ToLower();
                    
                    if (!allowedExtensions.Contains(ext))
                        return Json(new { success = false, message = $"Invalid file type '{ext}'." });

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Leave");
                    EnsureDirectoryExists(uploadFolder);

                    var fileName = $"{GenerateRandomNumber()}{DateTime.Now:yyyyMMddHHmmssfff}_LeaveApproval{ext}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Document.CopyToAsync(stream);
                    }

                    documentPath = fileName;
                }

                // Update observation flow
                observationFlow.Status = Status_to == "Approved" ? "Approved" : "Rejected";
                observationFlow.Status_to = Status_to;
                observationFlow.ActionTaken = ActionTaken;
                observationFlow.ActionTakenDatetime = DateTime.Now;
                observationFlow.Document = documentPath;
                observationFlow.UpdatedBy = userId;

                _db.LeaveObservationFlow.Update(observationFlow);
                await _db.SaveChangesAsync();

                // If approved, move to next level
                if (Status_to == "Approved")
                {
                    var nextLevelFlow = await _db.LeaveObservationDesiredFlow
                        .Where(x => x.LeaveId == LeaveId && x.Level == observationFlow.Level + 1)
                        .FirstOrDefaultAsync();

                    if (nextLevelFlow != null)
                    {
                        // Insert next level into LeaveObservationFlow
                        LeaveObservationFlow nextObservationFlow = new LeaveObservationFlow
                        {
                            LeaveId = LeaveId,
                            LeaveObservationDesiredFlowId = nextLevelFlow.Id,
                            RoleId = nextLevelFlow.RoleId,
                            UserId = nextLevelFlow.UserId,
                            Level = nextLevelFlow.Level,
                            Status = "Pending",
                            Status_to = "N",
                            CreatedDatetime = DateTime.Now,
                            MannualEntry = 1
                        };

                        await _db.LeaveObservationFlow.AddAsync(nextObservationFlow);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {

                        // ✅ Financial Year Logic for Final Save
                        DateTime now = DateTime.Now;

                        DateTime financialYearStart = now.Month >= 4
                            ? new DateTime(now.Year, 4, 1)
                            : new DateTime(now.Year - 1, 4, 1);

                        DateTime financialYearEnd = financialYearStart.AddYears(1).AddDays(-1);

                        string prefix = "LEAVE";

                        // ✅ Generate Reference & SL
                        string referenceNo = await GenerateLeaveReferenceNumberAsync(prefix, financialYearStart, financialYearEnd);
                        int sl = await GetNextLeaveSerialNumberAsync(financialYearStart, financialYearEnd);

                        observationFlow.Leave.Status = "Approved";
                        observationFlow.Leave.ReferenceNo = referenceNo;
                        observationFlow.Leave.Sl = sl;

                        _db.Leave.Update(observationFlow.Leave);
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    // Rejected, update leave status
                    observationFlow.Leave.Status = "Rejected";
                    _db.Leave.Update(observationFlow.Leave);
                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Leave {Status_to} successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error! Please try again later.", detail = ex.Message });
            }
        }
        private async Task<string> GenerateLeaveReferenceNumberAsync(string prefix, DateTime financialYearStart, DateTime financialYearEnd)
        {
            var serialNumber = await GetNextLeaveSerialNumberAsync(financialYearStart, financialYearEnd);

            string month = DateTime.Now.ToString("MM");
            string year = DateTime.Now.ToString("yyyy");

            return $"{prefix}/{month}-{year}/{serialNumber:00000}";
        }

        private async Task<int> GetNextLeaveSerialNumberAsync(DateTime financialYearStart, DateTime financialYearEnd)
        {

            // Get max Sl within the current financial year
            int? maxSl = await _db.Leave
                .Where(x => x.CreatedDatetime >= financialYearStart && x.CreatedDatetime <= financialYearEnd)
                .MaxAsync(x => (int?)x.Sl);

            return maxSl.HasValue ? maxSl.Value + 1 : 1;

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
                return BadRequest("Invalid ID format.");
            }

            var Leave = await _db.Leave
                .Include(x => x.Employee)
                .Include(x => x.LeaveMaster)
                .FirstOrDefaultAsync(m => m.Id == decryptedId);

            if (Leave == null)
                return NotFound("Data not found.");

            var createdByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == Leave.CreatedBy);
            var updatedByUser = await _db.Users.FirstOrDefaultAsync(m => m.Id == Leave.UpdatedBy);

            var leaveReasonname = await _db.LeaveMaster.FirstOrDefaultAsync(m => m.Id == Leave.LeaveReasonId);

            // Get current logged-in user
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

            // Get pending LeaveObservationFlow for this leave
            var pendingFlow = await _db.LeaveObservationFlow
                .Include(x => x.Users)
                .Include(x => x.RoleMaster)
                .Where(x => x.LeaveId == decryptedId && x.Status == "Pending")
                .OrderBy(x => x.Level)
                .FirstOrDefaultAsync();

            // Check if current user is the pending approver
            bool isPendingApprover = pendingFlow != null && pendingFlow.UserId == currentUserId;

            // Get all LeaveObservationFlow for this leave
            var leaveFlows = await _db.LeaveObservationFlow
                .Include(x => x.RoleMaster)
                .Include(x => x.Users)
                .Where(x => x.LeaveId == decryptedId)
                .OrderBy(x => x.Level)
                .ToListAsync();

            // Get all users for the flow display
            var users = await _db.Users.ToListAsync();

            var viewModel = new LeaveDetailsViewModel
            {
                Leave = Leave,
                CurrentUserId = currentUserId,
                CurrentUserName = currentUser?.Name ?? "Unknown",
                PendingFlow = pendingFlow,
                IsPendingApprover = isPendingApprover,
                CreatedBy = createdByUser?.Name ?? "Not Updated",
                UpdatedBy = updatedByUser?.Name ?? "Not Updated",
                LeaveReasonName = leaveReasonname?.LeaveName ?? "Not Updated",
                LeaveFlows = leaveFlows,
                Users = users
            };

            return View(viewModel);
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

            var Leave = await _db.Leave.FindAsync(decryptedId);
            if (Leave == null)
            {
                return Json(new { success = false, message = "Leave Data not found" });
            }

            try
            {
                // Delete related LeaveObservationFlow records
                var leaveObservationFlows = await _db.LeaveObservationFlow
                    .Where(x => x.LeaveId == decryptedId)
                    .ToListAsync();

                if (leaveObservationFlows.Any())
                {
                    _db.LeaveObservationFlow.RemoveRange(leaveObservationFlows);
                }

                // Delete related LeaveObservationDesiredFlow records
                var leaveObservationDesiredFlows = await _db.LeaveObservationDesiredFlow
                    .Where(x => x.LeaveId == decryptedId)
                    .ToListAsync();

                if (leaveObservationDesiredFlows.Any())
                {
                    _db.LeaveObservationDesiredFlow.RemoveRange(leaveObservationDesiredFlows);
                }

                // Delete the main Leave record
                _db.Leave.Remove(Leave);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Leave Record Deleted Successfully" });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {

                return Json(new { success = false, message = "Deletion failed" });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "An error occurred while deleting.", error = ex.Message });
            }


        }
        private string GenerateRandomNumber()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
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
