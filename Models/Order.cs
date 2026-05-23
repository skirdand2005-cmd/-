using System.ComponentModel.DataAnnotations;

namespace App.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Новый";

        public string Address { get; set; } = "";

        public string? Comment { get; set; }

        // ✅ ДАТА ДОСТАВКИ
        public DateTime? DeliveryDate { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public List<OrderItem> OrderItems { get; set; } =
            new();
    }
}