using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using OrderService.Protos.Catalog;

namespace OrderService.Services;

public sealed class CatalogGrpcClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Catalog.CatalogClient _client;
    private readonly IConfiguration _configuration;

    public CatalogGrpcClient(IConfiguration configuration)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _configuration = configuration;
        var address = configuration["CatalogService:Address"] ?? "http://localhost:8082";
        _channel = GrpcChannel.ForAddress(address);
        _client = new Catalog.CatalogClient(_channel);
    }

    public async Task<ProductSummary?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetProductDetailAsync(new ProductRequest { Id = id }, cancellationToken: cancellationToken);
            return response.Product;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> AdjustStockAsync(int productId, int quantityDelta, CancellationToken cancellationToken = default)
    {
        var response = await _client.AdjustStockAsync(new AdjustStockRequest
        {
            ProductId = productId,
            QuantityDelta = quantityDelta
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
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
        var issuer = configuration["CatalogService:Jwt:Issuer"] ?? "CatalogService";
        var audience = configuration["CatalogService:Jwt:Audience"] ?? "WebDevStd2531";
        var key = configuration["CatalogService:Jwt:Key"] ?? "DevOnly_ChangeThis_For_Real_Projects_1234567890!";
        var expiresMinutesRaw = configuration["CatalogService:Jwt:ExpiresMinutes"] ?? "120";

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
                new Claim(ClaimTypes.Name, "OrderService"),
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
