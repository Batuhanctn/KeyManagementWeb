using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using KeyManagementWeb.Data;

namespace KeyManagamentWeb.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly KeyManagementContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(KeyManagementContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdminUser()
        {
            // Kullanıcının kimliği var mı kontrol et
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Admin kullanıcı adını kontrol et
            if (User.Identity?.Name != "admin")
            {
                return false;
            }

            // Claims'den admin kontrolü
            var isAdminClaim = User.Claims.FirstOrDefault(c => c.Type == "IsAdmin")?.Value;
            if (isAdminClaim != "True")
            {
                return false;
            }

            return true;
        }

        public IActionResult AdminPanel()
        {
            // Admin kontrolü
            if (!IsAdminUser())
            {
                _logger.LogWarning($"Yetkisiz admin paneli erişim denemesi: {User.Identity?.Name ?? "Anonim kullanıcı"}");
                return RedirectToAction("Login", "Account");
            }

            var users = _context.Users.Where(u => u.Username != "admin" && u.Username != null).ToList();
            _logger.LogInformation($"Admin paneli yüklendi. {users.Count} kullanıcı listelendi.");
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePermissions(int userId, string[] permissions)
        {
            try
            {
                // Admin kontrolü
                if (!IsAdminUser())
                {
                    _logger.LogWarning($"Yetkisiz yetki güncelleme denemesi: {User.Identity?.Name ?? "Anonim kullanıcı"}");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Yetki güncelleme isteği alındı - UserId: {userId}, Permissions: {(permissions != null ? string.Join(",", permissions) : "null")}");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError($"Kullanıcı bulunamadı. UserId: {userId}");
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction(nameof(AdminPanel));
                }

                var newPermissions = permissions != null && permissions.Length > 0
                    ? string.Join("", permissions.OrderBy(p => p))
                    : "";

                _logger.LogInformation($"Yetki güncelleme - Kullanıcı: {user.Username}, Eski: {user.UserPermission ?? "Yok"}, Yeni: {newPermissions}");

                user.UserPermission = newPermissions;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Kullanıcı yetkileri güncellendi: {user.Username} - Yeni yetkiler: {newPermissions}");

                TempData["Success"] = "Yetkiler başarıyla güncellendi";
                return RedirectToAction(nameof(AdminPanel));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Yetki güncelleme hatası: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Yetkiler güncellenirken bir hata oluştu.";
                return RedirectToAction(nameof(AdminPanel));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                // Admin kontrolü
                if (!IsAdminUser())
                {
                    _logger.LogWarning($"Yetkisiz kullanıcı silme denemesi: {User.Identity?.Name ?? "Anonim kullanıcı"}");
                    return Json(new { success = false, message = "Yetkisiz erişim" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError($"Silinecek kullanıcı bulunamadı. UserId: {userId}");
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                if (user.Username == "admin")
                {
                    _logger.LogWarning($"Admin kullanıcısı silinmeye çalışıldı. UserId: {userId}");
                    return Json(new { success = false, message = "Admin kullanıcısı silinemez" });
                }

                _logger.LogInformation($"Kullanıcı silme işlemi başlatıldı - UserId: {userId}, Username: {user.Username}");

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Kullanıcı başarıyla silindi - Username: {user.Username}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kullanıcı silme hatası: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu" });
            }
        }
    }
}
