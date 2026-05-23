using Microsoft.AspNetCore.Mvc;
using App.Data;
using Microsoft.EntityFrameworkCore;
using App.Models;

namespace App.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Product (Каталог)
        public async Task<IActionResult> Index(
        string category = null,
        string search = null,
        string sort = "popular")
        {
            IQueryable<Product> products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive);

            // ===== КАТЕГОРИЯ =====
            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p => p.Category != null &&
                                               p.Category.Name == category);
            }

            // ===== ПОИСК =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.Name.Contains(search) ||
                    (p.ShortDescription != null &&
                     p.ShortDescription.Contains(search)));
            }

            // ===== СОРТИРОВКА =====
            products = sort switch
            {
                "price_desc" => products.OrderByDescending(p => p.Price),

                "price_asc" => products.OrderBy(p => p.Price),

                "discount" => products.OrderByDescending(p => p.Discount),

                "new" => products.OrderByDescending(p => p.Id),

                _ => products.OrderBy(p => p.Name)
            };

            var model = await products.ToListAsync();

            // DEBUG ПРОВЕРКА
            foreach (var p in model)
            {
                Console.WriteLine(
                    $"{p.Name} | Price={p.Price} | Discount={p.Discount} | Short={p.ShortDescription}"
                );
            }

            ViewBag.TotalProducts = model.Count;
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentCategory = category;
            ViewBag.Search = search;

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(model);
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}