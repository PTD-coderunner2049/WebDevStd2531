using System.ComponentModel.DataAnnotations;

namespace WebDevStd2531.Models
{
    public class Order
    {
        //Order "pending" is create once user add an item to cart (if not already exist),
        //and paid when user hit pay btn
        //only one order with status "Pending" per user at a time
        public int? Id { get; set; }
        [StringLength(450)]// chiều dài bằng userid trong bảng user dùng cho identity
        public string UserId { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public ICollection<OrderProduct>? OrderProducts { get; set; }
    }
}
