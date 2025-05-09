using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Book_Haven.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(toEmail))
                throw new ArgumentException("Recipient email cannot be empty.", nameof(toEmail));

            _logger.LogDebug("Attempting to send email to {ToEmail} with subject {Subject}", toEmail, subject);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Book Haven", _configuration["Email:SmtpUsername"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(_configuration["Email:SmtpServer"], int.Parse(_configuration["Email:SmtpPort"]), MailKit.Security.SecureSocketOptions.SslOnConnect);
                    _logger.LogDebug("Connected to SMTP server {SmtpServer} on port {SmtpPort}", _configuration["Email:SmtpServer"], _configuration["Email:SmtpPort"]);

                    client.Authenticate(_configuration["Email:SmtpUsername"], _configuration["Email:SmtpPassword"]);
                    _logger.LogDebug("Authenticated with SMTP server");

                    await client.SendAsync(message);
                    _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {ToEmail}. Error: {ErrorMessage}", toEmail, ex.Message);
                    throw; // Re-throw to be caught by the controller
                }
                finally
                {
                    await client.DisconnectAsync(true);
                    _logger.LogDebug("Disconnected from SMTP server");
                }
            }
        }
    }
}