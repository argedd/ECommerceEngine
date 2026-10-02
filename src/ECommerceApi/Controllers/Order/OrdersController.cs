using System.Security.Claims;
using ECommerceApi.Dtos;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Realiza el checkout del carrito actual y genera una orden de compra atómica.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout()
    {
            int userId = GetUserId();
            var order = await _orderService.CheckoutAsync(userId);
            return Ok(order);
    }

    /// <summary>
    /// Obtiene el historial de órdenes del usuario autenticado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders()
    {
        int userId = GetUserId();
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    /// <summary>
    /// Obtiene el detalle de una orden específica por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        int userId = GetUserId();
        var order = await _orderService.GetOrderByIdAsync(id, userId);
        if (order == null)
        {
            return NotFound(new { message = $"Orden con ID {id} no encontrada." });
        }

        return Ok(order);
    }

    private int GetUserId()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claimValue, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("El token no contiene un identificador de usuario válido.");
    }
}