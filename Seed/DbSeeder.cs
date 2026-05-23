using System.Text.Json;
using App.Models;

namespace App.Data.Seed
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Products.Any())
                return;

            var jsonPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "products.json"
            );

            if (!File.Exists(jsonPath))
                return;

            var json = File.ReadAllText(jsonPath);

            var productsData = JsonSerializer.Deserialize<List<ProductJson>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (productsData == null)
                return;

            foreach (var item in productsData)
            {
                // CATEGORY
                var category = context.Categories
                    .FirstOrDefault(c => c.Name == item.Category);

                if (category == null)
                {
                    category = new Category
                    {
                        Name = item.Category,
                        Description = item.Category
                    };

                    context.Categories.Add(category);
                    context.SaveChanges();
                }

                // BRAND
                var brand = context.Brands
                    .FirstOrDefault(b => b.Name == item.Brand);

                if (brand == null)
                {
                    brand = new Brand
                    {
                        Name = item.Brand,
                        Country = "Unknown"
                    };

                    context.Brands.Add(brand);
                    context.SaveChanges();
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

                context.Products.Add(product);
            }

            context.SaveChanges();
        }
    }

    public class ProductJson
    {
        public string Article { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public string ImageUrl { get; set; }
    }
}