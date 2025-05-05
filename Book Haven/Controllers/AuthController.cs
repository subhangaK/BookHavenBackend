using Microsoft.AspNetCore.Mvc;
using Book_Haven.DTO;
using Book_Haven.Entities;
using Microsoft.AspNetCore.Identity;
using Book_Haven.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            UserManager<User> userManager,
            ITokenService tokenService,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            Console.WriteLine($"Received registration request: {registerDto.Username}, {registerDto.Email}");
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return BadRequest(new { errors = new[] { new { description = "Passwords do not match" } } });
            }

            var user = new User { UserName = registerDto.Username, Email = registerDto.Email };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                Console.WriteLine("User registered successfully");
                return Ok("User registered successfully");
            }
            Console.WriteLine("Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            Console.WriteLine($"Received login request for email: {loginDto.Email}");
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                var roles = await _userManager.GetRolesAsync(user); // Fetch roles
                var token = _tokenService.GenerateToken(user, roles); // Generate token with roles
                Console.WriteLine("Login successful, token generated");
                return Ok(new { token });
            }
            Console.WriteLine("Login failed: Invalid credentials");
            return Unauthorized();
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult AdminOnly()
        {
            return Ok("This is an admin-only endpoint.");
        }

        [HttpPost("books")]
        [Authorize(Roles = "SuperAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddBook([FromForm] BookDto bookDto)
        {
            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                ISBN = bookDto.ISBN,
                Price = bookDto.Price,
                PublicationYear = bookDto.PublicationYear
            };

            if (bookDto.Image != null && bookDto.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(bookDto.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await bookDto.Image.CopyToAsync(stream);
                }

                book.ImagePath = $"/images/{fileName}";
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        [HttpGet("books/{id}")]
        public async Task<IActionResult> GetBook(long id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return Ok(book);
        }

        // New method to get all books (publicly accessible)
        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _context.Books.ToListAsync();
            return Ok(books);
        }
    }
}