using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;

namespace KeyManagementWeb.Controllers
{
    public class DecryptionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GenerateTestData()
        {
            return Json(new { result = EncryptText("merhaba dunya") });
        }

        [HttpPost]
        public IActionResult Decrypt([FromBody] DecryptionRequest request)
        {
            try
            {
                string decryptedText = "";

                if (request.KeyType == "AES")
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] keyBytes = Convert.FromBase64String(request.Key);
                        byte[] ivBytes = Convert.FromBase64String(request.IV);

                        // AES-256 için key boyutu 32 byte olmalı
                        if (keyBytes.Length != 32)
                        {
                            throw new Exception("AES-256 için key boyutu 32 byte olmalıdır.");
                        }

                        // IV boyutu 16 byte olmalı
                        if (ivBytes.Length != 16)
                        {
                            throw new Exception("IV boyutu 16 byte olmalıdır.");
                        }

                        aes.Key = keyBytes;
                        aes.IV = ivBytes;

                        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                        using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(request.EncryptedText)))
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            decryptedText = srDecrypt.ReadToEnd();
                        }
                    }
                }
                else if (request.KeyType == "DES")
                {
                    using (DES des = DES.Create())
                    {
                        byte[] keyBytes = Convert.FromBase64String(request.Key);
                        byte[] ivBytes = Convert.FromBase64String(request.IV);

                        // DES için key boyutu 8 byte olmalı
                        if (keyBytes.Length != 8)
                        {
                            throw new Exception("DES için key boyutu 8 byte olmalıdır.");
                        }

                        // IV boyutu 8 byte olmalı
                        if (ivBytes.Length != 8)
                        {
                            throw new Exception("IV boyutu 8 byte olmalıdır.");
                        }

                        des.Key = keyBytes;
                        des.IV = ivBytes;

                        ICryptoTransform decryptor = des.CreateDecryptor(des.Key, des.IV);

                        using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(request.EncryptedText)))
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            decryptedText = srDecrypt.ReadToEnd();
                        }
                    }
                }
                else if (request.KeyType == "Kazakistan")
                {
                    // Kazakistan Decrypt algoritması (TripleDES kullanıyor)
                    string key = !string.IsNullOrEmpty(request.Key) ? request.Key : "abcd.1234";
                    decryptedText = Decrypt(request.EncryptedText, key);
                }

                return Json(new { success = true, result = decryptedText });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Kazakistan Connection String için Decrypt metodu
        public string Decrypt(string encodedText, string key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = Convert.FromBase64String(encodedText);
            string plaintext = Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return plaintext;
        }

        // Test amaçlı şifreleme metodu
        private string EncryptText(string plainText, string keyType = "AES")
        {
            if (keyType == "AES")
            {
                using (Aes aes = Aes.Create())
                {
                    // Key ve IV oluştur
                    string key = Convert.ToBase64String(aes.Key); // 32 byte
                    string iv = Convert.ToBase64String(aes.IV);   // 16 byte

                    byte[] encrypted;
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }

                    return $"Key: {key}\nIV: {iv}\nEncrypted: {Convert.ToBase64String(encrypted)}";
                }
            }
            else if (keyType == "TripleDES")
            {
                // TripleDES için test verisi oluştur
                string key = "abcd.1234"; // örnek anahtar

                using (TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider())
                using (MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider())
                {
                    byte[] byteHash;
                    byte[] byteBuff;

                    byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
                    desCryptoProvider.Key = byteHash;
                    desCryptoProvider.Mode = CipherMode.ECB; // ECB modu kullanılıyor
                    byteBuff = Encoding.UTF8.GetBytes(plainText);

                    string encrypted = Convert.ToBase64String(
                        desCryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));

                    return $"Key: {key}\nEncrypted: {encrypted}";
                }
            }
            else if (keyType == "SHA256")
            {
                // SHA-256 hash oluştur
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] degerBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] shaBytes = sha.ComputeHash(degerBytes);
                    string hashed = Convert.ToBase64String(shaBytes);

                    return $"Original: {plainText}\nHashed: {hashed}";
                }
            }
            return string.Empty;
        }

        // SHA-256 hash için yardımcı metod
        private static string HashToByte(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            foreach (byte b in hash)
            {
                result.Append(b.ToString("X2"));
            }
            return result.ToString();
        }
    }

    public class DecryptionRequest
    {
        public string KeyType { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }
        public string EncryptedText { get; set; }
    }
}
