using Microsoft.AspNetCore.Mvc;
using App.Data;
using App.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace App.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);

            if (user == null)
            {
                ViewBag.Error = "Неверный email или пароль";
                return View();
            }

            // Создаём claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string fullName, string phone)
        {
            // Валидация Email
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                ViewBag.Error = "Введите корректный email";
                return View();
            }

            // Валидация телефона
            if (string.IsNullOrWhiteSpace(phone) || !phone.StartsWith("+7"))
            {
                ViewBag.Error = "Телефон должен начинаться с +7";
                return View();
            }

            // Валидация пароля
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6 ||
                !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            {
                ViewBag.Error = "Пароль должен содержать минимум 6 символов, включая буквы и цифры";
                return View();
            }

            // Проверка уникальности
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Пользователь с таким email уже существует";
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ViewBag.Error = "Имя пользователя уже занято";
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Phone == phone))
            {
                ViewBag.Error = "Этот номер телефона уже зарегистрирован";
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = password,
                FullName = fullName,
                Phone = phone,
                Address = "Не указан",
                Role = "customer"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Регистрация прошла успешно! Теперь войдите в аккаунт.";
            return View("Login");
        }

        // Выход
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }
    }
}