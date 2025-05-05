using Microsoft.AspNetCore.Mvc;
using Book_Haven.DTO;
using Book_Haven.Entities;
using Microsoft.AspNetCore.Identity;
using Book_Haven.Services;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;

        public AuthController(UserManager<User> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var user = new User { UserName = registerDto.Username, Email = registerDto.Email };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                return Ok("User registered successfully");
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                var token = _tokenService.GenerateToken(user);
                return Ok(new { token });
            }
            return Unauthorized();
        }
    }
}