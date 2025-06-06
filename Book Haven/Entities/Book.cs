﻿using System;

namespace Book_Haven.Entities
{
    public class Book
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public decimal Price { get; set; }
        public int PublicationYear { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // Fiction or Nonfiction
        public string ImagePath { get; set; }
        public bool IsOnSale { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }

        public decimal CurrentPrice => IsOnSale && SaleStartDate <= DateTime.UtcNow && SaleEndDate >= DateTime.UtcNow
            ? Price * (1 - DiscountPercentage / 100)
            : Price;
    }
}