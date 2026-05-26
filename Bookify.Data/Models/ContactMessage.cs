using System.ComponentModel.DataAnnotations;

namespace Bookify.Data.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string? UserId { get; set; } // null nếu khách vãng lai

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;      // Admin đã đọc tin nhắn chưa
        public bool IsUserRead { get; set; } = false;  // User đã xem reply chưa

        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}
