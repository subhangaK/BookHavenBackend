using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Book_Haven.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ApplicationDbContext context, ILogger<ReviewController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] ReviewDto dto)
        {
            if (dto == null || dto.BookId <= 0 || dto.Rating < 1 || dto.Rating > 5 || string.IsNullOrEmpty(dto.Comment))
            {
                _logger.LogWarning("Invalid review submission: {@ReviewDto}", dto);
                return BadRequest("Invalid review data. Rating must be between 1 and 5, and comment is required.");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Unauthorized: Invalid or missing user ID");
                return Unauthorized("User not authenticated or invalid token.");
            }

            // Check if the book exists
            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
            {
                _logger.LogWarning("Book not found for ID: {BookId}", dto.BookId);
                return NotFound("Book not found");
            }

            // Check if the user has purchased the book
            var hasPurchased = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.BookId == dto.BookId && o.IsPurchased);
            if (!hasPurchased)
            {
                _logger.LogWarning("User {UserId} has not purchased book {BookId}", userId, dto.BookId);
                return BadRequest("You can only review books you have purchased.");
            }

            // Check if the user has already reviewed this book
            var existingReview = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.BookId == dto.BookId);
            if (existingReview)
            {
                _logger.LogWarning("User {UserId} has already reviewed book {BookId}", userId, dto.BookId);
                return BadRequest("You have already reviewed this book.");
            }

            var review = new Review
            {
                UserId = userId,
                BookId = dto.BookId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                DatePosted = DateTime.UtcNow
            };

            try
            {
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Review submitted by user {UserId} for book {BookId}", userId, dto.BookId);
                return Ok(new { message = "Review submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit review for user {UserId}, book {BookId}", userId, dto.BookId);
                return StatusCode(500, "An error occurred while submitting your review.");
            }
        }

        [HttpGet("{bookId}")]
        public async Task<IActionResult> GetReviewsForBook(long bookId)
        {
            if (bookId <= 0)
            {
                _logger.LogWarning("Invalid book ID: {BookId}", bookId);
                return BadRequest("Invalid book ID");
            }

            var reviews = await _context.Reviews
                .Where(r => r.BookId == bookId)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.DatePosted,
                    UserName = r.User != null ? (r.User.UserName ?? r.User.Email ?? "Anonymous") : "Anonymous"
                })
                .ToListAsync();

            var reviewCount = reviews.Count;
            var averageRating = reviewCount > 0 ? reviews.Average(r => r.Rating) : 0;

            return Ok(new
            {
                reviews,
                reviewCount,
                averageRating = Math.Round(averageRating, 1)
            });
        }
    }

    public class ReviewDto
    {
        public long BookId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}