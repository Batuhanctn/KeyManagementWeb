using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using KeyManagementWeb.Data;
using KeyManagementWeb.Models;
using BCrypt.Net;

namespace KeyManagamentWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly KeyManagementContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(KeyManagementContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string email)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Tüm alanları doldurunuz!";
                return RedirectToAction("Register");
            }

            if (await _context.Users.AnyAsync(u => u.Username == username || u.UserMail == email))
            {
                TempData["Error"] = "Bu kullanıcı adı veya email zaten kullanımda!";
                return RedirectToAction("Register");
            }

            // Şifreyi hashle
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                UserPassword = hashedPassword,
                UserMail = email,
                UserPermission = null // Yeni kullanıcılar hiçbir yetkiye sahip olmaz
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Yeni kullanıcı kaydoldu: {username} - {DateTime.Now}");
            TempData["Success"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Kullanıcı adı ve şifre gereklidir!";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı adı veya şifre hatalı!";
                return RedirectToAction("Login");
            }

            bool isValidPassword = false;

            // Şifre BCrypt formatında mı kontrol et
            if (user.UserPassword?.StartsWith("$2") ?? false)
            {
                try
                {
                    // BCrypt ile doğrula
                    isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.UserPassword);
                }
                catch
                {
                    // BCrypt doğrulama hatası - muhtemelen geçersiz hash
                    isValidPassword = false;
                }
            }
            else
            {
                // Eski format - düz metin karşılaştırması
                isValidPassword = user.UserPassword == password;

                if (isValidPassword)
                {
                    // Şifreyi BCrypt formatına çevir
                    user.UserPassword = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Kullanıcı şifresi otomatik olarak güvenli formata dönüştürüldü: {username}");
                }
            }

            if (!isValidPassword)
            {
                TempData["Error"] = "Kullanıcı adı veya şifre hatalı!";
                return RedirectToAction("Login");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("UserMail", user.UserMail ?? string.Empty),
                new Claim("IsAdmin", (username == "admin").ToString())
            };

            if (!string.IsNullOrEmpty(user.UserPermission))
            {
                claims.Add(new Claim("Permissions", user.UserPermission));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties();

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"Kullanıcı giriş yaptı: {username} - {DateTime.Now}");

            if (username == "admin")
            {
                return RedirectToAction("AdminPanel", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MigratePasswords()
        {
            // Sadece admin kullanıcısının erişimine izin ver
            if (User.Identity?.Name != "admin")
            {
                _logger.LogWarning($"Yetkisiz şifre migrasyon denemesi: {User.Identity?.Name ?? "Anonim kullanıcı"}");
                return RedirectToAction("AccessDenied");
            }

            try
            {
                var users = await _context.Users.ToListAsync();
                int updatedCount = 0;

                foreach (var user in users)
                {
                    // Eğer şifre zaten BCrypt formatında ise veya boş/null ise atla
                    if (string.IsNullOrEmpty(user.UserPassword) || user.UserPassword.StartsWith("$2"))
                    {
                        continue;
                    }

                    try
                    {
                        // Mevcut şifreyi hashle
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword);
                        user.UserPassword = hashedPassword;
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Kullanıcı şifresi hashlenirken hata: {user.Username} - {ex.Message}");
                        continue;
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"{updatedCount} kullanıcının şifresi başarıyla hashlendi");
                    TempData["Success"] = $"{updatedCount} kullanıcının şifresi güvenli formata dönüştürüldü.";
                }
                else
                {
                    TempData["Info"] = "Tüm şifreler zaten güvenli formatta.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Şifre migrasyon hatası: {ex.Message}");
                TempData["Error"] = "Şifreler güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("AdminPanel", "Admin");
        }
    }
}
