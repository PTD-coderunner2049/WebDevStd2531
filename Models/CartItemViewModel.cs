namespace WebDevStd2531.Models
{
    public class CartItemViewModel
    {
        // one item in the cart, CartDetail get a list of these CartItemViewModel.
        public int OrderProductId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int MaxStock { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double? Discount { get; set; }
        public double? Tax { get; set; }
        public required string SelectedType { get; set; }
        public double LineTotal => Quantity * Price;
    }
}