using Dapper;
using ECommerceApi.Data;
using ECommerceApi.Dtos;
using ECommerceApi.Models;
using ECommerceApi.Models.Categories;

namespace ECommerceApi.Repositories;

public class ProductReadRepository : IProductReadRepository
{
    private readonly DapperContext _context;

    public ProductReadRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT p.Id, p.Name, p.Description, p.Price, p.Stock,
                   c.Id, c.Name
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.Id = @Id AND p.IsActive = 1;";

        using var connection = _context.CreateConnection();

        var result = await connection.QueryAsync<Product, Category, ProductResponseDto>(
            sql,
            (product, category) => new ProductResponseDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                new CategoryDto(category.Id, category.Name)
            ),
            new { Id = id },
            splitOn: "Id"
        );

        return result.FirstOrDefault();
    }

    public async Task<PagedResultDto> GetAllAsync(int pageNumber, int pageSize, string? searchTerm = null)
    {
        // Ejecución de dos sentencias SQL en un solo comando (Cero tiempo desperdiciado en red)
        const string sql = @"
            SELECT COUNT(1) 
            FROM Products p 
            WHERE p.IsActive = 1 
              AND (@SearchTerm IS NULL OR p.Name LIKE '%' + @SearchTerm + '%');

            SELECT p.Id, p.Name, p.Description, p.Price, p.Stock,
                   c.Id, c.Name
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.IsActive = 1 
              AND (@SearchTerm IS NULL OR p.Name LIKE '%' + @SearchTerm + '%')
            ORDER BY p.Id
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var connection = _context.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            SearchTerm = searchTerm,
            Offset = (pageNumber - 1) * pageSize,
            PageSize = pageSize
        });

        int totalCount = await multi.ReadFirstAsync<int>();

        var items = multi.Read<Product, Category, ProductResponseDto>(
            (product, category) => new ProductResponseDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                new CategoryDto(category.Id, category.Name)
            ),
            splitOn: "Id"
        );

        return new PagedResultDto(items, pageNumber, pageSize, totalCount);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(int categoryId)
    {
        const string sql = @"
            SELECT p.Id, p.Name, p.Description, p.Price, p.Stock,
                   c.Id, c.Name
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.CategoryId = @CategoryId AND p.IsActive = 1
            ORDER BY p.Name;";

        using var connection = _context.CreateConnection();

        return await connection.QueryAsync<Product, Category, ProductResponseDto>(
            sql,
            (product, category) => new ProductResponseDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                new CategoryDto(category.Id, category.Name)
            ),
            new { CategoryId = categoryId },
            splitOn: "Id"
        );
    }
}