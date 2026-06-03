using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Controllers;

public class CartController : Controller
{
    private readonly IOrderGrpcClient _orderClient;
    private readonly ILogger<CartController> _logger;

    public CartController(IOrderGrpcClient orderClient, ILogger<CartController> logger)
    {
        _orderClient = orderClient;
        _logger = logger;
    }

    public IActionResult ConfirmPay(List<CartItemViewModel> cartItems)
    {
        if (cartItems == null || !cartItems.Any())
        {
            return RedirectToAction(nameof(CartDetail));
        }

        return View(cartItems);
    }

    public async Task<IActionResult> CartDetail()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "User");
        }

        _logger.LogInformation("CartDetail requested for user {UserId}.", currentUserId);
        var cartItems = await _orderClient.GetCartAsync(currentUserId);
        return View(cartItems);
    }

    [HttpPost]
    public async Task<IActionResult> AddCartItem(AddCartViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "User");
        }

        _logger.LogInformation("AddCartItem submit received for user {UserId}, product {ProductId}, quantity {Quantity}.", currentUserId, model.ProductId, model.Quantity);
        var success = await _orderClient.AddCartItemAsync(currentUserId, model);
        if (success)
        {
            _logger.LogInformation("AddCartItem succeeded for user {UserId}, product {ProductId}.", currentUserId, model.ProductId);
        }
        else
        {
            _logger.LogWarning("AddCartItem failed for user {UserId}, product {ProductId}.", currentUserId, model.ProductId);
        }
        return RedirectToAction(nameof(CartDetail));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCartItem(int OrderProductId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "User");
        }

        _logger.LogInformation("RemoveCartItem requested for user {UserId}, orderProduct {OrderProductId}.", currentUserId, OrderProductId);
        await _orderClient.RemoveCartItemAsync(currentUserId, OrderProductId);
        return RedirectToAction(nameof(CartDetail));
    }

    [HttpPost]
    public async Task<IActionResult> Pay(List<CartItemViewModel> cartItems)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "User");
        }

        _logger.LogInformation("Pay requested for user {UserId}.", currentUserId);
        var success = await _orderClient.PayAsync(currentUserId);
        if (!success)
        {
            _logger.LogWarning("Pay failed for user {UserId}.", currentUserId);
            TempData["StockError"] = "Checkout failed. Please review your cart and try again.";
            return RedirectToAction(nameof(CartDetail));
        }

        _logger.LogInformation("Pay succeeded for user {UserId}.", currentUserId);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> IncrCartItem(int OrderProductId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "User");
        }

        _logger.LogInformation("IncrCartItem requested for user {UserId}, orderProduct {OrderProductId}.", currentUserId, OrderProductId);
        await _orderClient.IncrementCartItemAsync(currentUserId, OrderProductId);
        return RedirectToAction(nameof(CartDetail));
    }

    [HttpPost]
    public async Task<IActionResult> DecrCartItem(int OrderProductId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "User");
        }

        _logger.LogInformation("DecrCartItem requested for user {UserId}, orderProduct {OrderProductId}.", currentUserId, OrderProductId);
        await _orderClient.DecrementCartItemAsync(currentUserId, OrderProductId);
        return RedirectToAction(nameof(CartDetail));
    }
}
