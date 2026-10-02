using ECommerceApi.Dtos;
using ECommerceApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IProductReadRepository _productRepository;

    public CatalogController(IProductReadRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// 
    /// Consulta paginada de productos con opción de búsqueda por nombre.
    /// 
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(new { message = "Los parámetros de paginación deben ser enteros mayores a cero." });
        }

        var result = await _productRepository.GetAllAsync(pageNumber, pageSize, search);
        return Ok(result);
    }

    /// 
    /// Obtiene un producto específico por su ID.
    /// 
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Producto con ID {id} no encontrado." });
        }

        return Ok(product);
    }

    /// 
    /// Obtiene todos los productos de una categoría específica.
    /// 
    [HttpGet("category/{categoryId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByCategory(int categoryId)
    {
        var products = await _productRepository.GetByCategoryIdAsync(categoryId);
        return Ok(products);
    }
}