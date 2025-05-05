using System.Security.Claims;
using Book_Haven.Entities;

namespace Book_Haven.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}