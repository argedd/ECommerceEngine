namespace ECommerceApi.Dtos;

public record CartItemDto(
    int ProductId,
    string ProductName,
    decimal Price,
    int Quantity
);

public record ShoppingCartDto(
    string UserId,
    List<CartItemDto> Items
)
{
    public decimal TotalAmount => Items.Sum(item => item.Price * item.Quantity);
}

public record AddToCartDto(
    int ProductId,
    int Quantity
);

public record UpdateQuantityDto(
    int ProductId,
    int Quantity
);