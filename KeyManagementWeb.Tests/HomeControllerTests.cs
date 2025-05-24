using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using KeyManagamentWeb.Controllers;
using KeyManagementWeb.Data;
using KeyManagementWeb.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;

namespace KeyManagementWeb.Tests
{


    [TestFixture]
    public class HomeControllerTests
    {
        private KeyManagementContext _dbContext; // Gerçek Context
        private Mock<ILogger<HomeController>> _mockLogger;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<HttpContext> _mockHttpContext;
        private Mock<ClaimsPrincipal> _mockUser;
        private HomeController _controller;
        private Mock<DbSet<Key>> _mockKeyDbSet;
        private Mock<DbSet<KeyHistory>> _mockKeyHistoryDbSet;
        private List<Key> _keys;
        private List<KeyHistory> _keyHistories;
        private Mock<ITempDataDictionaryFactory> _mockTempDataDictionaryFactory;
        private Mock<ITempDataDictionary> _mockTempData;

        [SetUp]
        public void Setup()
        {
            // Mock verilerin hazırlanması
            _keys = new List<Key>
            {
                new Key 
                { 
                    KeyId = 1, 
                    KeyType = "AES_KEY", 
                    KeyValue = "TestKey1", 
                    InsertDate = DateTime.Now.AddDays(-10),
                    KeyUpdateDay = 5,
                    LastUpdateDay = DateTime.Now.AddDays(-3)
                },
                new Key 
                { 
                    KeyId = 2, 
                    KeyType = "DES_KEY", 
                    KeyValue = "TestKey2", 
                    InsertDate = DateTime.Now.AddDays(-5),
                    KeyUpdateDay = 10,
                    LastUpdateDay = DateTime.Now.AddDays(-1)
                }
            };

            _keyHistories = new List<KeyHistory>
            {
                new KeyHistory 
                { 
                    HistoryId = 1, 
                    KeyId = 1, 
                    KeyValue = "OldKey1", 
                    ChangeDate = DateTime.Now.AddDays(-5),
                    ChangedBy = "testuser"
                }
            };

            // Mock KeyDbSet
            _mockKeyDbSet = new Mock<DbSet<Key>>();
            _mockKeyDbSet.As<IQueryable<Key>>().Setup(m => m.Provider).Returns(_keys.AsQueryable().Provider);
            _mockKeyDbSet.As<IQueryable<Key>>().Setup(m => m.Expression).Returns(_keys.AsQueryable().Expression);
            _mockKeyDbSet.As<IQueryable<Key>>().Setup(m => m.ElementType).Returns(_keys.AsQueryable().ElementType);
            _mockKeyDbSet.As<IQueryable<Key>>().Setup(m => m.GetEnumerator()).Returns(_keys.AsQueryable().GetEnumerator());
            _mockKeyDbSet.Setup(m => m.Add(It.IsAny<Key>())).Callback<Key>((k) => _keys.Add(k));
            _mockKeyDbSet.Setup(m => m.Remove(It.IsAny<Key>())).Callback<Key>((k) => _keys.Remove(k));
            _mockKeyDbSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync((object[] ids) => 
            {
                int id = Convert.ToInt32(ids[0]);
                return _keys.FirstOrDefault(k => k.KeyId == id);
            });

            // Mock KeyHistoryDbSet
            _mockKeyHistoryDbSet = new Mock<DbSet<KeyHistory>>();
            _mockKeyHistoryDbSet.As<IQueryable<KeyHistory>>().Setup(m => m.Provider).Returns(_keyHistories.AsQueryable().Provider);
            _mockKeyHistoryDbSet.As<IQueryable<KeyHistory>>().Setup(m => m.Expression).Returns(_keyHistories.AsQueryable().Expression);
            _mockKeyHistoryDbSet.As<IQueryable<KeyHistory>>().Setup(m => m.ElementType).Returns(_keyHistories.AsQueryable().ElementType);
            _mockKeyHistoryDbSet.As<IQueryable<KeyHistory>>().Setup(m => m.GetEnumerator()).Returns(_keyHistories.AsQueryable().GetEnumerator());
            _mockKeyHistoryDbSet.Setup(m => m.Add(It.IsAny<KeyHistory>())).Callback<KeyHistory>((kh) => _keyHistories.Add(kh));
            _mockKeyHistoryDbSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<KeyHistory>>())).Callback<IEnumerable<KeyHistory>>((khs) => _keyHistories.AddRange(khs));
            _mockKeyHistoryDbSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<KeyHistory>>())).Callback<IEnumerable<KeyHistory>>((khs) => 
            {
                foreach (var kh in khs)
                {
                    _keyHistories.Remove(kh);
                }
            });
            
            // InMemory test veritabanı oluştur - her test için benzersiz bir veritabanı adı kullan
            var options = new DbContextOptionsBuilder<KeyManagementContext>()
                .UseInMemoryDatabase(databaseName: "TestKeyMgmtDb_" + Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new KeyManagementContext(options);
            
            // InMemory veritabanına test verilerini ekle
            _dbContext.ChangeTracker.Clear(); // Önceki tüm entity'leri temizle
            _dbContext.Keys.AddRange(_keys);
            _dbContext.KeyHistory.AddRange(_keyHistories);
            _dbContext.SaveChanges();

            // Mock Logger
            _mockLogger = new Mock<ILogger<HomeController>>();

            // Mock Configuration
            _mockConfiguration = new Mock<IConfiguration>();
            var emailSection = new Mock<IConfigurationSection>();
            emailSection.Setup(s => s["FromEmail"]).Returns("test@example.com");
            emailSection.Setup(s => s["ToEmail"]).Returns("admin@example.com");
            emailSection.Setup(s => s["SmtpServer"]).Returns("smtp.example.com");
            emailSection.Setup(s => s["Port"]).Returns("587");
            emailSection.Setup(s => s["Username"]).Returns("testuser");
            emailSection.Setup(s => s["Password"]).Returns("testpassword");
            _mockConfiguration.Setup(c => c.GetSection("EmailSettings")).Returns(emailSection.Object);

            // Mock User ve HttpContext
            _mockUser = new Mock<ClaimsPrincipal>();
            var identity = new Mock<ClaimsIdentity>();
            identity.Setup(i => i.Name).Returns("testuser");
            identity.Setup(i => i.IsAuthenticated).Returns(true);
            _mockUser.Setup(u => u.Identity).Returns(identity.Object);
            _mockUser.Setup(u => u.Claims).Returns(new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("Permissions", "DUC")
            });

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpContext.Setup(c => c.User).Returns(_mockUser.Object);

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
            
            // ITempDataDictionaryFactory servisi
            _mockTempDataDictionaryFactory = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
            _mockTempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>();
            _mockTempDataDictionaryFactory.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(_mockTempData.Object);
            mockServiceProvider.Setup(s => s.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)))
                .Returns(_mockTempDataDictionaryFactory.Object);
                
            _mockHttpContext.Setup(h => h.RequestServices).Returns(mockServiceProvider.Object);

            // Controller oluşturma - gerçek bir context kullanarak
            _controller = new HomeController(_dbContext, _mockLogger.Object, _mockConfiguration.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
        }

        [Test]
        public async Task Index_ForRegularUser_ReturnsViewWithKeys()
        {
            // Arrange - TestInitialize'da hazırlandı

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ViewData["UserPermissions"]);
            Assert.AreEqual("DUC", result.ViewData["UserPermissions"]);
            
            var model = result.Model as List<Key>;
            Assert.NotNull(model);
            Assert.AreEqual(2, model.Count);
        }

        [Test]
        public async Task UpdateKeyValue_WithValidId_UpdatesKeyAndRedirects()
        {
            // Arrange - TestInitialize'da hazırlandı
            int keyId = 1;
            var originalKey = _keys.FirstOrDefault(k => k.KeyId == keyId);
            var originalValue = originalKey?.KeyValue;

            // Act
            var result = await _controller.UpdateKeyValue(keyId) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            
            // Key değerinin değiştiğini kontrol et
            var updatedKey = _keys.FirstOrDefault(k => k.KeyId == keyId);
            Assert.NotNull(updatedKey);
            Assert.AreNotEqual(originalValue, updatedKey.KeyValue);
            
            // History kaydının eklendiğini kontrol et
            Assert.True(_keyHistories.Any(kh => kh.KeyId == keyId && kh.KeyValue == originalValue));
        }

        [Test]
        public async Task UpdateKeyUpdateDay_WithValidData_UpdatesKeyAndRedirects()
        {
            // Arrange
            int keyId = 1;
            int newUpdateDay = 15;
            var originalKey = _keys.FirstOrDefault(k => k.KeyId == keyId);
            var originalUpdateDay = originalKey?.KeyUpdateDay;

            // Act
            var result = await _controller.UpdateKeyUpdateDay(keyId, newUpdateDay) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            
            // Güncelleme gününün değiştiğini kontrol et
            var updatedKey = _keys.FirstOrDefault(k => k.KeyId == keyId);
            Assert.NotNull(updatedKey);
            Assert.AreEqual(newUpdateDay, updatedKey.KeyUpdateDay);
            
            // History kaydının eklendiğini kontrol et
            Assert.True(_keyHistories.Any(kh => kh.KeyId == keyId));
        }

        [Test]
        public async Task DeleteKey_WithValidId_DeletesKeyAndHistory()
        {
            // Arrange
            int keyId = 1;
            
            // History'de keyId=1 için kayıt olduğundan emin olalım
            _keyHistories.Add(new KeyHistory 
            { 
                HistoryId = 2, 
                KeyId = 1, 
                KeyValue = "TestHistoryValue", 
                ChangeDate = DateTime.Now.AddDays(-2),
                ChangedBy = "testuser"
            });

            // InMemory veritabanı kullanırken mock'lara gerek yok
            // KeyHistory kayıtlarının mevcut olduğundan emin olalım
            var initialHistoryCount = _dbContext.KeyHistory.Count(kh => kh.KeyId == keyId);
            Assert.GreaterOrEqual(initialHistoryCount, 1, "Test için en az bir KeyHistory kaydı bulunmalı");
            
            // Başlangıçta anahtarın varlığını kontrol et
            var initialKey = _dbContext.Keys.FirstOrDefault(k => k.KeyId == keyId);
            Assert.IsNotNull(initialKey, "Silinecek anahtar bulunamadı");

            // Not: Artık mock yerine gerçek bir InMemory database kullanıyoruz
            // Bu nedenle where sorgusu için bir ayarlama yapmamıza gerek yok

            // Act
            var result = await _controller.DeleteKey(keyId) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            
            // Key'in silindiğini kontrol et
            Assert.False(_keys.Any(k => k.KeyId == keyId));
        }

        [Test]
        public async Task CreateNewKey_WithValidData_CreatesNewKey()
        {
            // Arrange
            string keyType = "AES_KEY";
            int initialCount = _keys.Count;

            // Act
            var result = await _controller.CreateNewKey(keyType) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            
            // Yeni key'in oluşturulduğunu kontrol et
            Assert.AreEqual(initialCount + 1, _keys.Count);
            Assert.True(_keys.Any(k => k.KeyType == keyType));
        }

        [Test]
        public void GenerateAESKey_ReturnsValidBase64String()
        {
            // Bu metot private olduğu için reflection ile erişiyoruz
            var method = typeof(HomeController).GetMethod("GenerateAESKey", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            var result = method.Invoke(_controller, null) as string;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            
            // Base64 formatında olduğunu kontrol et
            bool isBase64 = false;
            try
            {
                Convert.FromBase64String(result);
                isBase64 = true;
            }
            catch { }
            
            Assert.True(isBase64, "Sonuç geçerli bir Base64 string olmalı");
        }

        [Test]
        public void GenerateDESKey_ReturnsValidBase64String()
        {
            // Bu metot private olduğu için reflection ile erişiyoruz
            var method = typeof(HomeController).GetMethod("GenerateDESKey", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            var result = method.Invoke(_controller, null) as string;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            
            // Base64 formatında olduğunu kontrol et
            bool isBase64 = false;
            try
            {
                Convert.FromBase64String(result);
                isBase64 = true;
            }
            catch { }
            
            Assert.True(isBase64, "Sonuç geçerli bir Base64 string olmalı");
        }
    }
}
