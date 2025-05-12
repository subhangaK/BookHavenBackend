using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Haven.Entities
{
    public class Cart
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

        [Required]
        public int Quantity { get; set; } = 1; // New property for quantity, default to 1

        public bool IsRemoved { get; set; } = false;
    }
}