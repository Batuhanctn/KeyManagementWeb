using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using KeyManagamentWeb.Controllers;
using KeyManagementWeb.Data;
using KeyManagementWeb.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace KeyManagementWeb.Tests
{


    [TestFixture]
    public class AdminControllerTests
    {
        private KeyManagementContext _dbContext; // Gerçek Context
        private AdminController _controller;
        private Mock<ILogger<AdminController>> _mockLogger;
        private List<User> _users;
        private List<Key> _keys;
        private Mock<HttpContext> _mockHttpContext;
        private Mock<ClaimsPrincipal> _mockUser;
        private Mock<ITempDataDictionaryFactory> _mockTempDataDictionaryFactory;
        private Mock<ITempDataDictionary> _mockTempData;

        [SetUp]
        public void TestInitialize()
        {
            // InMemory veritabanı oluştur
            var options = new DbContextOptionsBuilder<KeyManagementContext>()
                .UseInMemoryDatabase(databaseName: "TestAdminDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new KeyManagementContext(options);

            // Test verileri
            _users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    Username = "testuser",
                    UserPassword = "password",
                    UserMail = "test@example.com",
                    UserPermission = "U"
                },
                new User
                {
                    UserId = 2,
                    Username = "admin",
                    UserPassword = "admin",
                    UserMail = "admin@example.com",
                    UserPermission = "D,C,U"
                },
                new User
                {
                    UserId = 3,
                    Username = "user2",
                    UserPassword = "password2",
                    UserMail = "user2@example.com",
                    UserPermission = "D,U"
                }
            };

            _keys = new List<Key>
            {
                new Key
                {
                    KeyId = 1,
                    KeyType = "AES_KEY",
                    KeyValue = "TestKey1",
                    InsertDate = DateTime.Now.AddDays(-10),
                    KeyUpdateDay = 5
                },
                new Key
                {
                    KeyId = 2,
                    KeyType = "DES_KEY",
                    KeyValue = "TestKey2",
                    InsertDate = DateTime.Now.AddDays(-5),
                    KeyUpdateDay = 10
                }
            };

            // Veritabanına test verilerini ekle
            _dbContext.ChangeTracker.Clear(); // Önceki tüm entity'leri temizle
            _dbContext.Users.AddRange(_users);
            _dbContext.Keys.AddRange(_keys);
            _dbContext.SaveChanges();

            // Set up Admin Controller
            _mockLogger = new Mock<ILogger<AdminController>>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockUser = new Mock<ClaimsPrincipal>();
            
            // Set up TempData mock
            _mockTempData = new Mock<ITempDataDictionary>();
            _mockTempDataDictionaryFactory = new Mock<ITempDataDictionaryFactory>();
            _mockTempDataDictionaryFactory
                .Setup(factory => factory.GetTempData(It.IsAny<HttpContext>()))
                .Returns(_mockTempData.Object);
                
            // ServiceProvider kurulumu
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory)))
                .Returns(_mockTempDataDictionaryFactory.Object);
            
            _mockHttpContext.Setup(h => h.RequestServices).Returns(mockServiceProvider.Object);

            // Admin User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _mockHttpContext.Setup(c => c.User).Returns(principal);
            
            // Controller oluşturma
            _controller = new AdminController(_dbContext, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };

            // Mock authentication result
            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService
                .Setup(auth => auth.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            mockServiceProvider
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);
        }

        private Mock<HttpContext> SetupRegularUser()
        {
            // Normal kullanıcı için identity ve claims
            var mockUser = new Mock<ClaimsPrincipal>();
            var identity = new Mock<ClaimsIdentity>();
            identity.Setup(i => i.Name).Returns("testuser");
            identity.Setup(i => i.IsAuthenticated).Returns(true);
            mockUser.Setup(u => u.Identity).Returns(identity.Object);
            mockUser.Setup(u => u.Claims).Returns(new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("Permissions", "U")
            });

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
            
            // Gerekli servisleri ekleyelim
            var mockServiceProvider = new Mock<IServiceProvider>();
            
            // IUrlHelperFactory servisi
            var mockUrlHelperFactory = new Mock<Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory>();
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(h => h.Action(It.IsAny<UrlActionContext>()))
                .Returns("testurl");
                
            mockUrlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(mockUrlHelper.Object);
                
            mockServiceProvider.Setup(s => s.GetService(typeof(Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory)))
                .Returns(mockUrlHelperFactory.Object);
            mockHttpContext.Setup(h => h.RequestServices).Returns(mockServiceProvider.Object);
            
            return mockHttpContext;
        }    
        

        [Test]
        public void AdminPanel_WithAdminUser_ReturnsViewWithUsers()
        {
            // Arrange - TestInitialize'da admin user ayarlandı

            // Act
            var result = _controller.AdminPanel() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as List<User>;
            Assert.NotNull(model);
            
            // admin hesabı dışındaki kullanıcıları listeler
            Assert.AreEqual(2, model.Count);
            Assert.False(model.Any(u => u.Username == "admin"));
        }

        [Test]
        public void AdminPanel_WithNonAdminUser_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = SetupRegularUser(); // Normal kullanıcı ile test
            
            // Controller context güncelleme
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.AdminPanel() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [Test]
        public async Task UpdatePermissions_WithAdminUser_UpdatesPermissions()
        {
            // Admin rolüne sahip kullanıcı oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("Permissions", "U")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _mockHttpContext.Setup(c => c.User).Returns(principal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
            
            // Arrange
            int userId = 1;
            string[] permissions = new string[] { "D", "U" };
            var originalUser = _users.FirstOrDefault(u => u.UserId == userId);
            var originalPermissions = originalUser?.UserPermission;

            // Act
            var result = await _controller.UpdatePermissions(userId, permissions) as ViewResult;

            // Assert
            Assert.NotNull(result);
            // Eğer controller AdminPanel view'ı döndürmüyorsa, ViewResult olarak kontrol edelim
            Assert.AreEqual("AdminPanel", result.ViewName ?? "");
            
            // Yetkilerin güncellendiğini kontrol et
            var updatedUser = _users.FirstOrDefault(u => u.UserId == userId);
            Assert.NotNull(updatedUser);
            Assert.AreEqual("DU", updatedUser.UserPermission); // Yetkiler alfabetik sıralanır
            Assert.AreNotEqual(originalPermissions, updatedUser.UserPermission);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        [Test]
        public async Task UpdatePermissions_WithNonExistentUser_ReturnsError()
        {
            // Admin rolüne sahip kullanıcı oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("Permissions", "A")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _mockHttpContext.Setup(c => c.User).Returns(principal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
            
            // Arrange
            int userId = 999; // Var olmayan kullanıcı
            string[] permissions = new string[] { "D", "U" };

            // Act
            var result = await _controller.UpdatePermissions(userId, permissions) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("AdminPanel", result.ViewName ?? "");
            Assert.IsTrue(_mockTempData.Object.ContainsKey("Error"));
        }

        [Test]
        public async Task UpdatePermissions_WithNonAdminUser_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = SetupRegularUser(); // Normal kullanıcı ile test
            
            // Controller context güncelleme
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };
            
            int userId = 1;
            string[] permissions = new[] { "D", "U" };

            // Act
            var result = await _controller.UpdatePermissions(userId, permissions) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [Test]
        public async Task UpdatePermissions_WithNoPermissions_RemovesPermissions()
        {
            // Admin rolüne sahip kullanıcı oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("Permissions", "A")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _mockHttpContext.Setup(c => c.User).Returns(principal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
            
            // Arrange
            int userId = 1;
            string[] permissions = new string[] { }; // Boş izin listesi
            var originalUser = _users.FirstOrDefault(u => u.UserId == userId);
            var originalPermissions = originalUser?.UserPermission;

            // Act
            var result = await _controller.UpdatePermissions(userId, permissions) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("AdminPanel", result.ViewName ?? "");
            
            // Yetkilerin kaldırıldığını kontrol et
            var updatedUser = _users.FirstOrDefault(u => u.UserId == userId);
            Assert.NotNull(updatedUser);
            Assert.AreEqual("", updatedUser.UserPermission);
        }

        [Test]
        public void IsAdminUser_WithAdminUser_ReturnsTrue()
        {
            // Admin rolüne sahip kullanıcı oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin"), // Role olarak admin tanımlı
                new Claim("Permissions", "A") // Veya eski yöntemle Permission A tanımlı
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _mockHttpContext.Setup(c => c.User).Returns(principal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
            
            // Bu metot private olduğu için reflection ile erişiyoruz
            var method = typeof(AdminController).GetMethod("IsAdminUser", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            var result = (bool)method.Invoke(_controller, null);

            // Assert
            Assert.True(result, "Kullanıcı admin rolüne sahip olmasına rağmen IsAdminUser false döndürdü");
        }

        [Test]
        public void IsAdminUser_WithRegularUser_ReturnsFalse()
        {
            // Arrange
            SetupRegularUser();
            
            // Bu metot private olduğu için reflection ile erişiyoruz
            var method = typeof(AdminController).GetMethod("IsAdminUser", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            var result = (bool)method.Invoke(_controller, null);

            // Assert
            Assert.False(result);
        }
    }
}
