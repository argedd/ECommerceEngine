namespace ECommerceApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer"; // Roles: Admin, Customer
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedad de navegación (1 Usuario -> N Órdenes)
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}