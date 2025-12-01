namespace WebDevStd2531.Models
{
    public class OrderProduct
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int? Quantity { get; set; }
        public double? Price { get; set; }
        public required string Type { get; set; } // Black/small/big/etc, for description show in cart
    }
}
