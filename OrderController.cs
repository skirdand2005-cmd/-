using Microsoft.AspNetCore.Mvc;
using App.Data;
using App.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace App.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // СТРАНИЦА ОФОРМЛЕНИЯ
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Login", "Account");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            decimal total = cart.CartItems.Sum(x =>
                (x.Product?.Price ?? 0m) * x.Quantity);

            ViewBag.Total = total;

            return View(cart);
        }

        // =========================
        // СОЗДАНИЕ ЗАКАЗА
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateOrder(
        string address,
        string comment,
        DateTime? deliveryDate,
        string deliveryTime)
        {
            if (!User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Login", "Account");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))   
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            decimal totalAmount = cart.CartItems.Sum(x =>
                (x.Product?.Price ?? 0m) * x.Quantity);

            // СОБИРАЕМ ДАТУ + ВРЕМЯ
            DateTime? finalDeliveryDate = null;

            if (deliveryDate.HasValue &&
                !string.IsNullOrEmpty(deliveryTime))
            {
                var timeParts = deliveryTime.Split(':');

                int hours = int.Parse(timeParts[0]);
                int minutes = int.Parse(timeParts[1]);

                finalDeliveryDate = new DateTime(
                    deliveryDate.Value.Year,
                    deliveryDate.Value.Month,
                    deliveryDate.Value.Day,
                    hours,
                    minutes,
                    0,
                    DateTimeKind.Utc);
            }

            var order = new Order
            {
                UserId = userId,

                TotalAmount = totalAmount,

                Status = "Новый",

                Address = string.IsNullOrWhiteSpace(address)
                    ? "Адрес не указан"
                    : address,

                Comment = string.IsNullOrWhiteSpace(comment)
                    ? "Без комментария"
                    : comment,

                CreatedAt = DateTime.UtcNow,

                DeliveryDate = finalDeliveryDate
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            foreach (var item in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceAtPurchase = item.Product?.Price ?? 0m
                };

                _context.OrderItems.Add(orderItem);
            }

            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { id = order.Id });
        }

        // =========================
        // УСПЕХ
        // =========================
        public IActionResult Success(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // =========================
        // ОТМЕНА
        // =========================
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Login", "Account");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId);

            if (order == null)
                return NotFound();

            if (order.Status != "Новый")
            {
                TempData["Error"] =
                    "Этот заказ нельзя отменить.";

                return RedirectToAction("Index", "Profile");
            }

            order.Status = "Отменён";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Заказ №{id} отменён.";

            return RedirectToAction("Index", "Profile");
        }
    }
}