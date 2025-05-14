using System;

namespace Book_Haven.DTO
{
    public class BannerDTO
    {
        public int Id { get; set; }

        public string Message { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

    }
}