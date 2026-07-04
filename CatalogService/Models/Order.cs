using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models;

public class Order
{
    public int? Id { get; set; }
    [StringLength(450)]
    public string UserId { get; set; } = null!;
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public ICollection<OrderProduct>? OrderProducts { get; set; }
}
