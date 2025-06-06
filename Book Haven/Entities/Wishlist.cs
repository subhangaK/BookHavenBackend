﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Haven.Entities
{
    public class Wishlist
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; } // Changed from string to long

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public long BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}