using System.ComponentModel.DataAnnotations;

namespace Bookify.Web.Models
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
        [StringLength(1000, ErrorMessage = "Tin nhắn tối đa 1000 ký tự")]
        [MinLength(10, ErrorMessage = "Tin nhắn tối thiểu 10 ký tự")]
        public string Message { get; set; } = string.Empty;
    }
}
