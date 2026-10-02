namespace ECommerceApi.Dtos;

public record CategoryDto(
    int Id,
    string Name
);

public record ProductResponseDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    CategoryDto Category
);

public record PagedResultDto(
    IEnumerable<ProductResponseDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount
);