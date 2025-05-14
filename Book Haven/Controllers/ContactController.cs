using Book_Haven.DTO;
using Book_Haven.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Book_Haven.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly ContactService _contactService;

        public ContactController(ContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitContact([FromBody] ContactDTO contactDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _contactService.SaveContactAsync(contactDto);
            if (result)
            {
                return Ok(new { message = "Contact form submitted successfully" });
            }

            return StatusCode(500, new { message = "An error occurred while saving the contact form" });
        }
    }
}