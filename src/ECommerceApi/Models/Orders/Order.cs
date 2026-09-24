namespace ECommerceApi.Models;

public enum OrderStatus
{
    Pending,
    Completed,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedad de navegación (1 Orden -> N Detalles)
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}