using Microsoft.AspNetCore.Authorization;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using OrderService.AppData;
using OrderService.Models;
using OrderService.Protos;
using OrderService.Protos.Catalog;

namespace OrderService.Services;

public sealed class OrderGrpcService : OrderManagement.OrderManagementBase
{
    private readonly OrderDbContext _db;
    private readonly CatalogGrpcClient _catalogClient;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(OrderDbContext db, CatalogGrpcClient catalogClient, ILogger<OrderGrpcService> logger)
    {
        _db = db;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    [Authorize]
    public override async Task<CartReply> GetCart(CartRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetCart requested for user {UserId}.", request.UserId);
        var cart = await GetPendingOrderAsync(request.UserId, context.CancellationToken);
        if (cart?.OrderProducts == null || cart.OrderProducts.Count == 0)
        {
            _logger.LogInformation("Cart is empty for user {UserId}.", request.UserId);
            return new CartReply();
        }

        var items = new List<CartItemSummary>();
        foreach (var orderProduct in cart.OrderProducts)
        {
            var product = await _catalogClient.GetProductAsync(orderProduct.ProductId, context.CancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} missing while building cart for user {UserId}.", orderProduct.ProductId, request.UserId);
                items.Add(MapFallbackCartItem(orderProduct));
                continue;
            }

            items.Add(MapCartItem(orderProduct, product));
        }

        return new CartReply
        {
            Items = { items },
            GrandTotal = items.Sum(CalculateLineTotal)
        };
    }

    [Authorize]
    public override async Task<MutationReply> AddCartItem(AddCartItemRequest request, ServerCallContext context)
    {
        _logger.LogInformation("AddCartItem requested for user {UserId}, product {ProductId}, quantity {Quantity}.", request.UserId, request.ProductId, request.Quantity);
        if (request.Quantity <= 0)
        {
            return Fail("Quantity must be at least 1.");
        }

        var product = await _catalogClient.GetProductAsync(request.ProductId, context.CancellationToken);
        if (product == null)
        {
            _logger.LogWarning("AddCartItem rejected: product {ProductId} not found for user {UserId}.", request.ProductId, request.UserId);
            return Fail("Product not found.");
        }

        if (request.Quantity > product.Stock)
        {
            _logger.LogWarning("AddCartItem rejected: insufficient stock for product {ProductId} for user {UserId}.", request.ProductId, request.UserId);
            return Fail($"Insufficient stock for '{product.Name}'.");
        }

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);

                var cart = await GetOrCreatePendingOrderAsync(request.UserId, context.CancellationToken);

                var existing = cart.OrderProducts?
                    .FirstOrDefault(op => op.ProductId == request.ProductId && op.Type == request.SelectedType);

                if (existing != null)
                {
                    var currentQuantity = existing.Quantity ?? 0;
                    if (currentQuantity + request.Quantity > product.Stock)
                    {
                        await transaction.RollbackAsync(context.CancellationToken);
                        return Fail($"Cannot add more of '{product.Name}'. Maximum stock is {product.Stock}.");
                    }

                    existing.Quantity = currentQuantity + request.Quantity;
                }
                else
                {
                    var newOrderProduct = new OrderProduct
                    {
                        ProductId = request.ProductId,
                        OrderId = cart.Id!.Value,
                        Quantity = request.Quantity,
                        Price = product.Price,
                        Type = string.IsNullOrWhiteSpace(request.SelectedType) ? "Default" : request.SelectedType.Trim()
                    };
                    _db.OrderProducts.Add(newOrderProduct);
                }

