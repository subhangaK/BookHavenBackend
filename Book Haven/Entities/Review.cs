using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Haven.Entities
{
    public class Review
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

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string Comment { get; set; }

        public DateTime DatePosted { get; set; } = DateTime.UtcNow;
    }
}