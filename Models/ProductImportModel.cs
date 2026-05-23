namespace App.Models
{
    public class ProductImportModel
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