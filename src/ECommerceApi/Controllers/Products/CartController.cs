using System.Security.Claims;
using ECommerceApi.Dtos;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Obtiene el carrito actual del usuario autenticado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _cartService.GetCartAsync(GetUserId());
        return Ok(cart);
    }

    /// <summary>
    /// Agrega un producto al carrito validando stock y precio oficial.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto request)
    {
        try
        {
            var cart = await _cartService.AddItemAsync(GetUserId(), request);
            return Ok(cart);
        }
        catch (BadHttpRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza la cantidad de un producto en el carrito.
    /// </summary>
    [HttpPut("items/{productId:int}")]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateQuantity(int productId, [FromBody] UpdateQuantityDto request)
    {
        try
        {
            var cart = await _cartService.UpdateQuantityAsync(GetUserId(), request with { ProductId = productId });
            return Ok(cart);
        }
        catch (BadHttpRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un producto del carrito.
    /// </summary>
    [HttpDelete("items/{productId:int}")]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var cart = await _cartService.RemoveItemAsync(GetUserId(), productId);
        return Ok(cart);
    }

    /// <summary>
    /// Vacía el carrito por completo.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCart()
    {
        bool deleted = await _cartService.DeleteCartAsync(GetUserId());
        return Ok(new { deleted });
    }

    private string GetUserId()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(claimValue))
        {
            return claimValue;
        }
        throw new UnauthorizedAccessException("El token no contiene un identificador de usuario válido.");
    }
}