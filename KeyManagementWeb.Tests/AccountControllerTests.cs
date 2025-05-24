using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using KeyManagamentWeb.Controllers;
using KeyManagementWeb.Data;
using KeyManagementWeb.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Extensions.DependencyInjection;

namespace KeyManagementWeb.Tests
{

    [TestFixture]
    public class AccountControllerTests
    {
        private KeyManagementContext _dbContext;
        private AccountController _controller;
        private Mock<ILogger<AccountController>> _mockLogger;
        private List<User> _users;
        private Mock<ITempDataDictionaryFactory> _mockTempDataDictionaryFactory;
        private Mock<ITempDataDictionary> _mockTempData;

        [SetUp]
        public void TestInitialize()
        {
            // InMemory veritabanı oluştur
            var options = new DbContextOptionsBuilder<KeyManagementContext>()
                .UseInMemoryDatabase(databaseName: "TestUserDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new KeyManagementContext(options);
            
            // Test verileri
            _users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    Username = "testuser",
                    UserPassword = BCrypt.Net.BCrypt.HashPassword("password"),
                    UserMail = "test@example.com",
                    UserPermission = "admin"
                },
                new User
                {
                    UserId = 2,
                    Username = "olduser",
                    UserPassword = "oldpassword", // Hashlenmemiş eski format şifre
                    UserMail = "old@example.com",
                    UserPermission = "user"
                },
                new User
                {
                    UserId = 3,
                    Username = "admin",
                    UserPassword = "$2a$11$eoEs2TqwK.iIQkAKcyaJaOGPQ3QJKE0pHikLq/pXA.zRpTPnQMQQm", // "admin" BCrypt ile hashlenmiş
                    UserMail = "admin@example.com",
                    UserPermission = "A,U"
                },
                new User
                {
                    UserId = 4,
                    Username = "user2",
                    UserPassword = "password2", // Hashlenmemiş şifre
                    UserMail = "user2@example.com",
                    UserPermission = "D,U"
                }
            };

            // Veritabanını test verileriyle doldur
            _dbContext.ChangeTracker.Clear(); // Önceki tüm entity'leri temizle
            _dbContext.Users.AddRange(_users);
            _dbContext.SaveChanges();

            // Mock Logger
            _mockLogger = new Mock<ILogger<AccountController>>();

