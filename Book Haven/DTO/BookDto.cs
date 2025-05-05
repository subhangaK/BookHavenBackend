using System.ComponentModel.DataAnnotations;

namespace Book_Haven.DTO
{
    public class BookDto
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Author { get; set; }

        [Required]
        public string ISBN { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(1000, 9999, ErrorMessage = "Publication year must be a valid year.")]
        public int PublicationYear { get; set; }
        public IFormFile Image { get; set; }
    }
}