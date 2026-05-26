using Bookify.Data.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.Web.ViewComponents
{
    /// <summary>Badge hòm thư cho USER — hiện số phản hồi admin chưa xem</summary>
    public class MailboxBadgeViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MailboxBadgeViewComponent(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int count = 0;
            try
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                if (user != null)
                {
                    // Đếm tin nhắn có reply từ admin mà user chưa xem
                    count = _context.ContactMessages
                        .Count(m => m.UserId == user.Id
                                 && m.AdminReply != null
                                 && !m.IsUserRead);
                }
            }
            catch { /* bảng chưa tồn tại → bỏ qua */ }

            return View("Default", count);
        }
    }
}
