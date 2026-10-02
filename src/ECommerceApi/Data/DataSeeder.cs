using ECommerceApi.Models;
using ECommerceApi.Models.Categories;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Data;

public static class DataSeeder
{
    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Aplicar migraciones pendientes de forma automática al arrancar
        await context.Database.MigrateAsync();

        // 2. Si ya existen categorías, asumimos que el seeder ya corrió antes
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        // 3. Crear categorías iniciales
        var categories = new List<Category>
        {
            new Category 
            { 
                Name = "Electrónica y Tecnología", 
                Description = "Dispositivos móviles, computación y accesorios de alta gama" 
            },
            new Category 
            { 
                Name = "Ropa y Moda", 
                Description = "Indumentaria casual y deportiva para hombre y mujer" 
            },
            new Category 
            { 
                Name = "Hogar y Cocina", 
                Description = "Electrodomésticos y utensilios inteligentes para el hogar" 
            }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync(); // Guardamos para que SQL Server genere los IDs autoincrementales

        // 4. Crear productos de prueba asociados a las categorías creadas
        var products = new List<Product>
        {
            new Product
            {
                Name = "Laptop Gamer Ryzen 7",
                Description = "Laptop de alto rendimiento con 16GB RAM y gráficos dedicados",
                Price = 1200.00m,
                Stock = 15,
                CategoryId = categories[0].Id,
                IsActive = true
            },
            new Product
            {
                Name = "Mouse Inalámbrico Ergonómico",
                Description = "Mouse óptico con conectividad Bluetooth y USB de alta precisión",
                Price = 45.50m,
                Stock = 50,
                CategoryId = categories[0].Id,
                IsActive = true
            },
            new Product
            {
                Name = "Camisa Casual de Algodón",
                Description = "Camisa manga larga elegante y cómoda para cualquier ocasión",
                Price = 30.00m,
                Stock = 40,
                CategoryId = categories[1].Id,
                IsActive = true
            },
            new Product
            {
                Name = "Cafetera Automática Express",
                Description = "Cafetera de 15 bares para preparar espresso y capuccino en casa",
                Price = 150.00m,
                Stock = 10,
                CategoryId = categories[2].Id,
                IsActive = true
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}