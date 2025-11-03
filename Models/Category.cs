namespace WebDevStd2531.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public required int GrandCategoryId { get; set; }
        public GrandCategory? GrandCategory { get; set; }
        public ICollection<Product>? Products { get; set; }
    }
}
