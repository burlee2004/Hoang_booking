using Bookify.Data;
using Bookify.Data.Data;
using Bookify.Data.Models;
using Bookify.Services.ModelsRepos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ReservationRepo _bookingRepo;
        private readonly AppDbContext _context;

        public BookingsController(ReservationRepo bookingRepo, AppDbContext appContext)
        {
            _bookingRepo = bookingRepo;
            _context = appContext;
        }

        // ─── Danh sách tất cả đặt phòng ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Reservations.ToListAsync();
            return View("~/Views/Admin/Bookings.cshtml", bookings);
        }

        // ─── Chi tiết đặt phòng ───────────────────────────────────────────────
        public IActionResult Details(int id)
        {
            var reservation = _context.Reservations
                .Where(r => r.Id == id)
                .Include(r => r.Items)
                .ThenInclude(i => i.Room)
                .FirstOrDefault();

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng!";
                return RedirectToAction("Bookings");
            }

            return View("~/Views/Admin/Details.cshtml", reservation);
        }

        // ─── Xóa đặt phòng ────────────────────────────────────────────────────
        public IActionResult DeleteBooking(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng!";
                // Redirect đúng theo role
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Bookings");
                return RedirectToAction("Index", "Profile");
            }

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã xóa đặt phòng thành công!";

            // Admin → trang quản lý, User → trang hồ sơ
            if (User.IsInRole("Admin"))
                return RedirectToAction("Bookings");

            return RedirectToAction("Index", "Profile");
        }

        // ─── Duyệt đặt phòng (Admin) ──────────────────────────────────────────
        [HttpPost]
        public IActionResult ApproveBooking(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng!";
                return RedirectToAction("Bookings");
            }
            reservation.Status = "Đã Duyệt";
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Đặt phòng #{id} đã được duyệt thành công!";
            return RedirectToAction("Bookings");
        }

        // ─── Từ chối đặt phòng (Admin) ────────────────────────────────────────
        [HttpPost]
        public IActionResult RejectBooking(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng!";
                return RedirectToAction("Bookings");
            }
            reservation.Status = "Đã Từ Chối";
            _context.SaveChanges();
            TempData["ErrorMessage"] = $"Đặt phòng #{id} đã bị từ chối.";
            return RedirectToAction("Bookings");
        }
    }
}
