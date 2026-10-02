namespace ECommerceApi.Dtos;

public record OrderItemResponseDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal SubTotal
);

public record OrderResponseDto(
    int Id,
    DateTime CreatedAt,
    decimal TotalAmount,
    string Status,
    IEnumerable<OrderItemResponseDto> Items
);