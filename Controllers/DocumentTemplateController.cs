using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prj1.Data;
using prj1.Models;

namespace prj1.Controllers
{
    public class DocumentTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentTemplateController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: DocumentTemplate
        public async Task<IActionResult> Index()
        {
            ViewBag.CurrentUser = await GetCurrentUserAsync();
            var templates = await _context.DocumentTemplates.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(templates);
        }

        // GET: DocumentTemplate/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var template = await _context.DocumentTemplates.FindAsync(id);
            if (template == null)
                return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, template.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound("File không tồn tại trên server");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/octet-stream", template.FileName);
        }

        // GET: DocumentTemplate/Create
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != "Admin")
                return Forbid();
            return View();
        }

        // POST: DocumentTemplate/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentTemplate model, IFormFile file)
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != "Admin")
                return Forbid();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.Error = "Dữ liệu nhập chưa hợp lệ: " + string.Join("; ", errors);
                return View(model);
            }
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Bạn phải chọn file mẫu.";
                return View(model);
            }

            try
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "templates");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                model.FilePath = $"/uploads/templates/{uniqueFileName}";
                model.FileName = file.FileName;
                model.FileSize = file.Length;
                model.FileExtension = Path.GetExtension(file.FileName);
                model.CreatedAt = DateTime.Now;
                model.CreatedBy = user.Email;
                model.Status = 1;
                _context.DocumentTemplates.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi lưu mẫu: " + ex.Message;
                return View(model);
            }
        }

        // GET: DocumentTemplate/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != "Admin")
                return Forbid();
            if (id == null) return NotFound();
            var template = await _context.DocumentTemplates.FindAsync(id);
            if (template == null) return NotFound();
            return View(template);
        }

        // POST: DocumentTemplate/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentTemplate model, IFormFile file)
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != "Admin")
                return Forbid();
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.Error = "Dữ liệu nhập chưa hợp lệ: " + string.Join("; ", errors);
                return View(model);
            }
            var template = await _context.DocumentTemplates.FindAsync(id);
            if (template == null) return NotFound();
            template.Name = model.Name;
            template.Description = model.Description;
            template.Category = model.Category;
            template.Status = model.Status;
            // Nếu có file mới, xóa file cũ và upload file mới
            if (file != null && file.Length > 0)
            {
                // Xóa file cũ
                if (!string.IsNullOrEmpty(template.FilePath))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, template.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }
                // Upload file mới
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "templates");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                template.FilePath = $"/uploads/templates/{uniqueFileName}";
                template.FileName = file.FileName;
                template.FileSize = file.Length;
                template.FileExtension = Path.GetExtension(file.FileName);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: DocumentTemplate/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != "Admin")
                return Forbid();
            var template = await _context.DocumentTemplates.FindAsync(id);
            if (template == null) return NotFound();
            // Xóa file vật lý
            var filePath = Path.Combine(_env.WebRootPath, template.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            _context.DocumentTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Lấy user hiện tại
        private async Task<User> GetCurrentUserAsync()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return await _context.Users.FindAsync(userId);
        }
    }
} 