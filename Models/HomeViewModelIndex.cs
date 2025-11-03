namespace WebDevStd2531.Models
{
    public class HomeViewModelIndex
    {
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<GrandCategory> AllGrandCategories { get; set; } = new List<GrandCategory>();
    }
}