using ECommerceApi.Dtos;

namespace ECommerceApi.Repositories;

public interface IProductReadRepository
{
    Task<ProductResponseDto?> GetByIdAsync(int id);
    Task<PagedResultDto> GetAllAsync(int pageNumber, int pageSize, string? searchTerm = null);
    Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(int categoryId);
}