using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prj1.Data;
using prj1.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace prj1.Controllers
{
    public class LegalProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;
        private const string UserCacheKey = "CurrentUser_{0}"; // {0} là userId

        public LegalProfileController(ApplicationDbContext context, IWebHostEnvironment environment, IMemoryCache cache)
        {
            _context = context;
            _environment = environment;
            _cache = cache;
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cacheKey = string.Format(UserCacheKey, userId);

            if (!_cache.TryGetValue(cacheKey, out User user))
            {
                user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    _cache.Set(cacheKey, user, TimeSpan.FromMinutes(30));
                }
            }

            return user;
        }

        // GET: LegalProfile
        public async Task<IActionResult> Index(LegalProfileStatus? status)
        {
            var user = await GetCurrentUserAsync();
            ViewBag.CurrentUser = user;

            var query = _context.LegalProfiles
                .Include(l => l.Files)
                .Where(l => l.UserId == user.Id || 
                           _context.LegalProfilePermissions
                               .Any(p => p.LegalProfileId == l.Id && p.UserId == user.Id));

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            var legalProfiles = await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(legalProfiles);
        }

        // GET: LegalProfile/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: LegalProfile/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LegalProfile legalProfile, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();
                legalProfile.UserId = user.Id;
                legalProfile.CreatedAt = DateTime.Now;
                legalProfile.Status = LegalProfileStatus.Draft;

                // 1. Lưu hồ sơ trước để lấy Id
                _context.Add(legalProfile);
                await _context.SaveChangesAsync();

                // 2. Sau đó mới thêm file đính kèm (nếu có)
                if (file != null && file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "legal-profiles", legalProfile.Id.ToString());
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    using (var sha256 = SHA256.Create())
                    using (var stream = file.OpenReadStream())
                    {
                        var hash = sha256.ComputeHash(stream);
                        var fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                        var legalProfileFile = new LegalProfileFile
                        {
                            FileName = fileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            FileSize = file.Length,
                            FileHash = fileHash,
                            LegalProfileId = legalProfile.Id // Đã có Id thật
                        };

                        _context.LegalProfileFiles.Add(legalProfileFile);
                        await _context.SaveChangesAsync();
                    }
                }

                // Audit log...
                var auditLog = new AuditLog
                {
                    LegalProfileId = legalProfile.Id,
                    UserId = user.Id,
                    UserName = user.FullName,
                    Action = "Create",
                    ChangedFields = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "Name", legalProfile.Name },
                        { "Description", legalProfile.Description },
                        { "Status", legalProfile.Status.ToString() }
                    }),
                    Timestamp = DateTime.Now
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(legalProfile);
        }

        // GET: LegalProfile/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();

            // Lấy hồ sơ nếu là chủ sở hữu, admin hoặc có quyền Edit
            var profile = await _context.LegalProfiles
                .Include(lp => lp.Files)
                .Include(lp => lp.Permissions)
                .FirstOrDefaultAsync(lp =>
                    lp.Id == id &&
                    (lp.UserId == user.Id ||
                     lp.Permissions.Any(p => p.UserId == user.Id && p.PermissionType == "Edit") ||
                     user.Role == "Admin")
                );

            if (profile == null)
            {
                return Forbid();
            }

            // Lấy lịch sử chỉnh sửa
            var auditLogs = await _context.AuditLogs
                .Where(l => l.LegalProfileId == id)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            ViewBag.AuditLogs = auditLogs;

            return View(profile);
        }

        // POST: LegalProfile/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LegalProfile profile, List<IFormFile> files)
        {
            var user = await GetCurrentUserAsync();

            // Lấy hồ sơ nếu là chủ sở hữu, admin hoặc có quyền Edit
            var existingProfile = await _context.LegalProfiles
                .Include(lp => lp.Permissions)
                .FirstOrDefaultAsync(lp =>
                    lp.Id == id &&
                    (lp.UserId == user.Id ||
                     lp.Permissions.Any(p => p.UserId == user.Id && p.PermissionType == "Edit") ||
                     user.Role == "Admin")
                );

            if (existingProfile == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingProfile.Name = profile.Name;
                    existingProfile.Description = profile.Description;
                    existingProfile.Status = profile.Status;

                    if (files != null && files.Any())
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "legal-profiles", profile.Id.ToString());
                        Directory.CreateDirectory(uploadsFolder);

                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = Path.GetFileName(file.FileName);
                                var filePath = Path.Combine(uploadsFolder, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                // Tính hash SHA-256
                                string fileHash;
                                using (var sha256 = SHA256.Create())
                                {
                                    using (var fs = System.IO.File.OpenRead(filePath))
                                    {
                                        var hashBytes = sha256.ComputeHash(fs);
                                        fileHash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
                                    }
                                }

                                var profileFile = new LegalProfileFile
                                {
                                    LegalProfileId = profile.Id,
                                    FileName = fileName,
                                    FilePath = $"/uploads/legal-profiles/{profile.Id}/{fileName}",
                                    ContentType = file.ContentType,
                                    FileSize = file.Length,
                                    FileHash = fileHash
                                };

                                _context.LegalProfileFiles.Add(profileFile);
                            }
                        }
                    }

                    var auditLog = new AuditLog
                    {
                        LegalProfileId = profile.Id,
                        UserId = user.Id,
                        UserName = user.FullName,
                        Action = "Edit",
                        ChangedFields = JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            { "Name", profile.Name },
                            { "Description", profile.Description },
                            { "Status", profile.Status.ToString() }
                        }),
                        Timestamp = DateTime.Now
                    };

                    _context.AuditLogs.Add(auditLog);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LegalProfileExists(profile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        // POST: LegalProfile/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var profile = await _context.LegalProfiles
                .Include(lp => lp.Files)
                .FirstOrDefaultAsync(lp => lp.Id == id && lp.UserId == userId);

            if (profile == null)
            {
                return NotFound();
            }

            // Xóa các file vật lý
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "legal-profiles", profile.Id.ToString());
            if (Directory.Exists(uploadsFolder))
            {
                Directory.Delete(uploadsFolder, true);
            }

            _context.LegalProfiles.Remove(profile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: LegalProfile/DeleteFile/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var file = await _context.LegalProfileFiles
                .Include(f => f.LegalProfile)
                .FirstOrDefaultAsync(f => f.Id == id && f.LegalProfile.UserId == userId);

            if (file == null)
            {
                return NotFound();
            }

            // Xóa file vật lý
            var filePath = Path.Combine(_environment.WebRootPath, file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.LegalProfileFiles.Remove(file);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = file.LegalProfileId });
        }

        // GET: LegalProfile/XacMinh
        public IActionResult XacMinh()
        {
            return View();
        }

        // POST: LegalProfile/XacMinh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacMinh(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Result = "Vui lòng chọn file để xác minh.";
                return View();
            }

            // Tính hash SHA-256 của file upload
            string fileHash;
            using (var sha256 = SHA256.Create())
            {
                using (var stream = file.OpenReadStream())
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    fileHash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
                }
            }

            // Tìm trong DB
            var matchedFile = await _context.LegalProfileFiles
                .Include(f => f.LegalProfile)
                .FirstOrDefaultAsync(f => f.FileHash == fileHash);
            if (matchedFile != null)
            {
                ViewBag.Result = $"Xác minh thành công! File thuộc hồ sơ: {matchedFile.LegalProfile.Name} (ID: {matchedFile.LegalProfileId})";
            }
            else
            {
                ViewBag.Result = "Không tìm thấy file phù hợp trong hệ thống.";
            }
            return View();
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();
            ViewBag.CurrentUser = user;

            var legalProfile = await _context.LegalProfiles
                .Include(l => l.Files)
                .Include(l => l.User)
                .Include(l => l.AuditLogs)
                .Include(l => l.Permissions)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (legalProfile == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập: chủ sở hữu, admin hoặc có quyền trong Permissions
            if (legalProfile.UserId != user.Id && user.Role != "Admin" &&
                !legalProfile.Permissions.Any(p => p.UserId == user.Id))
            {
                return Forbid();
            }

            return View(legalProfile);
        }

        // GET: LegalProfile/Share/5
        public async Task<IActionResult> Share(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            var legalProfile = await _context.LegalProfiles
                .Include(l => l.Permissions)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (legalProfile == null)
            {
                return NotFound();
            }
            // Chỉ chủ sở hữu hoặc admin mới được phân quyền
            if (legalProfile.UserId != user.Id && user.Role != "Admin")
            {
                return Forbid();
            }
            // Lấy danh sách user (trừ chủ sở hữu)
            var users = await _context.Users.Where(u => u.Id != legalProfile.UserId).ToListAsync();
            ViewBag.Users = users;
            ViewBag.LegalProfile = legalProfile;
            return View();
        }

        // POST: LegalProfile/Share/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(int id, int userId, string permissionType)
        {
            var user = await GetCurrentUserAsync();
            var legalProfile = await _context.LegalProfiles
                .Include(l => l.Permissions)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (legalProfile == null)
            {
                return NotFound();
            }
            if (legalProfile.UserId != user.Id && user.Role != "Admin")
            {
                return Forbid();
            }
            // Kiểm tra đã có quyền chưa
            var existing = legalProfile.Permissions.FirstOrDefault(p => p.UserId == userId);
            if (existing != null)
            {
                existing.PermissionType = permissionType;
            }
            else
            {
                var permission = new LegalProfilePermission
                {
                    LegalProfileId = id,
                    UserId = userId,
                    PermissionType = permissionType
                };
                _context.LegalProfilePermissions.Add(permission);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePermission(int legalProfileId, int userId)
        {
            var user = await GetCurrentUserAsync();
            var legalProfile = await _context.LegalProfiles
                .Include(l => l.Permissions)
                .FirstOrDefaultAsync(l => l.Id == legalProfileId);
            if (legalProfile == null)
            {
                return NotFound();
            }
            if (legalProfile.UserId != user.Id && user.Role != "Admin")
            {
                return Forbid();
            }
            var perm = legalProfile.Permissions.FirstOrDefault(p => p.UserId == userId);
            if (perm != null)
            {
                _context.LegalProfilePermissions.Remove(perm);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = legalProfileId });
        }

        private bool LegalProfileExists(int id)
        {
            return _context.LegalProfiles.Any(e => e.Id == id);
        }
    }
} 