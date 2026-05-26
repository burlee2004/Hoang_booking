using Bookify.Data.Data;
using Bookify.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContactAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ContactAdminController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: danh sách tin nhắn
        public async Task<IActionResult> Index()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
            return View("~/Views/Admin/ContactMessages.cshtml", messages);
        }

        // POST: đánh dấu đã đọc + gửi phản hồi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyText)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            msg.AdminReply = replyText?.Trim();
            msg.RepliedAt  = DateTime.UtcNow;
            msg.IsRead     = true;
            msg.IsUserRead = false; // reset để user thấy badge thông báo mới
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gửi phản hồi đến {msg.Email}!";
            return RedirectToAction("Index");
        }

        // POST: xóa tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                _context.ContactMessages.Remove(msg);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Đã xóa tin nhắn!";
            return RedirectToAction("Index");
        }

        // POST: đánh dấu đã đọc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null) { msg.IsRead = true; await _context.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }
    }
}
