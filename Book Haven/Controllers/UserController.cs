using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Book_Haven.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Book_Haven;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;

        // Define the default profile picture path
        private const string DefaultProfilePicture = "/profile-pictures/default-profile.jpg";

        public UserController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("User ID not found in token.");
            }

            var userId = long.Parse(userIdClaim);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Book)
                .Select(o => new
                {
                    o.Id,
                    o.Book.Title,
                    o.Book.Author,
                    o.Book.Price,
                    o.Book.ImagePath,
                    o.DateAdded,
                    Status = o.IsPurchased ? "Purchased" : "Pending"
                })
                .ToListAsync();

            var wishlists = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Book)
                .Select(w => new
                {
                    w.Id,
                    w.Book.Title,
                    w.Book.Author,
                    w.Book.Price,
                    w.Book.ImagePath
                })
                .ToListAsync();

            // Use the default profile picture if the user's profile picture is null or empty
            var profilePicture = string.IsNullOrEmpty(user.ProfilePicture) ? DefaultProfilePicture : user.ProfilePicture;

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                ProfilePicture = profilePicture,
                Orders = orders,
                Wishlists = wishlists
            });
        }

        [HttpPut("update-username")]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("User ID not found in token.");
            }

            var userId = long.Parse(userIdClaim);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.UserName = dto.Username;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok("Username updated successfully.");
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("upload-profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile image)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("User ID not found in token.");
            }

            var userId = long.Parse(userIdClaim);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest("No image uploaded.");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "profile-pictures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{userId}_{Guid.NewGuid().ToString()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture != DefaultProfilePicture)
            {
                var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            user.ProfilePicture = $"/profile-pictures/{fileName}";
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { ProfilePicture = user.ProfilePicture });
            }

            return BadRequest(result.Errors);
        }
    }

    public class UpdateUsernameDto
    {
        public string Username { get; set; }
    }
}