namespace App.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Article { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        public decimal Price { get; set; }

        public decimal Discount { get; set; } = 0;

        public int Stock { get; set; }

        public string Description { get; set; } = string.Empty;

        public string ShortDescription { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

    }
}