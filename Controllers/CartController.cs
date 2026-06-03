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

    public CartController(IOrderGrpcClient orderClient)
    {
        _orderClient = orderClient;
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

        var success = await _orderClient.AddCartItemAsync(currentUserId, model);
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

        var success = await _orderClient.PayAsync(currentUserId);
        if (!success)
        {
            TempData["StockError"] = "Checkout failed. Please review your cart and try again.";
            return RedirectToAction(nameof(CartDetail));
        }

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

        await _orderClient.DecrementCartItemAsync(currentUserId, OrderProductId);
        return RedirectToAction(nameof(CartDetail));
    }
}
