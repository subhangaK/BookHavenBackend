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
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Add book to order
        [HttpPost("add")]
        public async Task<IActionResult> AddToOrder([FromBody] AddOrderDto dto)
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

            var existing = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.BookId == dto.BookId);
            if (existing)
            {
                return BadRequest("Book already in order");
            }

            var orderItem = new Order
            {
                UserId = userId,
                BookId = dto.BookId
            };

            _context.Orders.Add(orderItem);
            await _context.SaveChangesAsync();

            return Ok("Book added to order");
        }

        // Remove book from order
        [HttpDelete("remove/{bookId}")]
        public async Task<IActionResult> RemoveFromOrder(long bookId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var orderItem = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.BookId == bookId);
            if (orderItem == null)
            {
                return NotFound("Book not in order");
            }

            _context.Orders.Remove(orderItem);
            await _context.SaveChangesAsync();

            return Ok("Book removed from order");
        }

        // Get user's order
        [HttpGet]
        public async Task<IActionResult> GetOrder()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User not authenticated or invalid token.");
            }

            var order = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Book)
                .Select(o => new
                {
                    o.Book.Id,
                    o.Book.Title,
                    o.Book.Author,
                    o.Book.Price,
                    o.Book.ImagePath
                })
                .ToListAsync();

            return Ok(order);
        }
    }

    public class AddOrderDto
    {
        public long BookId { get; set; }
    }
}