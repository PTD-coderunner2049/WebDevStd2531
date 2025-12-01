namespace WebDevStd2531.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }

        public required double Price { get; set; }
        public required int Stock { get; set; }
        public double? Discount { get; set; }
        public double? Tax { get; set; }
        public required int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<OrderProduct>? OrderProducts { get; set; }
        public required string ImageUrl { get; set; }
        public ICollection<ProductOption>? AvailableOptions { get; set; } // Black/small/big/etc, for description show in cart
    }
}
