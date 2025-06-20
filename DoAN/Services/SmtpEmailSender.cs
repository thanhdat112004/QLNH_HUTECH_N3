using DoAN.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace DoAN.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _opts;

        public SmtpEmailSender(IOptions<SmtpSettings> options)
        {
            _opts = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            using var client = new SmtpClient(_opts.Host, _opts.Port)
            {
                EnableSsl = _opts.EnableSsl,
                Credentials = new NetworkCredential(_opts.UserName, _opts.Password)
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(_opts.From),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            msg.To.Add(to);

            await client.SendMailAsync(msg);
        }
    }
}
