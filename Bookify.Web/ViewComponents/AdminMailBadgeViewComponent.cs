using Bookify.Data.Data;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.Web.ViewComponents
{
    /// <summary>Badge hòm thư cho ADMIN — hiện số tin nhắn chưa đọc</summary>
    public class AdminMailBadgeViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public AdminMailBadgeViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            int count = 0;
            try
            {
                count = _context.ContactMessages.Count(m => !m.IsRead);
            }
            catch { /* bảng chưa tồn tại → bỏ qua */ }

            return View("Default", count);
        }
    }
}
