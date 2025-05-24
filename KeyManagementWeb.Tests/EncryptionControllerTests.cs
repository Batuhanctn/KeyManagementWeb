using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using KeyManagementWeb.Controllers;
using System;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KeyManagementWeb.Tests
{
    [TestFixture]
    public class EncryptionControllerTests
    {
        private EncryptionController _controller;

        [SetUp]
        public void TestInitialize()
        {
            _controller = new EncryptionController();
        }

        [Test]
        public void Encrypt_AES_ValidInput_ReturnsEncryptedData()
        {
            // Arrange
            var request = new EncryptionRequest
            {
                PlainText = "Test metin",
                KeyType = "AES"
            };

            // Act
            var result = _controller.Encrypt(request) as JsonResult;
            var json = JsonConvert.SerializeObject(result.Value);
            var data = JObject.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.True(data.Value<bool>("success"));
            Assert.NotNull(data.Value<string>("result"));
            Assert.NotNull(data.Value<string>("key"));
            Assert.NotNull(data.Value<string>("iv"));

            // Key ve IV doğru boyutlarda mı kontrol et
            byte[] keyBytes = Convert.FromBase64String(data.Value<string>("key"));
            byte[] ivBytes = Convert.FromBase64String(data.Value<string>("iv"));
            Assert.AreEqual(32, keyBytes.Length, "AES-256 anahtarı 32 byte olmalıdır");
            Assert.AreEqual(16, ivBytes.Length, "IV 16 byte olmalıdır");
        }

        [Test]
        public void Encrypt_AES_WithProvidedKey_ReturnsCorrectEncryptedData()
        {
            // Arrange
            // Test için sabit anahtar ve IV oluştur
            using (Aes aes = Aes.Create())
            {
                string keyBase64 = Convert.ToBase64String(aes.Key);
                string ivBase64 = Convert.ToBase64String(aes.IV);

                var request = new EncryptionRequest
                {
                    PlainText = "Test metin",
                    KeyType = "AES",
                    Key = keyBase64,
                    IV = ivBase64
                };

                // Act
                var result = _controller.Encrypt(request) as JsonResult;
                var json = JsonConvert.SerializeObject(result.Value);
                var data = JObject.Parse(json);

                // Assert
                Assert.NotNull(result);
                Assert.True(data.Value<bool>("success"));
                Assert.NotNull(data.Value<string>("result"));
                Assert.AreEqual(keyBase64, data.Value<string>("key"), "Key değeri değişmemeli");
                Assert.AreEqual(ivBase64, data.Value<string>("iv"), "IV değeri değişmemeli");
            }
        }

        [Test]
        public void Encrypt_DES_ValidInput_ReturnsEncryptedData()
        {
            // Arrange
            var request = new EncryptionRequest
            {
                PlainText = "Test metin",
                KeyType = "DES"
            };

            // Act
            var result = _controller.Encrypt(request) as JsonResult;
            var json = JsonConvert.SerializeObject(result.Value);
            var data = JObject.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.True(data.Value<bool>("success"));
            Assert.NotNull(data.Value<string>("result"));
            Assert.NotNull(data.Value<string>("key"));
            Assert.NotNull(data.Value<string>("iv"));

            // Key ve IV doğru boyutlarda mı kontrol et
            byte[] keyBytes = Convert.FromBase64String(data.Value<string>("key"));
            byte[] ivBytes = Convert.FromBase64String(data.Value<string>("iv"));
            Assert.AreEqual(8, keyBytes.Length, "DES anahtarı 8 byte olmalıdır");
            Assert.AreEqual(8, ivBytes.Length, "IV 8 byte olmalıdır");
        }

        [Test]
        public void Encrypt_Kazakistan_ValidInput_ReturnsEncryptedData()
        {
            // Arrange
            var request = new EncryptionRequest
            {
                PlainText = "Test metin",
                KeyType = "Kazakistan"
            };

            // Act
            var result = _controller.Encrypt(request) as JsonResult;
            var json = JsonConvert.SerializeObject(result.Value);
            var data = JObject.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.True(data.Value<bool>("success"));
            Assert.NotNull(data.Value<string>("result"));
            Assert.NotNull(data.Value<string>("key"));
            Assert.AreEqual("abcd.1234", data.Value<string>("key"), "Varsayılan anahtar kullanılmalı");
        }

        [Test]
        public void Encrypt_KazakistanBank_ValidInput_ReturnsHashedText()
        {
            // Arrange
            var request = new EncryptionRequest
            {
                PlainText = "Test metin",
                KeyType = "KazakistanBank"
            };

            // Act
            var result = _controller.Encrypt(request) as JsonResult;
            var json = JsonConvert.SerializeObject(result.Value);
            var data = JObject.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.True(data.Value<bool>("success"));
            Assert.NotNull(data.Value<string>("result"));
            // SHA-256 hash'in doğru formatta olduğunu kontrol et
            Assert.AreEqual(64, data.Value<string>("result").Length, "SHA-256 hash 64 karakter uzunluğunda olmalıdır");
        }

        [Test]
        public void Encrypt_InvalidKeyType_ReturnsError()
        {
            // Arrange
            var request = new EncryptionRequest
            {
                PlainText = "Test metin",
                KeyType = "InvalidType"
            };

            // Act
            var result = _controller.Encrypt(request) as JsonResult;
            var json = JsonConvert.SerializeObject(result.Value);
            var data = JObject.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.False(data.Value<bool>("success"));
            Assert.AreEqual("Geçersiz şifreleme tipi.", data.Value<string>("error"));
        }

        [Test]
        public void Encrypt_AES_InvalidKeyLength_ThrowsException()
        {
            // Arrange
            // 32 byte'dan kısa bir anahtar oluştur
            byte[] shortKey = new byte[16]; // 16 byte
            new Random().NextBytes(shortKey);
            string keyBase64 = Convert.ToBase64String(shortKey);

            var request = new EncryptionRequest
            {
                PlainText = "Test metin",
                KeyType = "AES",
                Key = keyBase64
            };

            // Act & Assert
            try
            {
                var result = _controller.Encrypt(request) as JsonResult;
                var json = JsonConvert.SerializeObject(result.Value);
                var data = JObject.Parse(json);
                
                // Controller bir JsonResult dönerse, hata mesajının doğru olduğunu kontrol et
                Assert.False(data.Value<bool>("success"), "AES için geçersiz anahtar uzunluğunda işlem başarılı olmamalı");
                Assert.True(data.Value<string>("error").Contains("AES-256 için key boyutu"), "Hata mesajı key boyutuyla ilgili olmalı");
            }
            catch (Exception ex)
            {
                // Exception fırlatılırsa, doğru exception olduğunu kontrol et
                Assert.True(ex.Message.Contains("AES-256 için key boyutu") || ex.Message.Contains("key boyutu"), "Beklenen hata mesajı alınmadı");
            }
        }

        [Test]
        public void Encrypt_Method_WithValidInput_ReturnsEncryptedString()
        {
            // Arrange
            string source = "Test metin";
            string key = "TestKey123";

            // Act
            string result = _controller.Encrypt(source, key);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            // Şifrelenmiş veri Base64 formatında olmalı
            try
            {
                Convert.FromBase64String(result);
                // Başarılı dönüşüm, istisna fırlatılmadı
                Assert.True(true);
            }
            catch
            {
                Assert.Fail("Şifrelenmiş metin Base64 formatında değil.");
            }
        }
    }
}
