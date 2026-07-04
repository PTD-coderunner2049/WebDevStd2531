namespace CatalogService.Models;

public class ProductOption
{
    public int Id { get; set; }
    public required string Value { get; set; }
    public required int ProductId { get; set; }
    public Product? Product { get; set; }
}
