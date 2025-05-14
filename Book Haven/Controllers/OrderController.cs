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
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            IEmailService emailService,
            INotificationService notificationService,
            ILogger<OrderController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
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

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == dto.BookId && !c.IsRemoved);
            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found for user {UserId} and book {BookId}", userId, dto.BookId);
                return BadRequest(new { message = "Book not found in cart" });
            }

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
                // Calculate purchase-based discount
                var successfulPurchaseCountBefore = await _context.Orders
                    .CountAsync(o => o.UserId == userId && o.IsPurchased);
                decimal purchaseDiscountPercentage = 0m;
                int purchasePosition = successfulPurchaseCountBefore + 1;
                if (purchasePosition >= 5)
                {
                    int cycleIndex = (purchasePosition - 5) / 5;
                    int positionInCycle = (purchasePosition - 5) % 5;
                    if (positionInCycle == 0)
                    {
                        purchaseDiscountPercentage = (cycleIndex % 2 == 0) ? 0.05m : 0.10m;
                    }
                }

                // Calculate quantity-based discount
                decimal quantityDiscountPercentage = 0m;
                if (cartItem.Quantity >= 10)
                {
                    quantityDiscountPercentage = 0.10m;
                }
                else if (cartItem.Quantity >= 5)
                {
                    quantityDiscountPercentage = 0.05m;
                }

                // Calculate sale discount
                decimal saleDiscountPercentage = book.IsOnSale && book.DiscountPercentage > 0 ? book.DiscountPercentage / 100 : 0m;

                // Use the highest discount
                orderItem.DiscountPercentage = Math.Max(purchaseDiscountPercentage + quantityDiscountPercentage, saleDiscountPercentage);

                _context.Orders.Add(orderItem);

                // Remove from cart
                cartItem.IsRemoved = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Order created with ID: {OrderId} for user {UserId}, Discount: {DiscountPercentage}%",
                    orderItem.Id, userId, orderItem.DiscountPercentage * 100);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to save order for user {UserId}, book {BookId}", userId, dto.BookId);
                return StatusCode(500, new { message = "An error occurred while processing your order" });
            }

            var successfulPurchaseCountAfter = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.IsPurchased);
            var totalOrderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            var billDetails = $@"<h2>Order Confirmation - Book Haven</h2>
        <p><strong>Claim Code:</strong> {claimCode}</p>
        <p><strong>Book:</strong> {book.Title}</p>
        <p><strong>Author:</strong> {book.Author}</p>
        <p><strong>Original Price per Book:</strong> ${book.Price:F2}</p>
        <p><strong>Quantity:</strong> {orderItem.Quantity}</p>
        {(orderItem.DiscountPercentage > 0 ? $"<p><strong>Discount:</strong> {orderItem.DiscountPercentage * 100:F0}%{(book.IsOnSale && orderItem.DiscountPercentage == book.DiscountPercentage / 100 ? " (On Sale)" : "")}</p>" : "")}
        <p><strong>Price per Book After Discount:</strong> ${(book.Price * (1 - orderItem.DiscountPercentage)):F2}</p>
        <p><strong>Total Price:</strong> ${(book.Price * orderItem.Quantity * (1 - orderItem.DiscountPercentage)):F2}</p>
        <p><strong>Order Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
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
                totalOrders = totalOrderCount,
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

            if (order.IsCancelled)
            {
                _logger.LogWarning("Cannot approve cancelled order for claim code: {ClaimCode}", dto.ClaimCode);
                return BadRequest(new { message = "Cannot approve a cancelled order" });
            }

            try
            {
                order.IsPurchased = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order approved for claim code: {ClaimCode}, OrderId: {OrderId}, UserId: {UserId}, BookId: {BookId}",
                    dto.ClaimCode, order.Id, order.UserId, order.BookId);

                // Send real-time notification
                try
                {
                    var notificationMessage = $"Your order for '{order.Book?.Title ?? "Unknown"}' (Claim Code: {order.ClaimCode}) has been successfully purchased.";
                    _logger.LogInformation("Attempting to send notification to UserId: {UserId}, Message: {Message}",
                        order.UserId, notificationMessage);
                    await _notificationService.SendNotificationAsync(order.UserId.ToString(), notificationMessage);
                    _logger.LogInformation("Real-time notification sent to user {UserId} for order {OrderId}",
                        order.UserId, order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send real-time notification to user {UserId} for order {OrderId}. Error: {ErrorMessage}",
                        order.UserId, order.Id, ex.Message);
                }

                var bookPrice = order.Book?.Price ?? 0m;
                var billDetails = $@"<h2>Order Approved - Book Haven</h2>
                    <p><strong>Claim Code:</strong> {order.ClaimCode}</p>
                    <p><strong>Book:</strong> {order.Book?.Title ?? "Unknown"}</p>
                    <p><strong>Author:</strong> {order.Book?.Author ?? "Unknown"}</p>
                    <p><strong>Original Price per Book:</strong> ${bookPrice:F2}</p>
                    <p><strong>Quantity:</strong> {order.Quantity}</p>
                    {(order.DiscountPercentage > 0 ? $"<p><strong>Discount:</strong> {order.DiscountPercentage * 100:F0}%{(order.Book != null && order.Book.IsOnSale && order.DiscountPercentage == order.Book.DiscountPercentage / 100 ? " (On Sale)" : "")}</p>" : "")}
                    <p><strong>Price per Book After Discount:</strong> ${(bookPrice * (1 - order.DiscountPercentage)):F2}</p>
                    <p><strong>Total Price:</strong> ${(bookPrice * order.Quantity * (1 - order.DiscountPercentage)):F2}</p>
                    <p><strong>Date Approved:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
                    <p>Your order has been successfully approved. Thank you for shopping with Book Haven!</p>";

                try
                {
                    if (order.User?.Email != null)
                    {
                        await _emailService.SendEmailAsync(
                            order.User.Email,
                            "Your Book Haven Order Has Been Approved",
                            billDetails
                        );
                        _logger.LogInformation("Approval email sent successfully to {Email} for order {OrderId}",
                            order.User.Email, order.Id);
                    }
                    else
                    {
                        _logger.LogWarning("No email address found for user {UserId} for order {OrderId}",
                            order.UserId, order.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval email to {Email} for order {OrderId}. Error: {ErrorMessage}",
                        order.User?.Email ?? "unknown", order.Id, ex.Message);
                }

                return Ok(new { message = "Order marked as purchased successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve order for claim code: {ClaimCode}. Error: {ErrorMessage}",
                    dto.ClaimCode, ex.Message);
                return StatusCode(500, new { message = "An error occurred while processing the order" });
            }
        }

        [HttpPost("update-claim-code")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateClaimCode([FromBody] UpdateClaimCodeDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid input for UpdateClaimCode: {Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
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
                _logger.LogInformation("Claim code updated for order {OrderId} from {OldClaimCode} to {NewClaimCode}",
                    dto.OrderId, oldClaimCode, dto.NewClaimCode);
                return Ok(new { message = "Claim code updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update claim code for order {OrderId}. Error: {ErrorMessage}",
                    dto.OrderId, ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating the claim code" });
            }
        }

        [HttpDelete("remove/{orderId}")]
        public async Task<IActionResult> RemoveFromOrder(long orderId)
        {
            if (orderId <= 0)
            {
                _logger.LogWarning("Invalid order ID: {OrderId} for RemoveFromOrder", orderId);
                return BadRequest(new { message = "Invalid order ID" });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID for RemoveFromOrder");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
            }

            var orderItem = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && !o.IsCancelled);
            if (orderItem == null)
            {
                _logger.LogWarning("Order not found for user {UserId} and order {OrderId}", userId, orderId);
                return NotFound(new { message = "Order not found" });
            }

            if (orderItem.IsPurchased)
            {
                _logger.LogWarning("Cannot remove purchased order for user {UserId} and order {OrderId}", userId, orderId);
                return BadRequest(new { message = "Cannot remove a purchased order" });
            }

            try
            {
                _context.Orders.Remove(orderItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order removed for user {UserId} and order {OrderId}", userId, orderId);
                return Ok(new { message = "Book removed from order successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove order for user {UserId} and order {OrderId}. Error: {ErrorMessage}",
                    userId, orderId, ex.Message);
                return StatusCode(500, new { message = "An error occurred while removing the order" });
            }
        }

        [HttpPatch("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(long orderId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID for CancelOrder");
                return Unauthorized(new { message = "User not authenticated or invalid token" });
            }

            var orderItem = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (orderItem == null)
            {
                _logger.LogWarning("Order not found for user {UserId} and order {OrderId}", userId, orderId);
                return NotFound(new { message = "Order not found" });
            }

            if (orderItem.IsCancelled)
            {
                _logger.LogWarning("Order already cancelled for user {UserId} and order {OrderId}", userId, orderId);
                return BadRequest(new { message = "Order is already cancelled" });
            }

            try
            {
                if (orderItem.Quantity > 1)
                {
                    orderItem.Quantity -= 1;
                    _logger.LogInformation("Order quantity decremented for user {UserId} and order {OrderId}. New quantity: {Quantity}",
                        userId, orderId, orderItem.Quantity);
                }
                else
                {
                    orderItem.IsCancelled = true;
                    _logger.LogInformation("Order cancelled for user {UserId} and order {OrderId}", userId, orderId);
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = "Order cancelled successfully", quantity = orderItem.Quantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order for user {UserId} and order {OrderId}. Error: {ErrorMessage}",
                    userId, orderId, ex.Message);
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
                    Id = o.Id,
                    BookId = o.BookId,
                    o.Book.Title,
                    o.Book.Author,
                    o.Book.Price,
                    o.Book.ImagePath,
                    o.DiscountPercentage,
                    o.IsPurchased,
                    o.IsCancelled,
                    o.Quantity,
                    o.ClaimCode,
                    o.DateAdded
                })
                .ToListAsync();

            var successfulPurchaseCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.IsPurchased);
            var totalOrderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
            var currentOrderBookCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.DateAdded.Date == DateTime.UtcNow.Date);

            decimal purchaseDiscountPercentage = 0m;
            int nextPurchasePosition = successfulPurchaseCount + 1;
            if (nextPurchasePosition >= 5)
            {
                int cycleIndex = (nextPurchasePosition - 5) / 5;
                int positionInCycle = (nextPurchasePosition - 5) % 5;
                if (positionInCycle == 0)
                {
                    purchaseDiscountPercentage = (cycleIndex % 2 == 0) ? 0.05m : 0.10m;
                }
            }

            return Ok(new
            {
                orders,
                purchaseDiscountPercentage = purchaseDiscountPercentage * 100,
                totalOrders = totalOrderCount,
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
                    Id = o.Id,
                    BookId = o.BookId,
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
                _logger.LogInformation("Database stats: Orders={OrderCount}, Users={UserCount}, Books={BookCount}",
                    orderCount, userCount, bookCount);

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
                        Total = o.Book != null ? Math.Round(o.Book.Price * (1 - o.DiscountPercentage) * o.Quantity, 2) : 0m,
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
                _logger.LogError(ex, "Failed to retrieve orders for admin dashboard. Error: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving orders", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetUserOrders(long userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Book)
                .Include(o => o.User)
                .Select(o => new
                {
                    o.Id,
                    o.BookId,
                    BookTitle = o.Book != null ? o.Book.Title : "Unknown",
                    o.UserId,
                    UserName = o.User != null ? (o.User.UserName ?? o.User.Email ?? "Unknown") : "Unknown",
                    o.ClaimCode,
                    o.IsPurchased,
                    o.IsCancelled,
                    o.Quantity,
                    o.DateAdded,
                    DiscountPercentage = o.DiscountPercentage * 100
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} orders for user {UserId}", orders.Count, userId);
            return Ok(orders);
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