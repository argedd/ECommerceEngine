using ECommerceApi.Dtos;

namespace ECommerceApi.Services;

public interface ICartService
{
    Task<ShoppingCartDto> GetCartAsync(string userId);
    Task<ShoppingCartDto> AddItemAsync(string userId, AddToCartDto request);
    Task<ShoppingCartDto> UpdateQuantityAsync(string userId, UpdateQuantityDto request);
    Task<ShoppingCartDto> RemoveItemAsync(string userId, int productId);
    Task<bool> DeleteCartAsync(string userId);
}