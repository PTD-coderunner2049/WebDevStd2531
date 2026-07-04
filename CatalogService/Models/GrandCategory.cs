namespace CatalogService.Models;

public class GrandCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Category>? Categories { get; set; }
}
