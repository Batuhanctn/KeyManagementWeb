using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace KeyManagementWeb.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendKeyUpdateNotificationAsync(string username, int keyId, DateTime updateDate)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Key Management System", _configuration["EmailSettings:FromEmail"]));
            email.To.Add(new MailboxAddress("Admin", _configuration["EmailSettings:ToEmail"]));
            email.Subject = "Key Güncelleme Bildirimi";

            var builder = new BodyBuilder();
            builder.TextBody = $@"Bir key güncelleme işlemi gerçekleşti:

Kullanıcı: {username}
Key ID: {keyId}
Güncelleme Tarihi: {updateDate:dd.MM.yyyy HH:mm:ss}";

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
