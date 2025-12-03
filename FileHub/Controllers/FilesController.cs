using FileHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Controllers
{
    [Authorize]
    public class FilesController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Отримуємо шлях до папки поточного користувача
        // Структура: UserFiles/UserName
        private string GetUserFolder()
        {
            var user = User.Identity?.Name ?? "anonymous";
            var path = Path.Combine(_env.ContentRootPath, "UserFiles", user);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        // --- ГОЛОВНА СТОРІНКА З ФАЙЛАМИ ---
        public IActionResult Index(string sort = "date_desc", string filter = "all")
        {
            var folder = GetUserFolder();
            var currentUser = User.Identity?.Name ?? "Unknown";

            // 1. Зчитуємо всі файли з папки
            var files = Directory.GetFiles(folder)
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new FileItem
                    {
                        Name = info.Name,
                        Path = path,
                        CreatedAt = info.CreationTime,
                        ModifiedAt = info.LastWriteTime,
                        Size = info.Length,
                        // Оскільки папка належить юзеру, він і є тим, хто завантажив/редагував
                        UploadedBy = currentUser,
                        EditedBy = currentUser
                    };
                })
                .ToList();

            // 2. Фільтрація (case-insensitive)
            if (filter == "c")
            {
                files = files.Where(f => f.Extension.Equals(".c", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (filter == "jpg")
            {
                files = files.Where(f =>
                    f.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    f.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            // Якщо filter == "all", нічого не робимо, показуємо все

            // 3. Сортування
            switch (sort)
            {
                case "date_asc": // Дата (зростання: спочатку старі)
                    files = files.OrderBy(f => f.CreatedAt).ToList();
                    break;
                case "date_desc": // Дата (спадання: спочатку нові)
                    files = files.OrderByDescending(f => f.CreatedAt).ToList();
                    break;
                case "asc": // Ім'я (А-Я)
                    files = files.OrderBy(f => f.Name).ToList();
                    break;
                case "desc": // Ім'я (Я-А)
                    files = files.OrderByDescending(f => f.Name).ToList();
                    break;
                default: // За замовчуванням - нові зверху
                    files = files.OrderByDescending(f => f.CreatedAt).ToList();
                    break;
            }

            // Зберігаємо поточні параметри, щоб View знав, які кнопки підсвітити
            ViewBag.Sort = sort;
            ViewBag.Filter = filter;

            return View(files);
        }

        // --- ЗАВАНТАЖЕННЯ ФАЙЛУ ---
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, bool overwrite = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не вибрано");

            var folder = GetUserFolder();
            var path = Path.Combine(folder, file.FileName);

            // Якщо файл існує і ми НЕ хочемо перезаписувати -> вертаємо помилку конфлікту (409)
            if (System.IO.File.Exists(path) && !overwrite)
            {
                return Conflict(new { exists = true, name = file.FileName });
            }

            // Логіка створення копії реалізована на фронтенді (JavaScript шле новий файл з новим ім'ям),
            // тому тут ми просто зберігаємо файл.

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { success = true });
        }

        // --- ВИДАЛЕННЯ ФАЙЛУ ---
        [HttpPost]
        public IActionResult Delete(string name)
        {
            var folder = GetUserFolder();
            var path = Path.Combine(folder, name);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            return RedirectToAction(nameof(Index));
        }

        // --- СКАЧУВАННЯ ФАЙЛУ ---
        [HttpGet]
        public IActionResult Download(string name)
        {
            var folder = GetUserFolder();
            var path = Path.Combine(folder, name);

            if (!System.IO.File.Exists(path)) return NotFound();

            var contentType = "application/octet-stream";
            return File(System.IO.File.OpenRead(path), contentType, name);
        }

        // --- ПОПЕРЕДНІЙ ПЕРЕГЛЯД ---
        [HttpGet]
        public IActionResult Preview(string name)
        {
            var folder = GetUserFolder();
            var path = Path.Combine(folder, name);

            if (!System.IO.File.Exists(path)) return NotFound();

            var ext = Path.GetExtension(name).ToLowerInvariant();

            // 1. Картинки
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".webp")
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                var base64 = Convert.ToBase64String(bytes);
                // MIME type спрощено для прикладу, можна визначити точніше
                return Json(new { type = "image", base64 = base64, mime = "image/png" });
            }
            // 2. Текстові файли та код (JS, C, HTML, XML, CSS, JSON, TXT)
            else if (ext == ".js" || ext == ".c" || ext == ".cpp" || ext == ".h" || ext == ".cs" ||
                     ext == ".txt" || ext == ".xml" || ext == ".json" || ext == ".html" || ext == ".css")
            {
                var text = System.IO.File.ReadAllText(path);
                return Json(new { type = "text", text = text });
            }
            // 3. Непідтримуваний тип
            else
            {
                return Json(new { type = "unsupported" });
            }
        }
    }
}