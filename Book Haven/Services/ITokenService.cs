using Book_Haven.Entities;
using System.Collections.Generic;

namespace Book_Haven.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user, IList<string> roles);
    }
}