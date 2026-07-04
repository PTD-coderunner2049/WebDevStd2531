namespace OrderService.Models;

public class OrderProduct
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int? Quantity { get; set; }
    public double? Price { get; set; }
    public required string Type { get; set; }
}
