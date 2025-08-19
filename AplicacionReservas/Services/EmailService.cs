using AplicacionReservas.Helpers;
using AplicacionReservas.Interfaces;
using System.Net.Mail;
using System.Net;

namespace AplicacionReservas.Services
{
    public class EmailService : IEmailServices
    {
        private readonly EmailSettings _settings;
        public EmailService(IConfiguration configuration)
        {
            _settings = configuration.GetSection("EmailSettings").Get<EmailSettings>()!;
        }

        public async Task EnviarCorreoAsync(string toEmail, string subject, string body)
        {
            var mensaje = new MailMessage();
            mensaje.From = new MailAddress(_settings.SenderEmail, _settings.SenderName);
            mensaje.To.Add(toEmail);
            mensaje.Subject = subject;
            mensaje.Body = body;
            mensaje.IsBodyHtml = true;

            using (var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort))
            {
                client.Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword);
                client.EnableSsl = true;
                await client.SendMailAsync(mensaje);
            }
        }
    }
}
