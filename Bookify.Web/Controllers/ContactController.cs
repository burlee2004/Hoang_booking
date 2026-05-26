using Bookify.Data.Data;
using Bookify.Data.Models;
using Bookify.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Web.Controllers
{
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ContactController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: trang liên hệ
        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            var vm = new ContactViewModel();
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    vm.Email = user.Email ?? string.Empty;
                    var profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);
                    vm.FullName = profile?.FullName ?? string.Empty;
                }
            }
            return View("~/Views/Shared/Contact.cshtml", vm);
        }

        // POST: gửi tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Shared/Contact.cshtml", vm);

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                userId = user?.Id;
            }

            _context.ContactMessages.Add(new ContactMessage
            {
                FullName = vm.FullName.Trim(),
                Email    = vm.Email.Trim(),
                Message  = vm.Message.Trim(),
                UserId   = userId,
                SentAt   = DateTime.UtcNow,
                IsRead   = false
            });
            await _context.SaveChangesAsync();

            TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất.";
            return RedirectToAction("Contact");
        }

        // GET: user xem phản hồi từ admin
        [Authorize]
        public async Task<IActionResult> MyMessages()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Contact");

            var messages = await _context.ContactMessages
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // Đánh dấu user đã xem reply
            var unread = messages.Where(m => m.AdminReply != null && !m.IsUserRead).ToList();
            foreach (var m in unread) m.IsUserRead = true;
            if (unread.Any()) await _context.SaveChangesAsync();

            return View("~/Views/Shared/MyMessages.cshtml", messages);
        }
    }
}
