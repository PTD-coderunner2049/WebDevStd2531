namespace WebDevStd2531.Models
{
    public class ProductDetailModel
    {
        public required Product MainProduct { get; set; }
        public List<Product>? RelatedProds { get; set; }
    }
}