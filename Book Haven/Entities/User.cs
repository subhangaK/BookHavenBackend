using Microsoft.AspNetCore.Identity;

namespace Book_Haven.Entities
{
    public class User : IdentityUser<long>
    {
        public string? ProfilePicture { get; set; }
    }
}