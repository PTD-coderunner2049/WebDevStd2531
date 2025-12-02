namespace WebDevStd2531.Models
{
    public class ProductDetailModel
    {
        //OBSOLETE CLASS : NOW WE JUST PASS THE PRODUCT ITSELF TO THE VIEW (I did some optimization, yey)
        // I LEFT IT HERE IN CASE I WANT TO REVERT BACK TO THIS METHOD LATER
        public required Product MainProduct { get; set; }
        public List<Product>? RelatedProds { get; set; }
    }
}