using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Book_Haven.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using Book_Haven.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        [HttpPost("add")]
        public async Task<IActionResult> AddToOrder([FromBody] AddOrderDto dto)
        {
            if (dto == null || dto.BookId <= 0)
            {
                _logger.LogWarning("Invalid input for AddToOrder: BookId is null or invalid");
                return BadRequest(new { message = "Invalid book ID" });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
            }

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
            {
                _logger.LogWarning("Book not found for ID: {BookId}", dto.BookId);
                return NotFound(new { message = "Book not found" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            var existing = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.BookId == dto.BookId);
            if (existing)
            {
                _logger.LogWarning("Book {BookId} already in order for user {UserId}", dto.BookId, userId);
                return BadRequest(new { message = "Book already in order" });
            }

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == dto.BookId && !c.IsRemoved);
            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found for user {UserId} and book {BookId}", userId, dto.BookId);
                return BadRequest(new { message = "Book not found in cart" });
            }

            var orderCountBefore = await _context.Orders.CountAsync(o => o.UserId == userId);
            var claimCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            _logger.LogInformation("Generated ClaimCode: {ClaimCode} for user {UserId}", claimCode, userId);

            var orderItem = new Order
            {
                UserId = userId,
                BookId = dto.BookId,
                DateAdded = DateTime.UtcNow,
                ClaimCode = claimCode,
                DiscountPercentage = 0m,
                IsPurchased = false,
                IsCancelled = false,
                Quantity = cartItem.Quantity
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
                _logger.LogInformation("Order created with ID: {OrderId} for user {UserId}", orderItem.Id, userId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to save order for user {UserId}, book {BookId}", userId, dto.BookId);
                return StatusCode(500, new { message = "An error occurred while processing your order" });
            }

            var orderCountAfter = await _context.Orders.CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            var billDetails = $@"<h2>Order Confirmation - Book Haven</h2>
        <p><strong>Claim Code:</strong> {claimCode}</p>
        <p><strong>Book:</strong> {book.Title}</p>
        <p><strong>Author:</strong> {book.Author}</p>
        <p><strong>Price:</strong> ${book.Price:F2}</p>
        <p><strong>Quantity:</strong> {orderItem.Quantity}</p>
        <p><strong>Discount:</strong> {orderItem.DiscountPercentage * 100:F0}%</p>
        <p><strong>Final Price:</strong> ${(book.Price * orderItem.Quantity * (1 - orderItem.DiscountPercentage)):F2}</p>
        <p><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
        <p>Thank you for shopping with Book Haven!</p>";

            bool emailSent = false;
            try
            {
                await _emailService.SendEmailAsync(user.Email, "Your Book Haven Order Confirmation", billDetails);
                emailSent = true;
                _logger.LogInformation("Email sent successfully to {Email} for order {OrderId}", user.Email, orderItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. SMTP error: {ErrorMessage}", user.Email, ex.Message);
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

        [HttpPost("approve")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> ApproveOrder([FromBody] ApproveOrderDto dto)
        {
            if (string.IsNullOrEmpty(dto.ClaimCode))
            {
                _logger.LogWarning("Invalid claim code provided for ApproveOrder");
                return BadRequest(new { message = "Claim code is required" });
            }

            var order = await _context.Orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.ClaimCode == dto.ClaimCode);
            if (order == null)
            {
                _logger.LogWarning("Order not found for claim code: {ClaimCode}", dto.ClaimCode);
                return NotFound(new { message = "Order not found" });
            }

            if (order.IsPurchased)
            {
                _logger.LogWarning("Order already purchased for claim code: {ClaimCode}", dto.ClaimCode);
                return BadRequest(new { message = "Order already marked as purchased" });
            }

            try
            {
                order.IsPurchased = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order approved for claim code: {ClaimCode}", dto.ClaimCode);

                var billDetails = $@"<h2>Order Approved - Book Haven</h2>
                    <p><strong>Claim Code:</strong> {order.ClaimCode}</p>
                    <p><strong>Book:</strong> {order.Book?.Title ?? "Unknown"}</p>
                    <p><strong>Author:</strong> {order.Book?.Author ?? "Unknown"}</p>
                    <p><strong>Price:</strong> ${order.Book?.Price:F2}</p>
                    <p><strong>Discount:</strong> {order.DiscountPercentage * 100:F0}%</p>
                    <p><strong>Final Price:</strong> ${(order.Book?.Price * (1 - order.DiscountPercentage)):F2}</p>
                    <p><strong>Date Approved:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
                    <p>Your order has been successfully approved. Thank you for shopping with Book Haven!</p>";

                try
                {
                    await _emailService.SendEmailAsync(
                        order.User?.Email ?? "unknown@example.com",
                        "Your Book Haven Order Has Been Approved",
                        billDetails
                    );
                    _logger.LogInformation("Approval email sent successfully to {Email}", order.User?.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval email to {Email}", order.User?.Email);
                }

                return Ok(new { message = "Order marked as purchased successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve order for claim code: {ClaimCode}", dto.ClaimCode);
                return StatusCode(500, new { message = "An error occurred while processing the order" });
            }
        }

        [HttpPost("update-claim-code")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateClaimCode([FromBody] UpdateClaimCodeDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid input for UpdateClaimCode: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found for ID: {OrderId}", dto.OrderId);
                return NotFound(new { message = "Order not found" });
            }

            var existingClaimCode = await _context.Orders
                .AnyAsync(o => o.ClaimCode == dto.NewClaimCode && o.Id != dto.OrderId);
            if (existingClaimCode)
            {
                _logger.LogWarning("Claim code {ClaimCode} already exists", dto.NewClaimCode);
                return BadRequest(new { message = "Claim code already exists" });
            }

            var oldClaimCode = order.ClaimCode;
            order.ClaimCode = dto.NewClaimCode;

            try
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Claim code updated for order {OrderId} from {OldClaimCode} to {NewClaimCode}", dto.OrderId, oldClaimCode, dto.NewClaimCode);
                return Ok(new { message = "Claim code updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update claim code for order {OrderId}", dto.OrderId);
                return StatusCode(500, new { message = "An error occurred while updating the claim code" });
            }
        }

        [HttpDelete("remove/{bookId}")]
        public async Task<IActionResult> RemoveFromOrder(long bookId)
        {
            if (bookId <= 0)
            {
                _logger.LogWarning("Invalid book ID: {BookId} for RemoveFromOrder", bookId);
                return BadRequest(new { message = "Invalid book ID" });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID for RemoveFromOrder");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
            }

            var orderItem = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.BookId == bookId && !o.IsCancelled);
            if (orderItem == null)
            {
                _logger.LogWarning("Order not found for user {UserId} and book {BookId}", userId, bookId);
                return NotFound(new { message = "Order not found" });
            }

            if (orderItem.IsPurchased)
            {
                _logger.LogWarning("Cannot remove purchased order for user {UserId} and book {BookId}", userId, bookId);
                return BadRequest(new { message = "Cannot remove a purchased order" });
            }

            try
            {
                _context.Orders.Remove(orderItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order removed for user {UserId} and book {BookId}", userId, bookId);
                return Ok(new { message = "Book removed from order successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove order for user {UserId} and book {BookId}", userId, bookId);
                return StatusCode(500, new { message = "An error occurred while removing the order" });
            }
        }

        [HttpPatch("cancel/{bookId}")]
        public async Task<IActionResult> CancelOrder(long bookId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID for CancelOrder");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
            }

            var orderItem = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.BookId == bookId);
            if (orderItem == null)
            {
                _logger.LogWarning("Order not found for user {UserId} and book {BookId}", userId, bookId);
                return NotFound(new { message = "Order not found" });
            }

            if (orderItem.IsCancelled)
            {
                _logger.LogWarning("Order already cancelled for user {UserId} and book {BookId}", userId, bookId);
                return BadRequest(new { message = "Order is already cancelled" });
            }

            try
            {
                orderItem.IsCancelled = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order cancelled for user {UserId} and book {BookId}", userId, bookId);
                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order for user {UserId} and book {BookId}", userId, bookId);
                return StatusCode(500, new { message = "An error occurred while cancelling the order" });
            }
        }

        [HttpGet("with-discount")]
        public async Task<IActionResult> GetOrderWithDiscount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID for GetOrderWithDiscount");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
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
                    o.DiscountPercentage,
                    o.IsPurchased,
                    o.IsCancelled,
                    o.Quantity
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

        [HttpGet]
        public async Task<IActionResult> GetOrder()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID for GetOrder");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
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
                    o.ClaimCode,
                    o.DateAdded,
                    o.IsPurchased,
                    HasPurchased = _context.Orders.Any(ord => ord.UserId == userId && ord.BookId == o.BookId && ord.IsPurchased)
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var userClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                _logger.LogInformation("User claims for GetAllOrders: {@Claims}", userClaims);

                var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                _logger.LogInformation("Roles for user in GetAllOrders: {Roles}", string.Join(", ", roles));

                _logger.LogInformation("Starting database query for orders");

                var orderCount = await _context.Orders.CountAsync();
                var userCount = await _context.Users.CountAsync();
                var bookCount = await _context.Books.CountAsync();
                _logger.LogInformation("Database stats: Orders={OrderCount}, Users={UserCount}, Books={BookCount}", orderCount, userCount, bookCount);

                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Book)
                    .AsNoTracking()
                    .Select(o => new
                    {
                        Id = o.Id,
                        ClaimCode = o.ClaimCode ?? "N/A",
                        Customer = o.User != null ? (o.User.UserName ?? o.User.Email ?? "Unknown") : "Unknown",
                        Date = o.DateAdded.ToString("yyyy-MM-dd"),
                        Total = o.Book != null ? Math.Round(o.Book.Price * (1 - o.DiscountPercentage), 2) : 0m,
                        Items = o.Book != null
                            ? new[]
                            {
                                new
                                {
                                    Name = o.Book.Title ?? "Unknown Book",
                                    Quantity = o.Quantity,
                                    Price = o.Book.Price != null ? Math.Round(o.Book.Price, 2) : 0m
                                }
                            }
                            : new[]
                            {
                                new
                                {
                                    Name = "Unknown Book",
                                    Quantity = o.Quantity,
                                    Price = 0m
                                }
                            },
                        Status = o.IsPurchased ? "purchased" : "pending"
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} orders for admin dashboard", orders.Count);
                _logger.LogDebug("Orders response data: {@Orders}", orders);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders for admin dashboard");
                return StatusCode(500, new { message = "An error occurred while retrieving orders", details = ex.Message });
            }
        }
    }

    public class AddOrderDto
    {
        public long BookId { get; set; }
    }

    public class ApproveOrderDto
    {
        public string ClaimCode { get; set; }
    }

    public class UpdateClaimCodeDto
    {
        [Required(ErrorMessage = "Order ID is required")]
        [Range(1, long.MaxValue, ErrorMessage = "Order ID must be a positive number")]
        public long OrderId { get; set; }

        [Required(ErrorMessage = "New claim code is required")]
        [MinLength(12, ErrorMessage = "Claim code must be at least 12 characters long")]
        public string NewClaimCode { get; set; }
    }
}