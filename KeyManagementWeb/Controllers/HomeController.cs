using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using KeyManagementWeb.Data;
using KeyManagementWeb.Models;
using KeyManagementWeb.Services;
using System.Net.Mail;
using System.Net;

namespace KeyManagamentWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly KeyManagementContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(KeyManagementContext context, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private async Task SendEmailNotification(string userName, int keyId, string action, string details)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromEmail = emailSettings["FromEmail"];
                var toEmail = emailSettings["ToEmail"];
                var smtpServer = emailSettings["SmtpServer"];
                var port = int.Parse(emailSettings["Port"]);
                var username = emailSettings["Username"];
                var password = emailSettings["Password"];

                var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = "Key Management System - Değişiklik Bildirimi",
                    Body = $@"Aşağıdaki değişiklik yapılmıştır:

Kullanıcı: {userName}
Key ID: {keyId}
İşlem: {action}
Detaylar: {details}
Tarih: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    IsBodyHtml = false
                };

                using var client = new SmtpClient(smtpServer, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email notification sent for user {userName} - Action: {action}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email sending failed: {ex.Message}");
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Admin tüm yetkilere sahiptir
                if (User.Identity?.Name == "admin")
                {
                    var adminKeys = await _context.Keys.OrderByDescending(k => k.InsertDate).ToListAsync();
                    ViewBag.UserPermissions = "DUC";
                    return View(adminKeys);
                }

                var userPermissions = User.Claims.FirstOrDefault(c => c.Type == "Permissions")?.Value;

                // Eğer kullanıcının hiç yetkisi yoksa boş liste göster
                if (string.IsNullOrEmpty(userPermissions))
                {
                    ViewBag.UserPermissions = "";
                    TempData["Warning"] = "Adminin yetki vermesini bekleyiniz.";
                    return View(new List<Key>());
                }

                var userKeys = await _context.Keys.OrderByDescending(k => k.InsertDate).ToListAsync();
                ViewBag.UserPermissions = userPermissions;

                _logger.LogInformation($"Kullanıcı {User.Identity?.Name} ana sayfayı görüntüledi. Toplam key sayısı: {userKeys.Count}");
                return View(userKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ana sayfa yüklenirken hata: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Veriler yüklenirken bir hata oluştu.";
                return View(new List<Key>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateKeyValue(int keyId)
        {
            var userPermissions = User.Identity?.Name == "admin" ? "DUC" :
                User.Claims.FirstOrDefault(c => c.Type == "Permissions")?.Value;

            if (string.IsNullOrEmpty(userPermissions) || !userPermissions.Contains("U"))
            {
                TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            var key = await _context.Keys.FindAsync(keyId);
            if (key == null)
            {
                return NotFound();
            }

            // Eski key değerini history tablosuna kaydet
            var keyHistory = new KeyHistory
            {
                KeyId = keyId,
                KeyValue = key.KeyValue,
                ChangeDate = DateTime.Now,
                ChangedBy = await GetCurrentUserIdAsync()
            };
            _context.KeyHistory.Add(keyHistory);

            // Key type'a göre yeni key değeri oluştur
            string newKeyValue = key.KeyType?.ToUpper() switch
            {
                "AES_KEY" => GenerateAESKey(),
                "DES_KEY" => GenerateDESKey(),
                _ => GenerateAESKey() // Varsayılan olarak AES key üret
            };

            // Yeni key değerini güncelle
            key.KeyValue = newKeyValue;
            key.LastUpdateDay = DateTime.Now;
            await _context.SaveChangesAsync();

            await SendEmailNotification(
                User.Identity?.Name ?? "Unknown",
                keyId,
                "Key Değeri Güncelleme",
                $"Key tipi: {key.KeyType}, Yeni değer atandı"
            );

            LogAction($"Kullanıcı {User.Identity?.Name} key değerini otomatik güncelledi - KeyId: {keyId}, KeyType: {key.KeyType}");
            TempData["Success"] = "Key değeri başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKeyUpdateDay(int keyId, int newUpdateDay)
        {
            var userPermissions = User.Identity?.Name == "admin" ? "DUC" :
                User.Claims.FirstOrDefault(c => c.Type == "Permissions")?.Value;

            if (string.IsNullOrEmpty(userPermissions) || !userPermissions.Contains("C"))
            {
                TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            var key = await _context.Keys.FindAsync(keyId);
            if (key == null)
            {
                return NotFound();
            }

            key.KeyUpdateDay = newUpdateDay;

            // Key güncelleme gününü history'ye kaydet
            var keyHistory = new KeyHistory
            {
                KeyId = keyId,
                KeyValue = key.KeyValue,
                ChangeDate = DateTime.Now,
                ChangedBy = await GetCurrentUserIdAsync()
            };
            _context.KeyHistory.Add(keyHistory);
            await _context.SaveChangesAsync();

            await SendEmailNotification(
                User.Identity?.Name ?? "Unknown",
                keyId,
                "Key Güncelleme Günü Değişikliği",
                $"Yeni güncelleme günü: {newUpdateDay}"
            );

            LogAction($"Kullanıcı {User.Identity?.Name} key güncelleme gününü değiştirdi - KeyId: {keyId}, Yeni değer: {newUpdateDay}");
            TempData["Success"] = "Key güncelleme günü başarıyla değiştirildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteKey(int keyId)
        {
            var userPermissions = User.Identity?.Name == "admin" ? "DUC" :
                User.Claims.FirstOrDefault(c => c.Type == "Permissions")?.Value;

            if (string.IsNullOrEmpty(userPermissions) || !userPermissions.Contains("D"))
            {
                TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            var key = await _context.Keys.FindAsync(keyId);
            if (key == null)
            {
                return NotFound();
            }

            try
            {
                // Key'e ait tüm history kayıtlarını sil
                var keyHistories = await _context.KeyHistory.Where(kh => kh.KeyId == keyId).ToListAsync();
                _context.KeyHistory.RemoveRange(keyHistories);
                await _context.SaveChangesAsync();

                // Sonra key'i sil
                _context.Keys.Remove(key);
                await _context.SaveChangesAsync();

                await SendEmailNotification(
                    User.Identity?.Name ?? "Unknown",
                    keyId,
                    "Key Silme",
                    "Key ve ilgili geçmişi silindi"
                );

                LogAction($"Kullanıcı {User.Identity?.Name} key ve ilgili geçmişini sildi - KeyId: {keyId}");
                TempData["Success"] = "Key ve geçmişi başarıyla silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Key silme işlemi sırasında hata: {ex.Message}");
                TempData["Error"] = "Key silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewKey(string keyType)
        {
            try
            {
                var userPermissions = User.Identity?.Name == "admin" ? "DUC" :
                    User.Claims.FirstOrDefault(c => c.Type == "Permissions")?.Value;

                if (string.IsNullOrEmpty(userPermissions) || !userPermissions.Contains("U"))
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction(nameof(Index));
                }

                string keyValue = keyType?.ToUpper() switch
                {
                    "AES_KEY" => GenerateAESKey(),
                    "DES_KEY" => GenerateDESKey(),
                    _ => GenerateAESKey() // Varsayılan olarak AES key üret
                };

                var currentDate = DateTime.Now;


                var key = new Key
                {
                    KeyValue = keyValue,
                    KeyType = keyType,
                    KeyUpdateDay = 5, // Default olarak 5 gün
                    LastUpdateDay = currentDate,
                    InsertDate = currentDate,
                };

                _context.Keys.Add(key);
                await _context.SaveChangesAsync();

                LogAction($"Kullanıcı {User.Identity?.Name} yeni bir key oluşturdu - KeyType: {keyType}");
                TempData["Success"] = "Yeni key başarıyla oluşturuldu.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Key oluşturma işlemi sırasında hata: {ex.Message}");
                TempData["Error"] = "Key oluşturulurken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            return User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
        }

        private string GenerateAESKey()
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        private string GenerateDESKey()
        {
            using var des = System.Security.Cryptography.DES.Create();
            des.GenerateKey();
            return Convert.ToBase64String(des.Key);
        }

        private void LogAction(string message)
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "log.txt");
                var logDir = Path.GetDirectoryName(logPath);

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} - {message}{Environment.NewLine}";
                System.IO.File.AppendAllText(logPath, logMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Loglama hatası: {ex.Message}");
            }
        }
    }
}
