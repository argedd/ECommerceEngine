using ECommerceApi.Data;
using ECommerceApi.Dtos;
using ECommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;

    public OrderService(ApplicationDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public async Task<OrderResponseDto> CheckoutAsync(int userId)
    {
        // 1. Obtener carrito actual desde Redis
        var cart = await _cartService.GetCartAsync(userId.ToString());
        if (cart == null || !cart.Items.Any())
        {
            throw new BadHttpRequestException("El carrito de compras está vacío.");
        }

        // 2. Iniciar Transacción Atómica en SQL Server
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var orderItems = new List<OrderDetail>();

            foreach (var cartItem in cart.Items)
            {
                // Buscar el producto con bloqueo de entidad para control de concurrencia
                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product == null || !product.IsActive)
                {
                    throw new BadHttpRequestException($"El producto '{cartItem.ProductName}' ya no está disponible.");
                }

                if (product.Stock < cartItem.Quantity)
                {
                    throw new BadHttpRequestException($"Stock insuficiente para '{product.Name}'. Stock actual: {product.Stock}");
                }

                // Descontar inventario
                product.Stock -= cartItem.Quantity;

                // Crear ítem de la orden
                orderItems.Add(new OrderDetail
                {
                    ProductId = product.Id,
                    Product = product,
                    UnitPrice = product.Price,
                    Quantity = cartItem.Quantity
                });
            }

            // 3. Crear la Orden principal
            var order = new Order
            {
                UserId = userId,
                TotalAmount = cart.TotalAmount,
                Status = OrderStatus.Completed,
                OrderDetails = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 4. Confirmar transacción de base de datos
            await transaction.CommitAsync();

            // 5. Limpiar y eliminar el carrito de Redis una vez procesada la compra
            await _cartService.DeleteCartAsync(userId.ToString());

            // 6. Retornar DTO de respuesta
            return MapToDto(order);
        }
        catch
        {
            // Revertir todos los cambios si ocurre cualquier fallo
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        return order == null ? null : MapToDto(order);
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        var itemDtos = order.OrderDetails.Select(i => new OrderItemResponseDto(
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.UnitPrice,
            i.Quantity,
            i.UnitPrice * i.Quantity
        ));

        return new OrderResponseDto(
            order.Id,
            order.CreatedAt,
            order.TotalAmount,
            order.Status.ToString(),
            itemDtos
        );
    }
}