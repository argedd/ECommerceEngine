namespace ECommerceApi.Models;



public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    
    // Foto/Snapshot del precio al momento de comprar (evita que cambios en el catálogo alteren facturas pasadas)
    public decimal UnitPrice { get; set; }
}