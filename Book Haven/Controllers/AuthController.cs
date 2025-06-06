﻿using Microsoft.AspNetCore.Mvc;
using Book_Haven.DTO;
using Book_Haven.Entities;
using Microsoft.AspNetCore.Identity;
using Book_Haven.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

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
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateToken(user, roles);
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
            // Validate sale-related fields
            if (bookDto.IsOnSale)
            {
                if (!bookDto.DiscountPercentage.HasValue || bookDto.DiscountPercentage <= 0 || bookDto.DiscountPercentage > 100)
                {
                    return BadRequest(new { errors = new[] { new { description = "Discount percentage must be between 1 and 100." } } });
                }
                if (!bookDto.SaleStartDate.HasValue)
                {
                    return BadRequest(new { errors = new[] { new { description = "Sale start date is required when on sale." } } });
                }
                if (!bookDto.SaleEndDate.HasValue)
                {
                    return BadRequest(new { errors = new[] { new { description = "Sale end date is required when on sale." } } });
                }
                if (bookDto.SaleEndDate <= bookDto.SaleStartDate)
                {
                    return BadRequest(new { errors = new[] { new { description = "Sale end date must be after start date." } } });
                }
            }

            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                ISBN = bookDto.ISBN,
                Price = bookDto.Price,
                PublicationYear = bookDto.PublicationYear,
                Description = bookDto.Description,
                Category = bookDto.Category,
                IsOnSale = bookDto.IsOnSale,
                DiscountPercentage = bookDto.IsOnSale ? bookDto.DiscountPercentage ?? 0 : 0,
                SaleStartDate = bookDto.IsOnSale ? bookDto.SaleStartDate : null,
                SaleEndDate = bookDto.IsOnSale ? bookDto.SaleEndDate : null
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

        [HttpPost("books/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBook(long id, [FromForm] BookDto bookDto)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            // Validate sale-related fields
            if (bookDto.IsOnSale)
            {
                if (!bookDto.DiscountPercentage.HasValue || bookDto.DiscountPercentage <= 0 || bookDto.DiscountPercentage > 100)
                {
                    return BadRequest(new { errors = new[] { new { description = "Discount percentage must be between 1 and 100." } } });
                }
                if (!bookDto.SaleStartDate.HasValue)
                {
                    return BadRequest(new { errors = new[] { new { description = "Sale start date is required when on sale." } } });
                }
                if (!bookDto.SaleEndDate.HasValue)
                {
                    return BadRequest(new { errors = new[] { new { description = "Sale end date is required when on sale." } } });
                }
                if (bookDto.SaleEndDate <= bookDto.SaleStartDate)
                {
                    return BadRequest(new { errors = new[] { new { description = "Sale end date must be after start date." } } });
                }
            }

            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.ISBN = bookDto.ISBN;
            book.Price = bookDto.Price;
            book.PublicationYear = bookDto.PublicationYear;
            book.Description = bookDto.Description;
            book.Category = bookDto.Category;
            book.IsOnSale = bookDto.IsOnSale;
            book.DiscountPercentage = bookDto.IsOnSale ? bookDto.DiscountPercentage ?? 0 : 0;
            book.SaleStartDate = bookDto.IsOnSale ? bookDto.SaleStartDate : null;
            book.SaleEndDate = bookDto.IsOnSale ? bookDto.SaleEndDate : null;

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

                // Delete old image if exists
                if (!string.IsNullOrEmpty(book.ImagePath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, book.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                book.ImagePath = $"/images/{fileName}";
            }

            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            return Ok(book);
        }

        [HttpDelete("books/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteBook(long id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(book.ImagePath))
            {
                var filePath = Path.Combine(_environment.WebRootPath, book.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("books/{id}")]
        public async Task<IActionResult> GetBook(long id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            // Check if sale has expired
            if (book.IsOnSale && book.SaleEndDate < DateTime.UtcNow)
            {
                book.IsOnSale = false;
                book.DiscountPercentage = 0;
                book.SaleStartDate = null;
                book.SaleEndDate = null;
                await _context.SaveChangesAsync();
            }

            return Ok(book);
        }

        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _context.Books.ToListAsync();

            // Update expired sales
            foreach (var book in books)
            {
                if (book.IsOnSale && book.SaleEndDate < DateTime.UtcNow)
                {
                    book.IsOnSale = false;
                    book.DiscountPercentage = 0;
                    book.SaleStartDate = null;
                    book.SaleEndDate = null;
                }
            }
            await _context.SaveChangesAsync();

            return Ok(books);
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetAllContacts()
        {
            var contacts = await _context.Contacts.ToListAsync();
            return Ok(contacts);
        }
    }
}