using ECommerceApi.Models.Categories;

namespace ECommerceApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relación con Categoría
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Token de Concurrencia Optimista para evitar sobreventa en compras simultáneas
    public byte[] RowVersion { get; set; } = [];
}