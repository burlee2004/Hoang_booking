using Bookify.Data.Data;
using Bookify.Data.Models;
using Bookify.Services.ModelsRepos;
using Bookify.Web.Models;
using Bookify.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ReservationRepo _reservationRepo;
        private readonly ReservationItemRepo _itemRepo;
        private readonly PaymentRepo _paymentRepo;
        private readonly IConfiguration _config;
        private readonly AppDbContext _dbContext;
        private readonly RoomRepo _roomRepo;
        private readonly VNPayService _vnPayService;
        // --- 2 BIẾN MỚI THÊM ---
        private readonly ContactMessageRepo _contactMessageRepo;
        private readonly EmailService _emailService;
        public PaymentController(
            ReservationRepo reservationRepo,
            ReservationItemRepo itemRepo,
            PaymentRepo paymentRepo,
            IConfiguration config,
            RoomRepo roomRepo,
            VNPayService vnPayService,
            ContactMessageRepo contactMessageRepo, // THÊM DÒNG NÀY
            EmailService emailService)             // THÊM DÒNG NÀY)
        {
            _reservationRepo = reservationRepo;
            _itemRepo = itemRepo;
            _config = config;
            _paymentRepo = paymentRepo;
            _roomRepo = roomRepo;
            _vnPayService = vnPayService;
            _contactMessageRepo = contactMessageRepo; // GÁN BIẾN
            _emailService = emailService;             // GÁN BIẾN
            _dbContext = null!; // not used directly, kept for future use
        }

        // ─── Hiển thị trang chọn thanh toán ───────────────────────────────────
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<ReservationCartItem>>("ReservationCart");
            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            return View(cart);
        }

        // ─── Tạo Reservation từ giỏ hàng ──────────────────────────────────────
        private Reservation? CreateReservation()
        {
            var cart = HttpContext.Session.GetObject<List<ReservationCartItem>>("ReservationCart");
            if (cart == null || !cart.Any()) return null;

            var reservation = new Reservation
            {
                UserId    = User.Identity!.Name!,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = cart.Sum(i => i.TotalPrice()),
                Status    = "Pending"
            };

            var response = _reservationRepo.Add(reservation).Result;
            if (response.Error) return null;

            foreach (var item in cart)
            {
                var ri = new ReservationItem
                {
                    ReservationId = reservation.Id,
                    RoomId        = item.RoomId,
                    PricePerNight = item.PricePerNight,
                    CheckIn       = item.CheckIn,
                    CheckOut      = item.CheckOut,
                    Quantity      = item.Quantity,
                    Nights        = item.Nights(),
                    TotalPrice    = item.TotalPrice(),
                };
                _itemRepo.Add(ri).Wait();
            }
            return reservation;
        }

        // ─── Thanh toán tiền mặt ──────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CashPayment()
        {
            var reservation = CreateReservation();
            if (reservation == null) return RedirectToAction("Index", "Cart");

            var cart   = HttpContext.Session.GetObject<List<ReservationCartItem>>("ReservationCart")!;
            var amount = cart.Sum(x => x.TotalPrice());

            await _paymentRepo.Add(new Payment
            {
                ResverationId = reservation.Id,
                Amount        = amount,
                PaymentMethod = "Cash",
                PaymentDate   = DateTime.UtcNow
            });

            reservation.Status = "Chờ thanh toán tại khách sạn";
            await SendConfirmationAndMessage(reservation.Id, "Tiền mặt", amount * 24000m);

            HttpContext.Session.Remove("ReservationCart");
            return RedirectToAction("Success", new { reservationId = reservation.Id });
        }

        // ─── Thanh toán VNPay ─────────────────────────────────────────────────
        [HttpPost]
        public IActionResult VNPayPayment()
        {
            var cart = HttpContext.Session.GetObject<List<ReservationCartItem>>("ReservationCart");
            if (cart == null || !cart.Any()) return RedirectToAction("Index", "Cart");

            var reservation = CreateReservation();
            if (reservation == null) return RedirectToAction("Index", "Cart");

            // Lưu reservationId vào session để dùng khi VNPay callback
            HttpContext.Session.SetInt32("PendingReservationId", reservation.Id);
            HttpContext.Session.CommitAsync().Wait(); // đảm bảo session được lưu trước khi redirect

            decimal totalVND = reservation.TotalAmount * 24000m; // USD → VND demo
            // orderInfo không dấu, không ký tự đặc biệt
            string orderInfo = $"Thanh toan dat phong {reservation.Id}";

            string paymentUrl = _vnPayService.CreatePaymentUrl(reservation.Id, totalVND, orderInfo);
            return Redirect(paymentUrl);
        }

        // ─── VNPay callback (Return URL) ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VNPayReturn()
        {
            var query = HttpContext.Request.Query;

            // Lấy các thông tin cơ bản từ VNPay trả về
            string responseCode = query["vnp_ResponseCode"].ToString();
            string txnRef       = query["vnp_TxnRef"].ToString();
            string vnpAmountStr = query["vnp_Amount"].ToString();

            // Parse reservationId từ txnRef (chỉ là số nguyên)
            int.TryParse(txnRef, out int reservationId);

            // Xác thực chữ ký
            bool isValid = _vnPayService.ValidateSignature(query);

            // Nếu chữ ký sai hoặc thanh toán thất bại
            if (!isValid || responseCode != "00")
            {
                // Xóa reservation pending nếu có
                if (reservationId > 0)
                {
                    var failed = _reservationRepo.Get(reservationId).Result.Data;
                    if (failed != null && failed.Status == "Pending")
                        await _reservationRepo.Delete(failed);
                }

                string reason = !isValid ? "Chữ ký không hợp lệ." : $"Mã lỗi VNPay: {responseCode}";
                TempData["ErrorMessage"] = $"Thanh toán VNPay thất bại. {reason} Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }

            // Thanh toán thành công — tìm reservation
            var reservation = _reservationRepo.Get(reservationId).Result.Data;
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }

            // Tránh ghi payment 2 lần nếu VNPay callback nhiều lần
            if (reservation.Status != "Đã thanh toán (VNPay)")
            {
                decimal vnpAmount = 0;
                if (!string.IsNullOrEmpty(vnpAmountStr))
                    vnpAmount = long.Parse(vnpAmountStr) / 100m;

                await _paymentRepo.Add(new Payment
                {
                    ResverationId = reservationId,
                    Amount        = vnpAmount > 0 ? vnpAmount : reservation.TotalAmount,
                    PaymentMethod = "VNPay",
                    PaymentDate   = DateTime.UtcNow
                });

                reservation.Status = "Đã thanh toán (VNPay)";
                await SendConfirmationAndMessage(reservationId, "Chuyển khoản VNPay", vnpAmount > 0 ? vnpAmount : reservation.TotalAmount);
            }

            HttpContext.Session.Remove("ReservationCart");
            HttpContext.Session.Remove("PendingReservationId");

            return RedirectToAction("Success", new { reservationId });
        }

        // ─── Stripe ───────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateStripeSession()
        {
            var reservation = CreateReservation();
            if (reservation == null) return RedirectToAction("Index", "Cart");

            var cart = HttpContext.Session.GetObject<List<ReservationCartItem>>("ReservationCart")!;

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode       = "payment",
                SuccessUrl = Url.Action("Success", "Payment", new { reservationId = reservation.Id }, Request.Scheme),
                CancelUrl  = Url.Action("Index", "Cart", null, Request.Scheme),
                Metadata   = new Dictionary<string, string>
                {
                    { "reservation_id", reservation.Id.ToString() },
                    { "user_id", User?.Identity?.Name ?? "guest" }
                },
                LineItems = cart.Select(item => new Stripe.Checkout.SessionLineItemOptions
                {
                    Quantity  = item.Quantity,
                    PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.TotalPrice() * 100),
                        Currency   = "usd",
                        ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{item.RoomName} ({item.CheckIn:yyyy-MM-dd} → {item.CheckOut:yyyy-MM-dd})"
                        }
                    }
                }).ToList()
            };

            var service = new Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options);
            return Redirect(session.Url);
        }

        // ─── Trang thành công ─────────────────────────────────────────────────
        public IActionResult Success(int reservationId)
        {
            var reservation = _reservationRepo.Get(reservationId).Result.Data;
            if (reservation == null) return RedirectToAction("Index", "Home");

            // Nếu thanh toán Stripe chưa ghi payment
            if (reservation.Payment == null)
            {
                _paymentRepo.Add(new Payment
                {
                    ResverationId = reservationId,
                    Amount        = reservation.TotalAmount,
                    PaymentMethod = "Stripe",
                    PaymentDate   = DateTime.UtcNow
                }).Wait();

                reservation.Status = "Đã thanh toán (Stripe)";
                _reservationRepo.Update(reservation).Wait();
            }

            HttpContext.Session.Remove("ReservationCart");
            return View();
        }

        // ─── Reset tất cả đặt phòng (Admin) ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ResetBookings()
        {
            var allItems = _itemRepo.GetAll().Result.Data;
            foreach (var item in allItems)
                await _itemRepo.Delete(item);

            var allReservations = _reservationRepo.GetAll().Result.Data;
            foreach (var res in allReservations)
                await _reservationRepo.Delete(res);

            var allRooms = await _roomRepo.GetAllRooms();
            foreach (var room in allRooms.Data)
            {
                room.IsAvailable = true;
                await _roomRepo.Update(room);
            }

            TempData["SuccessMessage"] = "Đã đặt lại tất cả đặt phòng thành công!";
            return RedirectToAction("Index", "Home");
        }

        // ─── Hàm Hỗ trợ: Bắn Email và Lưu MyMessages ──────────────────────────
        private async Task SendConfirmationAndMessage(int reservationId, string method, decimal amount)
        {
            // Lấy email khách hàng (Do Identity lưu Email vào Name)
            string userEmail = User.Identity?.Name ?? "guest";

            // 1. SỬA LẠI THÊM CHỮ "VNĐ" Ở DÒNG NÀY
            var sysMessage = new ContactMessage
            {
                FullName = "Hệ thống Hoang Booking",
                Email = "noreply@hoangbooking.somee.com",
                Message = $"CHÚC MỪNG: Bạn đã đặt phòng thành công (Mã đơn: #{reservationId}). Tổng thanh toán: {amount:N0} VNĐ qua {method}.",
                UserId = userEmail,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                IsUserRead = false
            };
            await _contactMessageRepo.Add(sysMessage);

            // 2. Bắn Email xác nhận
            if (userEmail != "guest")
            {
                string subject = $"Hoang Booking - Xác nhận đơn đặt phòng #{reservationId}";
                // 3. SỬA LẠI THÊM CHỮ "VNĐ" Ở DÒNG TỔNG TIỀN NÀY NỮA LÀ XONG
                string body = $"<h3>Cảm ơn bạn đã lựa chọn Hoang Booking!</h3>" +
                              $"<p>Mã đặt phòng của bạn là: <b>#{reservationId}</b></p>" +
                              $"<p>Phương thức thanh toán: <b>{method}</b></p>" +
                              $"<p>Tổng tiền: <b>{amount:N0} VNĐ</b></p>" +
                              $"<p>Vui lòng kiểm tra mục MyMessages trên website để xem chi tiết.</p>";
                try
                {
                    await _emailService.SendEmailAsync(userEmail, subject, body);
                }
                catch
                {
                    // Đặt try-catch để nếu cấu hình mail sai, web vẫn không bị sập luồng thanh toán
                }
            }
        }
    }
}
