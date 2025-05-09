using System.Threading.Tasks;

namespace Book_Haven.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}