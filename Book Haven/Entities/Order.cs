using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Haven.Entities
{
    public class Order
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public long BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public decimal DiscountPercentage { get; set; } // Added for tracking discounts
        public string ClaimCode { get; set; }
        public bool IsPurchased { get; set; } = false;
    }
}