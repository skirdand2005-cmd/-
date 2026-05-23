using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using App.Models;

namespace App.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(user);
        }

        // GET: Редактирование профиля
        public async Task<IActionResult> Edit()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Index");

            return View(user);
        }

        // POST: Сохранение изменений
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User updatedUser)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            if (userId != updatedUser.Id)
                return BadRequest();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Index");

            // === ВАЛИДАЦИЯ ===
            if (string.IsNullOrWhiteSpace(updatedUser.FullName))
            {
                ModelState.AddModelError("FullName", "Полное имя обязательно");
                return View(user);
            }

            // Улучшенная проверка Email
            if (string.IsNullOrWhiteSpace(updatedUser.Email) || !IsValidEmail(updatedUser.Email))
            {
                ModelState.AddModelError("Email", "Введите корректный email (например: example@gmail.com, example@yandex.ru)");
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(updatedUser.Phone) || !updatedUser.Phone.StartsWith("+7"))
            {
                ModelState.AddModelError("Phone", "Телефон должен начинаться с +7");
                return View(user);
            }

            // Проверка уникальности email
            if (user.Email != updatedUser.Email &&
                await _context.Users.AnyAsync(u => u.Email == updatedUser.Email && u.Id != userId))
            {
                ModelState.AddModelError("Email", "Этот email уже занят");
                return View(user);
            }

            // Обновляем данные
            user.FullName = updatedUser.FullName;
            user.Email = updatedUser.Email;
            user.Phone = updatedUser.Phone;
            user.Address = string.IsNullOrWhiteSpace(updatedUser.Address) ? "Не указан" : updatedUser.Address;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Профиль успешно обновлён!";
            return RedirectToAction("Index");
        }

        // GET: Смена пароля
        public async Task<IActionResult> ChangePassword()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Index");

            return View();
        }

        // POST: Смена пароля
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Index");

            // Проверка текущего пароля
            if (user.PasswordHash != currentPassword)
            {
                ViewBag.Error = "Текущий пароль указан неверно";
                return View();
            }

            // Валидация нового пароля
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6 ||
                !newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
            {
                ViewBag.Error = "Пароль должен содержать минимум 6 символов, включая буквы и цифры";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Пароли не совпадают";
                return View();
            }

            user.PasswordHash = newPassword;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Пароль успешно изменён!";
            return RedirectToAction("Index");
        }


        // Вспомогательный метод для проверки email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email) return false;
            }
            catch
            {
                return false;
            }

            // Дополнительная проверка популярных доменов
            var allowedDomains = new[] { "gmail.com", "yandex.ru", "yandex.com", "mail.ru", "bk.ru", "list.ru", "inbox.ru", "rambler.ru", "hotmail.com", "outlook.com" };
            var domain = email.Split('@').Last().ToLower();

            return allowedDomains.Contains(domain) || domain.Contains(".");
        }
    }
}