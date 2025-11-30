namespace WebDevStd2531.Models
{
    public class CartItemViewModel
    {
        // one item in the cart, CartDetail get a list of these CartItemViewModel.
        public int OrderProductId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public int MaxStock { get; set; } // Available stock for validation

        // Order Details (from OrderProduct)
        public int Quantity { get; set; }
        public double Price { get; set; } // Unit price at the time of adding to cart

        // Calculated property for display
        public double LineTotal => Quantity * Price;
    }
}