                await _db.SaveChangesAsync(context.CancellationToken);
                await transaction.CommitAsync(context.CancellationToken);
                _logger.LogInformation("Cart item added for user {UserId}, product {ProductId}.", request.UserId, request.ProductId);
                return Success($"Product '{product.Name}' added to cart.");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add product {ProductId} to cart for user {UserId}.", request.ProductId, request.UserId);
            return Fail("Failed to add item to cart.");
        }
    }

    [Authorize]
    public override async Task<MutationReply> RemoveCartItem(CartItemActionRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RemoveCartItem requested for user {UserId}, orderProduct {OrderProductId}.", request.UserId, request.OrderProductId);
        var orderProduct = await _db.OrderProducts
            .Include(op => op.Order)
            .FirstOrDefaultAsync(op => op.Id == request.OrderProductId && op.Order!.UserId == request.UserId && op.Order.Status == "Pending", context.CancellationToken);

        if (orderProduct == null)
        {
            _logger.LogWarning("RemoveCartItem rejected: order product {OrderProductId} not found for user {UserId}.", request.OrderProductId, request.UserId);
            return Fail("Cart item not found.");
        }

        _db.OrderProducts.Remove(orderProduct);
        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Cart item {OrderProductId} removed for user {UserId}.", request.OrderProductId, request.UserId);
        return Success("Cart item removed.");
    }

    [Authorize]
    public override async Task<MutationReply> IncrementCartItem(CartItemActionRequest request, ServerCallContext context)
    {
        _logger.LogInformation("IncrementCartItem requested for user {UserId}, orderProduct {OrderProductId}.", request.UserId, request.OrderProductId);
        var orderProduct = await _db.OrderProducts
            .Include(op => op.Order)
            .FirstOrDefaultAsync(op => op.Id == request.OrderProductId && op.Order!.UserId == request.UserId && op.Order.Status == "Pending", context.CancellationToken);

        if (orderProduct == null)
        {
            _logger.LogWarning("IncrementCartItem rejected: order product {OrderProductId} not found for user {UserId}.", request.OrderProductId, request.UserId);
            return Fail("Cart item not found.");
        }

        var product = await _catalogClient.GetProductAsync(orderProduct.ProductId, context.CancellationToken);
        if (product == null)
        {
            _logger.LogWarning("IncrementCartItem rejected: product {ProductId} not found for user {UserId}.", orderProduct.ProductId, request.UserId);
            return Fail("Product not found.");
        }

        var currentQuantity = orderProduct.Quantity ?? 0;
        if (currentQuantity + 1 > product.Stock)
        {
            _logger.LogWarning("IncrementCartItem rejected: stock limit reached for product {ProductId} for user {UserId}.", orderProduct.ProductId, request.UserId);
            return Fail($"Cannot add more. Maximum stock for '{product.Name}' is {product.Stock}.");
        }

        orderProduct.Quantity = currentQuantity + 1;
        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Cart item {OrderProductId} incremented for user {UserId}.", request.OrderProductId, request.UserId);
        return Success("Cart item increased.");
    }

    [Authorize]
    public override async Task<MutationReply> DecrementCartItem(CartItemActionRequest request, ServerCallContext context)
    {
        _logger.LogInformation("DecrementCartItem requested for user {UserId}, orderProduct {OrderProductId}.", request.UserId, request.OrderProductId);
        var orderProduct = await _db.OrderProducts
            .Include(op => op.Order)
            .FirstOrDefaultAsync(op => op.Id == request.OrderProductId && op.Order!.UserId == request.UserId && op.Order.Status == "Pending", context.CancellationToken);

        if (orderProduct == null)
        {
            _logger.LogWarning("DecrementCartItem rejected: order product {OrderProductId} not found for user {UserId}.", request.OrderProductId, request.UserId);
            return Fail("Cart item not found.");
        }

        var currentQuantity = orderProduct.Quantity ?? 0;
        if (currentQuantity - 1 <= 0)
        {
            _db.OrderProducts.Remove(orderProduct);
        }
        else
        {
            orderProduct.Quantity = currentQuantity - 1;
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Cart item {OrderProductId} decremented for user {UserId}.", request.OrderProductId, request.UserId);
        return Success("Cart item decreased.");
    }

    [Authorize]
    public override async Task<PayReply> Pay(PayRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Pay requested for user {UserId}.", request.UserId);
        var cart = await GetPendingOrderAsync(request.UserId, context.CancellationToken);
        if (cart?.OrderProducts == null || cart.OrderProducts.Count == 0)
        {
            _logger.LogWarning("Pay rejected: cart is empty for user {UserId}.", request.UserId);
            return new PayReply
            {
                Success = false,
                Message = "Your cart is empty."
            };
        }

        var pricingSnapshot = new List<(OrderProduct OrderProduct, ProductSummary Product)>();
        foreach (var orderProduct in cart.OrderProducts)
        {
            var product = await _catalogClient.GetProductAsync(orderProduct.ProductId, context.CancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Pay rejected: product {ProductId} missing for user {UserId}.", orderProduct.ProductId, request.UserId);
                return new PayReply
                {
                    Success = false,
                    Message = $"Product {orderProduct.ProductId} could not be found."
                };
            }

            var quantity = orderProduct.Quantity ?? 0;
            if (product.Stock < quantity)
            {
                _logger.LogWarning("Pay rejected: insufficient stock for product {ProductId} for user {UserId}.", orderProduct.ProductId, request.UserId);
                return new PayReply
                {
                    Success = false,
                    Message = $"Insufficient stock for {product.Name}."
                };
            }

            pricingSnapshot.Add((orderProduct, product));
        }

        var adjusted = new List<(int ProductId, int Quantity)>();
        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);

                foreach (var entry in pricingSnapshot)
                {
                    var quantity = entry.OrderProduct.Quantity ?? 0;
                    var adjustedOk = await _catalogClient.AdjustStockAsync(entry.OrderProduct.ProductId, -quantity, context.CancellationToken);
                    if (!adjustedOk)
                    {
                        foreach (var revert in adjusted)
                        {
                            await _catalogClient.AdjustStockAsync(revert.ProductId, revert.Quantity, context.CancellationToken);
                        }

                        await transaction.RollbackAsync(context.CancellationToken);
                        return new PayReply
                        {
                            Success = false,
                            Message = "Failed to reserve stock for checkout."
                        };
                    }

                    adjusted.Add((entry.OrderProduct.ProductId, quantity));
                }

                cart.Status = "Completed";
                cart.PaidAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(context.CancellationToken);
                await transaction.CommitAsync(context.CancellationToken);

                var grandTotal = pricingSnapshot.Sum(entry => CalculateLineTotal(MapCartItem(entry.OrderProduct, entry.Product)));
                _logger.LogInformation("Payment completed for user {UserId}. Items: {ItemCount}, Total: {Total}.", request.UserId, pricingSnapshot.Count, grandTotal);
                return new PayReply
                {
                    Success = true,
                    Message = "Payment completed.",
                    Total = grandTotal,
                    ItemCount = pricingSnapshot.Count
                };
            });
        }
        catch (Exception ex)
        {
            foreach (var revert in adjusted)
            {
                await _catalogClient.AdjustStockAsync(revert.ProductId, revert.Quantity, context.CancellationToken);
            }

            _logger.LogError(ex, "Failed to complete payment for user {UserId}.", request.UserId);
            return new PayReply
            {
                Success = false,
                Message = "Checkout failed."
            };
        }
    }

    [Authorize]
    public override async Task StreamCartItems(CartRequest request, IServerStreamWriter<CartItemSummary> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("StreamCartItems requested for user {UserId}.", request.UserId);
        var reply = await GetCart(request, context);
        foreach (var item in reply.Items)
        {
            await responseStream.WriteAsync(item);
        }
    }

    private async Task<Order> GetOrCreatePendingOrderAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await GetPendingOrderAsync(userId, cancellationToken);
        if (cart != null)
        {
            return cart;
        }

        cart = new Order
        {
            UserId = userId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            OrderProducts = new List<OrderProduct>()
        };

        _db.Orders.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<Order?> GetPendingOrderAsync(string userId, CancellationToken cancellationToken)
    {
        return await _db.Orders
            .Include(o => o.OrderProducts)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Pending", cancellationToken);
    }

    private static CartItemSummary MapCartItem(OrderProduct orderProduct, ProductSummary product)
    {
        return new CartItemSummary
        {
            OrderProductId = orderProduct.Id,
            ProductId = orderProduct.ProductId,
            ProductName = product.Name,
            ImageUrl = product.ImageUrl,
            MaxStock = product.Stock,
            Quantity = orderProduct.Quantity ?? 0,
            Price = orderProduct.Price ?? product.Price,
            Discount = product.Discount,
            Tax = product.Tax,
            SelectedType = orderProduct.Type
        };
    }

    private static CartItemSummary MapFallbackCartItem(OrderProduct orderProduct)
    {
        return new CartItemSummary
        {
            OrderProductId = orderProduct.Id,
            ProductId = orderProduct.ProductId,
            ProductName = $"Product #{orderProduct.ProductId}",
            ImageUrl = "/assets/themes/images/ico-cart.png",
            MaxStock = 0,
            Quantity = orderProduct.Quantity ?? 0,
            Price = orderProduct.Price ?? 0,
            Discount = 0,
            Tax = 0,
            SelectedType = orderProduct.Type
        };
    }

    private static double CalculateLineTotal(CartItemSummary item)
    {
        var discountRate = item.Discount / 100.0;
        var taxRate = item.Tax / 100.0;
        var discountedPricePerUnit = item.Price * (1 - discountRate);
        var finalPricePerUnit = discountedPricePerUnit * (1 + taxRate);
        return finalPricePerUnit * item.Quantity;
    }

    private static MutationReply Success(string message) => new()
    {
        Success = true,
        Message = message
    };

    private static MutationReply Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
