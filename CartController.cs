using Microsoft.AspNetCore.Mvc;
using App.Data;
using App.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace App.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "GuestCart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : null;
        }

        // ====================== ПОЛУЧЕНИЕ КОРЗИНЫ ======================
        private async Task<Cart> GetOrCreateCartAsync()
        {
            var userId = GetCurrentUserId();

            if (userId.HasValue) // Авторизованный
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId.Value };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
                return cart;
            }
            else // Гость
            {
                var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);
                if (cart == null)
                {
                    cart = new Cart { UserId = 0 };
                    HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
                }
                return cart;
            }
        }
        // ====================== INDEX ======================
        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCartAsync();

            // Для гостя подгружаем товары
            if (cart.UserId == 0 && cart.CartItems.Any())
            {
                var ids = cart.CartItems.Select(x => x.ProductId).ToList();

                var products = await _context.Products
                    .Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var item in cart.CartItems)
                {
                    if (products.TryGetValue(item.ProductId, out var p))
                        item.Product = p;
                }
            }

            // ==========================
            // РАСЧЁТ СКИДОК
            // ==========================

            decimal totalCurrent = 0;
            decimal totalOld = 0;

            foreach (var item in cart.CartItems)
            {
                decimal currentPrice = item.Product.Price;

                decimal discount = item.Product.Discount;

                decimal oldPrice =
                    currentPrice / (1 - discount / 100m);

                totalCurrent += currentPrice * item.Quantity;

                totalOld += oldPrice * item.Quantity;
            }

            decimal totalDiscount =
                totalOld - totalCurrent;

            int discountPercent =
                totalOld > 0
                ? (int)Math.Round(totalDiscount / totalOld * 100)
                : 0;

            // Передаём в View
            ViewBag.TotalCurrent = totalCurrent;

            ViewBag.TotalOld = totalOld;

            ViewBag.TotalDiscount = totalDiscount;

            ViewBag.DiscountPercent = discountPercent;

            return View(cart);
        }

        // ====================== ADD TO CART ======================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id)
        {
            var cart = await GetOrCreateCartAsync();

            if (cart.UserId == 0) // Гость
            {
                var existing = cart.CartItems.FirstOrDefault(x => x.ProductId == id);

                if (existing != null)
                {
                    existing.Quantity++;
                }
                else
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                        return BadRequest("Товар не найден");

                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = id,
                        Quantity = 1,
                        Product = product
                    });
                }

                // ← КРИТИЧНОЕ ИСПРАВЛЕНИЕ
                foreach (var item in cart.CartItems)
                {
                    if (item.ProductId == 0 && item.Product != null)
                        item.ProductId = item.Product.Id;
                }

                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            else // Авторизованный
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == id);

                if (cartItem != null)
                    cartItem.Quantity++;
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = id,
                        Quantity = 1
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        // AJAX: Изменение количества товара
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int change)
        {
            var userId = GetCurrentUserId();

            // ================= АВТОРИЗОВАННЫЙ =================
            if (userId.HasValue && userId.Value != 0)
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                    return Json(new { success = false, message = "Корзина не найдена" });

                var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == id);

                if (item == null)
                    return Json(new { success = false, message = "Товар не найден" });

                item.Quantity += change;

                if (item.Quantity < 1)
                    item.Quantity = 1;

                await _context.SaveChangesAsync();
            }

            // ================= ГОСТЬ =================
            else
            {
                var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);

                if (cart == null)
                    return Json(new { success = false, message = "Корзина не найдена" });

                // КРИТИЧНОЕ ИСПРАВЛЕНИЕ
                foreach (var cartItem in cart.CartItems)
                {
                    if (cartItem.ProductId == 0 && cartItem.Product != null)
                        cartItem.ProductId = cartItem.Product.Id;
                }

                var item = cart.CartItems.FirstOrDefault(x => x.ProductId == id);

                if (item == null)
                    return Json(new { success = false, message = $"Товар {id} не найден в сессии" });

                item.Quantity += change;

                if (item.Quantity < 1)
                    item.Quantity = 1;

                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }

            return Json(new
            {
                success = true
            });
        }

        // ====================== REMOVE ======================
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetCurrentUserId();

            if (userId.HasValue)
            {
                var item = await _context.CartItems.FindAsync(id);
                if (item != null)
                {
                    var cart = await _context.Carts.FindAsync(item.CartId);
                    if (cart?.UserId == userId.Value)
                    {
                        _context.CartItems.Remove(item);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);
                if (cart != null)
                {
                    var item = cart.CartItems.FirstOrDefault(x => x.Id == id);
                    if (item != null)
                        cart.CartItems.Remove(item);

                    HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
                }
            }

            return RedirectToAction("Index");
        }

        // ====================== GET COUNT ======================
        public IActionResult GetCount()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var count = _context.CartItems
                    .Where(ci => ci.Cart.UserId == userId.Value)
                    .Sum(ci => ci.Quantity);
                return Content(count.ToString());
            }
            else
            {
                var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);
                int count = cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
                return Content(count.ToString());
            }
        }
    }
}