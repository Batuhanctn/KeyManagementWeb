using KeyManagementWeb.Helpers;

namespace KeyManagementWeb.Tools
{
    public class ConnectionStringEncryptor
    {
        public static void Main(string[] args)
        {
            var connectionString = "Server=localhost;Database=KeyManagementDB;Trusted_Connection=True;TrustServerCertificate=True;";
            var encrypted = StringCipher.Encrypt(connectionString);
            Console.WriteLine("Şifrelenmiş Connection String:");
            Console.WriteLine(encrypted);

            Console.WriteLine("\nBu şifrelenmiş metni appsettings.json dosyanızdaki ConnectionStrings:KeyManagementDB değeri ile değiştirin.");
            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}
