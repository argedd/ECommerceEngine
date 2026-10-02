using System.Text.Json;
using ECommerceApi.Dtos;
using ECommerceApi.Repositories;
using StackExchange.Redis;

namespace ECommerceApi.Services;

public class CartService : ICartService
{
    private readonly IDatabase _redisDb;
    private readonly IProductReadRepository _productRepository;
    private readonly TimeSpan _cartTimeToLive = TimeSpan.FromDays(30); // El carrito expira tras 30 días de inactividad

    public CartService(IConnectionMultiplexer redis, IProductReadRepository productRepository)
    {
        _redisDb = redis.GetDatabase();
        _productRepository = productRepository;
    }

    private string GetCartKey(string userId) => $"cart:{userId}";

    public async Task<ShoppingCartDto> GetCartAsync(string userId)
    {
        var data = await _redisDb.StringGetAsync(GetCartKey(userId));
        
        if (data.IsNullOrEmpty)
        {
            return new ShoppingCartDto(userId, new List<CartItemDto>());
        }

        var items = JsonSerializer.Deserialize<List<CartItemDto>>(data.ToString());
        return new ShoppingCartDto(userId, items ?? new List<CartItemDto>());
    }

    public async Task<ShoppingCartDto> AddItemAsync(string userId, AddToCartDto request)
    {
        // 1. Validar que el producto existe en la BD y obtener su precio oficial
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new BadHttpRequestException($"El producto con ID {request.ProductId} no existe.");
        }

        if (product.Stock < request.Quantity)
        {
            throw new BadHttpRequestException($"Stock insuficiente para el producto '{product.Name}'. Stock disponible: {product.Stock}");
        }

        // 2. Obtener carrito actual
        var cart = await GetCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem != null)
        {
            // Actualizar cantidad acumulada
            int newQuantity = existingItem.Quantity + request.Quantity;
            
            if (product.Stock < newQuantity)
            {
                throw new BadHttpRequestException($"No puedes agregar {request.Quantity} unidades más. Excede el stock disponible ({product.Stock}).");
            }

            cart.Items.Remove(existingItem);
            cart.Items.Add(existingItem with { Quantity = newQuantity, Price = product.Price });
        }
        else
        {
            // Agregar nuevo ítem
            cart.Items.Add(new CartItemDto(product.Id, product.Name, product.Price, request.Quantity));
        }

        // 3. Persistir en Redis con expiración (TTL)
        await SaveCartAsync(userId, cart.Items);

        return cart;
    }

    public async Task<ShoppingCartDto> UpdateQuantityAsync(string userId, UpdateQuantityDto request)
    {
        var cart = await GetCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem == null)
        {
            throw new BadHttpRequestException("El producto no se encuentra en el carrito.");
        }

        if (request.Quantity <= 0)
        {
            return await RemoveItemAsync(userId, request.ProductId);
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product != null && product.Stock < request.Quantity)
        {
            throw new BadHttpRequestException($"Stock insuficiente. Solo hay {product.Stock} unidades disponibles.");
        }

        cart.Items.Remove(existingItem);
        cart.Items.Add(existingItem with { Quantity = request.Quantity });

        await SaveCartAsync(userId, cart.Items);

        return cart;
    }

    public async Task<ShoppingCartDto> RemoveItemAsync(string userId, int productId)
    {
        var cart = await GetCartAsync(userId);
        cart.Items.RemoveAll(i => i.ProductId == productId);

        await SaveCartAsync(userId, cart.Items);

        return cart;
    }

    public async Task<bool> DeleteCartAsync(string userId)
    {
        return await _redisDb.KeyDeleteAsync(GetCartKey(userId));
    }

    private async Task SaveCartAsync(string userId, List<CartItemDto> items)
    {
        string json = JsonSerializer.Serialize(items);
        await _redisDb.StringSetAsync(GetCartKey(userId), json, _cartTimeToLive);
    }
}