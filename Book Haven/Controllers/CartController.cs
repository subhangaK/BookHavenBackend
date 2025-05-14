using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Book_Haven.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            if (dto.Quantity < 1)
            {
                return BadRequest("Quantity must be at least 1.");
            }

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
            {
                return NotFound("Book not found");
            }

            var existing = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == dto.BookId && !c.IsRemoved);
            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
                await _context.SaveChangesAsync();
                return Ok("Quantity updated in cart");
            }

            var cartItem = new Cart
            {
                UserId = userId,
                BookId = dto.BookId,
                Quantity = dto.Quantity,
                IsRemoved = false
            };

            _context.Carts.Add(cartItem);
            await _context.SaveChangesAsync();

            return Ok("Book added to cart");
        }

        [HttpPatch("update-quantity/{bookId}")]
        public async Task<IActionResult> UpdateQuantity(long bookId, [FromBody] UpdateQuantityDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            if (dto.Quantity < 1)
            {
                return BadRequest("Quantity must be at least 1.");
            }

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId && !c.IsRemoved);
            if (cartItem == null)
            {
                return NotFound("Book not in cart");
            }

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok("Quantity updated");
        }

        [HttpPatch("remove/{bookId}")]
        public async Task<IActionResult> RemoveFromCart(long bookId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId && !c.IsRemoved);
            if (cartItem == null)
            {
                return NotFound("Book not in cart");
            }

            cartItem.IsRemoved = true;
            await _context.SaveChangesAsync();

            return Ok("Book marked as removed from cart");
        }

        [HttpPatch("restore/{bookId}")]
        public async Task<IActionResult> RestoreToCart(long bookId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId && c.IsRemoved);
            if (cartItem == null)
            {
                return NotFound("Removed book not found in cart");
            }

            cartItem.IsRemoved = false;
            await _context.SaveChangesAsync();

            return Ok("Book restored to cart");
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var cart = await _context.Carts
                .Where(c => c.UserId == userId && !c.IsRemoved)
                .Include(c => c.Book)
                .Select(c => new
                {
                    c.Book.Id,
                    c.Book.Title,
                    c.Book.Author,
                    c.Book.Price,
                    c.Book.ImagePath,
                    c.Quantity,
                    c.Book.IsOnSale, // Added
                    DiscountPercentage = c.Book.DiscountPercentage // Added
                })
                .ToListAsync();

            return Ok(cart);
        }
    }

    public class AddCartDto
    {
        public long BookId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityDto
    {
        public int Quantity { get; set; }
    }
}