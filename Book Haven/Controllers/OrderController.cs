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

        // Add book to order with discount calculation
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

            // Count total orders before adding the new one
            var orderCountBefore = await _context.Orders
                .CountAsync(o => o.UserId == userId); // Total successful orders before this one

            var orderItem = new Order
            {
                UserId = userId,
                BookId = dto.BookId
            };

            _context.Orders.Add(orderItem);

            // Calculate discounts based on total order count
            decimal discountPercentage = 0m;
            int orderPosition = orderCountBefore + 1; // Position of the current order being added

            // Determine discount based on 5-order intervals starting from the 6th order
            if (orderPosition >= 6) // Discounts start from the 6th order
            {
                int cycleIndex = (orderPosition - 6) / 5; // Integer division to determine the cycle (0 for 6-10, 1 for 11-15, etc.)
                int positionInCycle = (orderPosition - 6) % 5; // 0 to 4 within each 5-order block

                // Apply discount only at the start of each 5-order cycle
                if (positionInCycle == 0) // 6th, 11th, 16th, 21st, etc.
                {
                    discountPercentage = (cycleIndex % 2 == 0) ? 0.05m : 0.10m; // 5% for even cycles (6, 16, 26), 10% for odd cycles (11, 21, 31)
                }
            }

            orderItem.DiscountPercentage = discountPercentage; // Store discount for record-keeping
            await _context.SaveChangesAsync();

            // Recalculate counts after adding the order for the response
            var orderCountAfter = await _context.Orders
                .CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            return Ok(new
            {
                message = "Book added to order",
                discountPercentage = discountPercentage * 100, // Return as percentage
                totalOrders = orderCountAfter,
                currentOrderBookCount
            });
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

        // Get user's order with discount
        [HttpGet("with-discount")]
        public async Task<IActionResult> GetOrderWithDiscount()
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
                    o.Book.ImagePath,
                    o.DiscountPercentage
                })
                .ToListAsync();

            var orderCount = await _context.Orders
                .CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            // Calculate the discount percentage for the next order
            decimal discountPercentage = 0m;
            int nextOrderPosition = orderCount + 1;
            if (nextOrderPosition >= 6)
            {
                int cycleIndex = (nextOrderPosition - 6) / 5; // Integer division to determine the cycle
                int positionInCycle = (nextOrderPosition - 6) % 5; // 0 to 4 within each 5-order block

                // Apply discount only at the start of each 5-order cycle
                if (positionInCycle == 0) // 6th, 11th, 16th, 21st, etc.
                {
                    discountPercentage = (cycleIndex % 2 == 0) ? 0.05m : 0.10m; // 5% for even cycles (6, 16, 26), 10% for odd cycles (11, 21, 31)
                }
            }

            return Ok(new
            {
                orders = order,
                discountPercentage = discountPercentage * 100, // Return as percentage
                totalOrders = orderCount,
                currentOrderBookCount
            });
        }

        // Get user's order (original endpoint)
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