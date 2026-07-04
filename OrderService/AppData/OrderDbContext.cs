using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.AppData;

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderProduct> OrderProducts { get; set; } = null!;

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }
}
