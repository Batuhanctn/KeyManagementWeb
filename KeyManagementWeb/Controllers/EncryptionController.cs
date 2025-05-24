using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace KeyManagementWeb.Controllers
{
    public class EncryptionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Encrypt([FromBody] EncryptionRequest request)
        {
            try
            {
                string encryptedText = "";
                string keyBase64 = "";
                string ivBase64 = "";
                string hashedText = "";

                if (request.KeyType == "AES")
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] keyBytes;
                        byte[] ivBytes;

                        // Key veya IV değerleri verilmişse, onları kullan; değilse yeni oluştur
                        if (!string.IsNullOrEmpty(request.Key))
                        {
                            keyBytes = Convert.FromBase64String(request.Key);
                            // AES-256 için key boyutu 32 byte olmalı
                            if (keyBytes.Length != 32)
                            {
                                throw new Exception("AES-256 için key boyutu 32 byte olmalıdır.");
                            }
                        }
                        else
                        {
                            keyBytes = aes.Key; // Otomatik olarak 32 byte key oluştur
                        }

                        if (!string.IsNullOrEmpty(request.IV))
                        {
                            ivBytes = Convert.FromBase64String(request.IV);
                            // IV boyutu 16 byte olmalı
                            if (ivBytes.Length != 16)
                            {
                                throw new Exception("IV boyutu 16 byte olmalıdır.");
                            }
                        }
                        else
                        {
                            ivBytes = aes.IV; // Otomatik olarak 16 byte IV oluştur
                        }

                        keyBase64 = Convert.ToBase64String(keyBytes);
                        ivBase64 = Convert.ToBase64String(ivBytes);

                        aes.Key = keyBytes;
                        aes.IV = ivBytes;

                        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                        using (MemoryStream msEncrypt = new MemoryStream())
                        {
                            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(request.PlainText);
                            }
                            encryptedText = Convert.ToBase64String(msEncrypt.ToArray());
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        result = encryptedText,
                        key = keyBase64,
                        iv = ivBase64
                    });
                }
                else if (request.KeyType == "DES")
                {
                    using (DES des = DES.Create())
                    {
                        byte[] keyBytes;
                        byte[] ivBytes;

                        // Key veya IV değerleri verilmişse, onları kullan; değilse yeni oluştur
                        if (!string.IsNullOrEmpty(request.Key))
                        {
                            keyBytes = Convert.FromBase64String(request.Key);
                            // DES için key boyutu 8 byte olmalı
                            if (keyBytes.Length != 8)
                            {
                                throw new Exception("DES için key boyutu 8 byte olmalıdır.");
                            }
                        }
                        else
                        {
                            keyBytes = des.Key; // Otomatik olarak 8 byte key oluştur
                        }

                        if (!string.IsNullOrEmpty(request.IV))
                        {
                            ivBytes = Convert.FromBase64String(request.IV);
                            // IV boyutu 8 byte olmalı
                            if (ivBytes.Length != 8)
                            {
                                throw new Exception("IV boyutu 8 byte olmalıdır.");
                            }
                        }
                        else
                        {
                            ivBytes = des.IV; // Otomatik olarak 8 byte IV oluştur
                        }

                        keyBase64 = Convert.ToBase64String(keyBytes);
                        ivBase64 = Convert.ToBase64String(ivBytes);

                        des.Key = keyBytes;
                        des.IV = ivBytes;

                        ICryptoTransform encryptor = des.CreateEncryptor(des.Key, des.IV);

                        using (MemoryStream msEncrypt = new MemoryStream())
                        {
                            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(request.PlainText);
                            }
                            encryptedText = Convert.ToBase64String(msEncrypt.ToArray());
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        result = encryptedText,
                        key = keyBase64,
                        iv = ivBase64
                    });
                }
                else if (request.KeyType == "Kazakistan")
                {
                    // Kazakistan Connection String için Encrypt metodu
                    string key = !string.IsNullOrEmpty(request.Key) ? request.Key : "abcd.1234";
                    encryptedText = Encrypt(request.PlainText, key);

                    return Json(new
                    {
                        success = true,
                        result = encryptedText,
                        key = key
                    });
                }
                else if (request.KeyType == "KazakistanBank")
                {
                    // Kazakistan Bank Password için SHA-256 Hash
                    hashedText = SHA_256_Encrypting(request.PlainText);

                    return Json(new
                    {
                        success = true,
                        result = hashedText
                    });
                }

                return Json(new { success = false, error = "Geçersiz şifreleme tipi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Kazakistan Connection String için Encrypt metodu
        public string Encrypt(string source, string key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = Encoding.UTF8.GetBytes(source);

            string encoded = Convert.ToBase64String(desCryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return encoded;
        }

        // Kazakistan Bank Password hash
        public static string SHA_256_Encrypting(string deger)
        {
            SHA256 sha = SHA256.Create();
            byte[] degerBytes = Encoding.UTF8.GetBytes(deger);
            byte[] shaBytes = sha.ComputeHash(degerBytes);
            return HashToByte(shaBytes);
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

    public class EncryptionRequest
    {
        public string KeyType { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }
        public string PlainText { get; set; }
    }
}