            // Mock HttpContext ve Authentication
            var mockHttpContext = new Mock<HttpContext>();
            var mockAuth = new Mock<IAuthenticationService>();
            mockAuth.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>())).Returns(Task.CompletedTask);
            mockAuth.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>())).Returns(Task.CompletedTask);
            
            var mockServiceProvider = new Mock<IServiceProvider>();
            
            // IUrlHelperFactory servisi
            var mockUrlHelperFactory = new Mock<Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory>();
            mockServiceProvider.Setup(s => s.GetService(typeof(Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory)))
                .Returns(mockUrlHelperFactory.Object);
                
            // ITempDataDictionaryFactory servisi
            _mockTempDataDictionaryFactory = new Mock<ITempDataDictionaryFactory>();
            _mockTempData = new Mock<ITempDataDictionary>();
            _mockTempDataDictionaryFactory.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(_mockTempData.Object);
            mockServiceProvider.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory)))
                .Returns(_mockTempDataDictionaryFactory.Object);
                
            // IAuthenticationService servisi
            mockServiceProvider.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(mockAuth.Object);
            mockHttpContext.Setup(h => h.RequestServices).Returns(mockServiceProvider.Object);
            
            // Mock Session
            var mockSession = new Mock<ISession>();
            var sessionStorage = new Dictionary<string, byte[]>();
            mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>())).Callback<string, byte[]>((key, value) => sessionStorage[key] = value);
            mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny)).Returns((string key, out byte[] value) => 
            {
                var success = sessionStorage.TryGetValue(key, out byte[] result);
                value = result;
                return success;
            });
            mockHttpContext.Setup(h => h.Session).Returns(mockSession.Object);

            // Controller oluşturma ve HttpContext ayarlama
            _controller = new AccountController(_dbContext, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // TempData değişkenlerini sınıf üyesi olarak kullanmaya devam ediyoruz
            // Üst kısımda zaten tanımladık
                
            _controller.TempData = _mockTempData.Object;
        }

        [Test]
        public void Login_ReturnsLoginView()
        {
            // Act
            var result = _controller.Login() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.ViewName); // Default view (Login)
        }

        [Test]
        public void Register_ReturnsRegisterView()
        {
            // Act
            var result = _controller.Register() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.ViewName); // Default view (Register)
        }

        [Test]
        public async Task Register_WithValidData_CreatesUserAndRedirects()
        {
            // Arrange
            string username = "newuser";
            string password = "newpassword";
            string email = "new@example.com";
            int initialCount = await _dbContext.Users.CountAsync();

            // Act
            var result = await _controller.Register(username, password, email) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Login", result.ActionName);

            var usersInDb = await _dbContext.Users.ToListAsync();
            Assert.AreEqual(initialCount + 1, usersInDb.Count);

            var newUser = usersInDb.FirstOrDefault(u => u.Username == username);
            Assert.IsNotNull(newUser);
            Assert.AreEqual(email, newUser.UserMail);
            Assert.True(newUser.UserPassword.StartsWith("$2"));
        }



        [Test]
        public async Task Register_WithExistingUsername_ReturnsError()
        {
            // Arrange
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;

            string username = "testuser"; // Zaten var olan kullanıcı
            string password = "newpassword";
            string email = "new@example.com";

            // Act
            var result = await _controller.Register(username, password, email) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Register", result.ActionName);

            // TempData'da hata mesajı olduğunu kontrol et
            Assert.IsTrue(_controller.TempData.ContainsKey("Error"));

            // Kullanıcı sayısının değişmediğini kontrol et
            Assert.AreEqual(1, _users.Count(u => u.Username == username));

            // Mevcut kullanıcının şifresinin hala aynı olduğunu kontrol et
            var existingUser = _users.First(u => u.Username == username);
            Assert.True(existingUser.UserPassword.StartsWith("$2"));
        }


        [Test]
        public async Task Login_WithValidCredentials_AuthenticatesAndRedirects()
        {
            // Arrange
            string username = "testuser";
            string password = "password";

            // Act
            var result = await _controller.Login(username, password) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);
            
            // Şifrenin hashlendiğini kontrol et
            var updatedUser = _users.First(u => u.Username == username);
            Assert.True(updatedUser.UserPassword.StartsWith("$2"));
        }

        [Test]
        public async Task Login_WithOldFormatPassword_UpdatesToHashedAndAuthenticates()
        {
            // Arrange
            string username = "olduser";
            string password = "oldpassword";
            // olduser'a ait şifrenin orjinal hali saklanıyor
            var originalUser = _users.FirstOrDefault(u => u.Username == username);
            Assert.IsNotNull(originalUser, "Test için olduser kullanıcısı bulunamadı");
            var originalPassword = originalUser.UserPassword;

            // Act
            var result = await _controller.Login(username, password) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result, "Login işlemi sonrası RedirectToActionResult dönmedi");
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);
            
            // Şifrenin hashlendiğini kontrol et
            var updatedUser = _users.FirstOrDefault(u => u.Username == username);
            Assert.IsNotNull(updatedUser, "Login sonrası olduser bulunamadı");
            Assert.AreNotEqual(originalPassword, updatedUser.UserPassword, "Şifre aynı kaldı, hashlenmemiş");
            Assert.True(updatedUser.UserPassword.StartsWith("$2"), "Şifre BCrypt formatında hashlenmedi");
        }

        [Test]
        public async Task Login_WithInvalidCredentials_ReturnsRedirectWithError()
        {
            // Arrange
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;

            string username = "testuser";
            string password = "wrongpassword";

            // Act
            var result = await _controller.Login(username, password) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.IsTrue(_controller.TempData.ContainsKey("Error"));
        }


        [Test]
        public async Task Logout_SignsOutAndRedirectsToLogin()
        {
            // Act
            var result = await _controller.Logout() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Login", result.ActionName);
        }

        [Test]
        public void AccessDenied_ReturnsAccessDeniedView()
        {
            // Act
            var result = _controller.AccessDenied() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.ViewName); // Default view (AccessDenied)
        }
    }
}
