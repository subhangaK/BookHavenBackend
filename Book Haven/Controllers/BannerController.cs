using Book_Haven;
using Book_Haven.DTO;
using Book_Haven.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Book_Haven.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BannerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Banner/active (Fetch active banners for frontend)
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<BannerDTO>>> GetActiveBanners()
        {
            var currentTime = DateTime.UtcNow;
            var activeBanners = await _context.Banners
                .Where(b => b.IsActive && !b.IsDeleted && b.StartTime <= currentTime && b.EndTime >= currentTime)
                .Select(b => new BannerDTO
                {
                    Id = b.Id,
                    Message = b.Message,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    IsActive = b.IsActive,
                    IsDeleted = b.IsDeleted
                })
                .ToListAsync();

            return Ok(activeBanners);
        }

        // GET: api/Banner (Fetch all banners - for admin)
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerDTO>>> GetBanners([FromQuery] bool? showDeleted = false)
        {
            var query = _context.Banners.AsQueryable();
            if (!showDeleted.GetValueOrDefault())
            {
                query = query.Where(b => !b.IsDeleted);
            }

            var banners = await query
                .Select(b => new BannerDTO
                {
                    Id = b.Id,
                    Message = b.Message,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    IsActive = b.IsActive,
                    IsDeleted = b.IsDeleted
                })
                .ToListAsync();

            return Ok(banners);
        }

        // POST: api/Banner (Create a new banner - for admin)
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult<BannerDTO>> CreateBanner([FromBody] BannerDTO bannerDTO)
        {
            if (bannerDTO.EndTime <= bannerDTO.StartTime)
            {
                return BadRequest("End time must be after start time.");
            }

            var banner = new Banner
            {
                Message = bannerDTO.Message,
                StartTime = bannerDTO.StartTime,
                EndTime = bannerDTO.EndTime,
                IsActive = bannerDTO.IsActive,
                IsDeleted = false, // Initialize IsDeleted
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();

            bannerDTO.Id = banner.Id;
            bannerDTO.IsDeleted = banner.IsDeleted;
            return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, bannerDTO);
        }

        // GET: api/Banner/5 (Fetch a specific banner - for admin)
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<BannerDTO>> GetBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);

            if (banner == null)
            {
                return NotFound();
            }

            var bannerDTO = new BannerDTO
            {
                Id = banner.Id,
                Message = banner.Message,
                StartTime = banner.StartTime,
                EndTime = banner.EndTime,
                IsActive = banner.IsActive,
                IsDeleted = banner.IsDeleted
            };

            return Ok(bannerDTO);
        }

        // PUT: api/Banner/5 (Update a banner - for admin)
        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBanner(int id, [FromBody] BannerDTO bannerDTO)
        {
            if (id != bannerDTO.Id)
            {
                return BadRequest("ID mismatch.");
            }

            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }

            banner.Message = bannerDTO.Message;
            banner.StartTime = bannerDTO.StartTime;
            banner.EndTime = bannerDTO.EndTime;
            banner.IsActive = bannerDTO.IsActive;
            banner.IsDeleted = bannerDTO.IsDeleted; // Update IsDeleted
            banner.UpdatedAt = DateTime.UtcNow;

            if (banner.EndTime <= banner.StartTime)
            {
                return BadRequest("End time must be after start time.");
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BannerExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Banner/5 (Soft delete a banner - for admin)
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }

            banner.IsDeleted = true;
            banner.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BannerExists(int id)
        {
            return _context.Banners.Any(e => e.Id == id);
        }
    }
}