namespace WebDevStd2531.Models
{
    public class GrandCategory
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public ICollection<Category>? Categories { get; set; }
    }
}
