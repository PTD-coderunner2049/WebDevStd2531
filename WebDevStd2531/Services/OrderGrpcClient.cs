using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using WebDevStd2531.Models;
using WebDevStd2531.Protos;

namespace WebDevStd2531.Services;

public sealed class OrderGrpcClient : IOrderGrpcClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly OrderManagement.OrderManagementClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderGrpcClient> _logger;

    public OrderGrpcClient(IConfiguration configuration, ILogger<OrderGrpcClient> logger)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _configuration = configuration;
        _logger = logger;

        var address = configuration["OrderService:Address"] ?? "http://localhost:8083";
        _channel = GrpcChannel.ForAddress(address);
        _client = new OrderManagement.OrderManagementClient(_channel);
    }

    public async Task<List<CartItemViewModel>> GetCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetCartAsync(new CartRequest { UserId = userId }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);
        return response.Items.Select(MapCartItem).ToList();
    }

    public async Task<bool> AddCartItemAsync(string userId, AddCartViewModel model, CancellationToken cancellationToken = default)
    {
        var response = await _client.AddCartItemAsync(new AddCartItemRequest
        {
            UserId = userId,
            ProductId = model.ProductId,
            SelectedType = model.SelectedType,
            Quantity = model.Quantity
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> RemoveCartItemAsync(string userId, int orderProductId, CancellationToken cancellationToken = default)
    {
        var response = await _client.RemoveCartItemAsync(new CartItemActionRequest
        {
            UserId = userId,
            OrderProductId = orderProductId
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> IncrementCartItemAsync(string userId, int orderProductId, CancellationToken cancellationToken = default)
    {
        var response = await _client.IncrementCartItemAsync(new CartItemActionRequest
        {
            UserId = userId,
            OrderProductId = orderProductId
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> DecrementCartItemAsync(string userId, int orderProductId, CancellationToken cancellationToken = default)
    {
        var response = await _client.DecrementCartItemAsync(new CartItemActionRequest
        {
            UserId = userId,
            OrderProductId = orderProductId
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> PayAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _client.PayAsync(new PayRequest { UserId = userId }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);
        if (!response.Success)
        {
            _logger.LogWarning("Checkout failed for user {UserId}: {Message}", userId, response.Message);
        }
        return response.Success;
    }

    private static CartItemViewModel MapCartItem(CartItemSummary item)
    {
        return new CartItemViewModel
        {
            OrderProductId = item.OrderProductId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ImageUrl = item.ImageUrl,
            MaxStock = item.MaxStock,
            Quantity = item.Quantity,
            Price = item.Price,
            Discount = item.Discount,
            Tax = item.Tax,
            SelectedType = item.SelectedType
        };
    }

    private Metadata? CreateAuthHeaders()
    {
        var serviceToken = CreateServiceToken(_configuration);
        if (string.IsNullOrWhiteSpace(serviceToken))
        {
            return null;
        }

        return new Metadata
        {
            { "Authorization", $"Bearer {serviceToken}" }
        };
    }

    private static string? CreateServiceToken(IConfiguration configuration)
    {
        var issuer = configuration["OrderService:Jwt:Issuer"] ?? "OrderService";
        var audience = configuration["OrderService:Jwt:Audience"] ?? "WebDevStd2531";
        var key = configuration["OrderService:Jwt:Key"] ?? "DevOnly_ChangeThis_For_Real_Projects_1234567890!";
        var expiresMinutesRaw = configuration["OrderService:Jwt:ExpiresMinutes"] ?? "120";

        if (!int.TryParse(expiresMinutesRaw, out var expiresMinutes))
        {
            expiresMinutes = 120;
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: new[]
            {
                new Claim(ClaimTypes.Name, "WebDevStd2531"),
                new Claim(ClaimTypes.Role, "Service")
            },
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
