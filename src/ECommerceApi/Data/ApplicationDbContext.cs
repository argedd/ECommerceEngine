using ECommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // 1. Configuración de Entidad: User
        // ==========================================
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(150);

            // Índice Único en Email para acelerar búsquedas en Login
            builder.HasIndex(u => u.Email)
                   .IsUnique();

            builder.Property(u => u.PasswordHash)
                   .IsRequired();

            builder.Property(u => u.Role)
                   .IsRequired()
                   .HasMaxLength(20);
        });

        // ==========================================
        // 2. Configuración de Entidad: Category
        // ==========================================
        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("Categories");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Description)
                   .HasMaxLength(500);
        });

        // ==========================================
        // 3. Configuración de Entidad: Product
        // ==========================================
        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(p => p.Description)
                   .HasMaxLength(1000);

            // Definición explícita de precisión monetaria
            builder.Property(p => p.Price)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.Stock)
                   .IsRequired();

            // Bloqueo de Concurrencia Optimista (RowVersion en SQL Server)
            builder.Property(p => p.RowVersion)
                   .IsRowVersion();

            // Relación 1:N (Categoría -> Productos)
            builder.HasOne(p => p.Category)
                   .WithMany(c => c.Products)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Evita eliminar una categoría con productos activos
        });

        // ==========================================
        // 4. Configuración de Entidad: Order
        // ==========================================
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.TotalAmount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(o => o.Status)
                   .HasConversion<string>() // Guarda el Enum como texto ("Pending", "Completed") en la BD
                   .HasMaxLength(20);

            // Relación 1:N (Usuario -> Órdenes)
            builder.HasOne(o => o.User)
                   .WithMany(u => u.Orders)
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // 5. Configuración de Entidad: OrderDetail
        // ==========================================
        modelBuilder.Entity<OrderDetail>(builder =>
        {
            builder.ToTable("OrderDetails");
            builder.HasKey(od => od.Id);

            builder.Property(od => od.UnitPrice)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            // Relación 1:N (Orden -> Detalles)
            builder.HasOne(od => od.Order)
                   .WithMany(o => o.OrderDetails)
                   .HasForeignKey(od => od.OrderId)
                   .OnDelete(DeleteBehavior.Cascade); // Si se elimina la orden, se borra su detalle

            // Relación 1:N (Producto -> Detalles)
            builder.HasOne(od => od.Product)
                   .WithMany()
                   .HasForeignKey(od => od.ProductId)
                   .OnDelete(DeleteBehavior.Restrict); // Protege el historial de compras si un producto se desactiva
        });
    }
}