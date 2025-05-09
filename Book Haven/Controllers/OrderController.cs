using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Book_Haven.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using Book_Haven.Services;
using Microsoft.Extensions.Logging;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Only authenticated users can access this controller
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, IEmailService emailService, ILogger<OrderController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Add book to order with discount calculation
        [HttpPost("add")]
        public async Task<IActionResult> AddToOrder([FromBody] AddOrderDto dto)
        {
            if (dto == null || dto.BookId <= 0)
            {
                _logger.LogWarning("Invalid input for AddToOrder: BookId is null or invalid");
                return BadRequest("Invalid book ID");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID");
                return Unauthorized("User not authenticated or invalid token.");
            }

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
            {
                _logger.LogWarning("Book not found for ID: {BookId}", dto.BookId);
                return NotFound("Book not found");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound("User not found");
            }

            var existing = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.BookId == dto.BookId);
            if (existing)
            {
                _logger.LogWarning("Book {BookId} already in order for user {UserId}", dto.BookId, userId);
                return BadRequest("Book already in order");
            }

            var orderCountBefore = await _context.Orders.CountAsync(o => o.UserId == userId);
            var claimCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            _logger.LogInformation("Generated ClaimCode: {ClaimCode}", claimCode);

            var orderItem = new Order
            {
                UserId = userId,
                BookId = dto.BookId,
                DateAdded = DateTime.UtcNow,
                ClaimCode = claimCode,
                DiscountPercentage = 0m
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal discountPercentage = 0m;
                int orderPosition = orderCountBefore + 1;
                if (orderPosition >= 6)
                {
                    int cycleIndex = (orderPosition - 6) / 5;
                    int positionInCycle = (orderPosition - 6) % 5;
                    if (positionInCycle == 0)
                    {
                        discountPercentage = (cycleIndex % 2 == 0) ? 0.05m : 0.10m;
                    }
                }
                orderItem.DiscountPercentage = discountPercentage;

                _context.Orders.Add(orderItem);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Order created with ID: {OrderId}", orderItem.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to save order for user {UserId}, book {BookId}", userId, dto.BookId);
                return StatusCode(500, "An error occurred while processing your order.");
            }

            var orderCountAfter = await _context.Orders.CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            var billDetails = $@"<h2>Order Confirmation - Book Haven</h2>
                <p><strong>Claim Code:</strong> {claimCode}</p>
                <p><strong>Book:</strong> {book.Title}</p>
                <p><strong>Author:</strong> {book.Author}</p>
                <p><strong>Price:</strong> ${book.Price:F2}</p>
                <p><strong>Discount:</strong> {orderItem.DiscountPercentage * 100:F0}%</p>
                <p><strong>Final Price:</strong> ${(book.Price * (1 - orderItem.DiscountPercentage)):F2}</p>
                <p><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
                <p>Thank you for shopping with Book Haven!</p>";

            bool emailSent = false;
            try
            {
                await _emailService.SendEmailAsync(user.Email, "Your Book Haven Order Confirmation", billDetails);
                emailSent = true;
                _logger.LogInformation("Email sent successfully to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. SMTP error: {ErrorMessage}", user.Email, ex.Message);
                // Continue with success response even if email fails, as order is saved
            }

            return Ok(new
            {
                message = emailSent
                    ? "Book added to order successfully. Confirmation email sent."
                    : "Book added to order successfully, but failed to send confirmation email.",
                discountPercentage = orderItem.DiscountPercentage * 100,
                totalOrders = orderCountAfter,
                currentOrderBookCount
            });
        }

        // Remove book from order
        [HttpDelete("remove/{bookId}")]
        public async Task<IActionResult> RemoveFromOrder(long bookId)
        {
            if (bookId <= 0)
            {
                _logger.LogWarning("Invalid book ID: {BookId}", bookId);
                return BadRequest("Invalid book ID");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID");
                return Unauthorized("User not authenticated or invalid token.");
            }

            var orderItem = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.BookId == bookId);
            if (orderItem == null)
            {
                _logger.LogWarning("Book {BookId} not in order for user {UserId}", bookId, userId);
                return NotFound("Book not in order");
            }

            try
            {
                _context.Orders.Remove(orderItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Book {BookId} removed from order for user {UserId}", bookId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove book {BookId} from order for user {UserId}", bookId, userId);
                return StatusCode(500, "An error occurred while removing the book from your order.");
            }

            return Ok(new { message = "Book removed from order successfully" });
        }

        // Get user's order with discount
        [HttpGet("with-discount")]
        public async Task<IActionResult> GetOrderWithDiscount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID");
                return Unauthorized("User not authenticated or invalid token.");
            }

            var orders = await _context.Orders
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

            var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            decimal discountPercentage = 0m;
            int nextOrderPosition = orderCount + 1;
            if (nextOrderPosition >= 6)
            {
                int cycleIndex = (nextOrderPosition - 6) / 5;
                int positionInCycle = (nextOrderPosition - 6) % 5;
                if (positionInCycle == 0)
                {
                    discountPercentage = (cycleIndex % 2 == 0) ? 0.05m : 0.10m;
                }
            }

            return Ok(new
            {
                orders,
                discountPercentage = discountPercentage * 100,
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
                _logger.LogWarning("Unauthorized: Invalid or missing user ID");
                return Unauthorized("User not authenticated or invalid token.");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Book)
                .Select(o => new
                {
                    o.Book.Id,
                    o.Book.Title,
                    o.Book.Author,
                    o.Book.Price,
                    o.Book.ImagePath,
                    o.ClaimCode, // Include ClaimCode for order details
                    o.DateAdded // Include DateAdded for order details
                })
                .ToListAsync();

            return Ok(orders);
        }
    }

    public class AddOrderDto
    {
        public long BookId { get; set; }
    }
}