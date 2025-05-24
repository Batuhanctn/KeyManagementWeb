
using KeyManagementWeb.Helpers;
using System;

namespace EncryptConnectionString
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var connectionString = "Server=localhost;Database=KeyManagementDB;Trusted_Connection=True;TrustServerCertificate=True;";
            var encrypted = StringCipher.Encrypt(connectionString);
            Console.WriteLine("Şifrelenmiş Connection String:");
            Console.WriteLine(encrypted);
        }
    }
}
