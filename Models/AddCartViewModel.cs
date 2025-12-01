
namespace WebDevStd2531.Models
{
    public class AddCartViewModel
    {
        public int ProductId { get; set; }
        public required string SelectedType { get; set; }
        public int Quantity { get; set; }
    }
}