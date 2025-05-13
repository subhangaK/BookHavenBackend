using System;
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

        [Required]
        public string Description { get; set; }

        [Required]
        public string Category { get; set; } // Fiction or Nonfiction

        public IFormFile? Image { get; set; } // Nullable to ensure optional

        public bool IsOnSale { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
        public decimal? DiscountPercentage { get; set; } // Add this field

        public DateTime? SaleStartDate { get; set; }

        public DateTime? SaleEndDate { get; set; }
    }
}