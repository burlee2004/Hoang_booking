using System.Net;
using System.Net.Mail;

namespace Bookify.Web.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var mail = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"]!, emailSettings["SenderName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            using var smtp = new SmtpClient(emailSettings["SmtpServer"], int.Parse(emailSettings["Port"]!))
            {
                Credentials = new NetworkCredential(emailSettings["SenderEmail"], emailSettings["SenderPassword"]),
                EnableSsl = true
            };
            await smtp.SendMailAsync(mail);
        }
    }
}