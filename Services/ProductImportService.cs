using System.Text.Json;
using App.Data;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Services
{
    public class ProductImportService
    {
        private readonly ApplicationDbContext _context;

        public ProductImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ImportProductsAsync(string jsonPath)
        {
            // Проверяем существует ли файл
            if (!File.Exists(jsonPath))
                return;

            // Читаем JSON
            var json = await File.ReadAllTextAsync(jsonPath);

            // Десериализация
            var products = JsonSerializer.Deserialize<List<ProductImportModel>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (products == null)
                return;

            // Удаляем старые товары
            _context.Products.RemoveRange(_context.Products);

            await _context.SaveChangesAsync();

            // Добавляем новые товары
            foreach (var item in products)
            {
                // CATEGORY
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == item.Category);

                if (category == null)
                {
                    category = new Category
                    {
                        Name = item.Category,
                        Description = item.Category
                    };

                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                // BRAND
                var brand = await _context.Brands
                    .FirstOrDefaultAsync(b => b.Name == item.Brand);

                if (brand == null)
                {
                    brand = new Brand
                    {
                        Name = item.Brand,
                        Country = "Unknown"
                    };

                    _context.Brands.Add(brand);
                    await _context.SaveChangesAsync();
                }

                // PRODUCT
                var product = new Product
                {
                    Article = item.Article,
                    Name = item.Name,
                    Description = item.Description,
                    ShortDescription = item.ShortDescription,
                    Price = item.Price,
                    Discount = item.Discount,
                    Stock = item.Stock,
                    ImageUrl = item.ImageUrl,

                    CategoryId = category.Id,
                    BrandId = brand.Id,

                    IsActive = true
                };

                _context.Products.Add(product);
            }

            await _context.SaveChangesAsync();
        }
    }
}