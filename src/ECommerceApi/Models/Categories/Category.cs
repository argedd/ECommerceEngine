namespace ECommerceApi.Models.Categories;

public class Category
{
    public int Id{ get; set;}
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedad de navegación (1 Categoría -> N Productos)
    public ICollection<Product> Products { get; set; } = new List<Product>();

}