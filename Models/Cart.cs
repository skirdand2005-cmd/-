namespace App.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public decimal OldTotalPrice { get; set; }

        public decimal TotalDiscount { get; set; }

        public int DiscountPercent { get; set; }

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}