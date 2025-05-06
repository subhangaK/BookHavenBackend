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
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Add book to cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartDto dto)
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

            var existing = await _context.Carts
                .AnyAsync(c => c.UserId == userId && c.BookId == dto.BookId);
            if (existing)
            {
                return BadRequest("Book already in cart");
            }

            var cartItem = new Cart
            {
                UserId = userId,
                BookId = dto.BookId
            };

            _context.Carts.Add(cartItem);
            await _context.SaveChangesAsync();

            return Ok("Book added to cart");
        }

        // Remove book from cart
        [HttpDelete("remove/{bookId}")]
        public async Task<IActionResult> RemoveFromCart(long bookId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);
            if (cartItem == null)
            {
                return NotFound("Book not in cart");
            }

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok("Book removed from cart");
        }

        // Get user's cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var cart = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Book)
                .Select(c => new
                {
                    c.Book.Id,
                    c.Book.Title,
                    c.Book.Author,
                    c.Book.Price,
                    c.Book.ImagePath
                })
                .ToListAsync();

            return Ok(cart);
        }
    }

    public class AddCartDto
    {
        public long BookId { get; set; }
    }
}