using System.Security.Cryptography;
using System.Text;

namespace KeyManagementWeb.EncryptConnectionString
{
    public static class StringCipher
    {
        private static readonly string _key = "KeyManagement2024!"; // Şifreleme anahtarı

        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                using (var aes = Aes.Create())
                {
                    var key = new byte[32]; // AES-256
                    Array.Copy(Encoding.UTF8.GetBytes(_key.PadRight(32)), key, 32);
                    aes.Key = key;

                    // Rastgele IV oluştur
                    aes.GenerateIV();
                    var iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        // IV'yi başa yaz
                        msEncrypt.Write(iv, 0, iv.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(textBytes, 0, textBytes.Length);
                            csEncrypt.FlushFinalBlock();
                        }

                        var result = msEncrypt.ToArray();
                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch
            {
                return text;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                using (var aes = Aes.Create())
                {
                    var key = new byte[32]; // AES-256
                    Array.Copy(Encoding.UTF8.GetBytes(_key.PadRight(32)), key, 32);
                    aes.Key = key;

                    // IV'yi oku (ilk 16 byte)
                    byte[] iv = new byte[16];
                    Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch
            {
                return encryptedText;
            }
        }
    }
}
