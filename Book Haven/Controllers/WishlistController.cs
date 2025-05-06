using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Book_Haven.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Only authenticated users can access this controller
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Add book to wishlist
        [HttpPost("add")]
        public async Task<IActionResult> AddToWishlist([FromBody] AddWishlistDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
            {
                return NotFound("Book not found");
            }

            var existing = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.BookId == dto.BookId);
            if (existing)
            {
                return BadRequest("Book already in wishlist");
            }

            var wishlistItem = new Wishlist
            {
                UserId = userId,
                BookId = dto.BookId
            };

            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok("Book added to wishlist");
        }

        // Remove book from wishlist
        [HttpDelete("remove/{bookId}")]
        public async Task<IActionResult> RemoveFromWishlist(long bookId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);
            if (wishlistItem == null)
            {
                return NotFound("Book not in wishlist");
            }

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok("Book removed from wishlist");
        }

        // Get user's wishlist
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var wishlist = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Book)
                .Select(w => new
                {
                    w.Book.Id,
                    w.Book.Title,
                    w.Book.Author,
                    w.Book.Price,
                    w.Book.ImagePath
                })
                .ToListAsync();

            return Ok(wishlist);
        }
    }

    // Updated DTO: removed UserId
    public class AddWishlistDto
    {
        public long BookId { get; set; }
    }
}
