using ECommerceApi.Dtos;

namespace ECommerceApi.Services;

public interface IOrderService
{
    Task<OrderResponseDto> CheckoutAsync(int userId);
    Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(int userId);
    Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId);
}