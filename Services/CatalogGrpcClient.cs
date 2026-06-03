using Grpc.Net.Client;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using WebDevStd2531.Models;
using WebDevStd2531.Protos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebDevStd2531.Services;

public class CatalogGrpcClient : ICatalogGrpcClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Catalog.CatalogClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CatalogGrpcClient> _logger;

    public CatalogGrpcClient(IConfiguration configuration, ILogger<CatalogGrpcClient> logger)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _configuration = configuration;
        _logger = logger;
        var address = configuration["CatalogService:Address"] ?? "http://localhost:8082";
        _channel = GrpcChannel.ForAddress(address);
        _client = new Catalog.CatalogClient(_channel);
    }

    public async Task<HomeViewModelIndex> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetHomeCatalogAsync(new CatalogRequest(), cancellationToken: cancellationToken);
            return new HomeViewModelIndex
            {
                FeaturedProducts = response.FeaturedProducts.Select(MapProduct).ToList(),
                AllGrandCategories = response.GrandCategories.Select(MapGrandCategory).ToList(),
                AllCategories = response.Categories.Select(MapCategory).ToList()
            };
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "CatalogService failed while loading home catalog data.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while loading home catalog data from CatalogService.");
        }

        return new HomeViewModelIndex();
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetProductDetailAsync(new ProductRequest { Id = id }, cancellationToken: cancellationToken);
            var product = MapProduct(response.Product);
            var category = product.Category ?? new Category { Id = product.CategoryId, Name = string.Empty, GrandCategoryId = 0, GrandCategory = new GrandCategory { Id = 0, Name = string.Empty } };
            category.Products = response.RelatedProducts.Select(MapProduct).ToList();
            product.Category = category;
            return product;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("CatalogService reported product {ProductId} not found.", id);
            return null;
        }
        catch
        {
            _logger.LogError("Failed to fetch product detail for product {ProductId} from CatalogService.", id);
            return null;
        }
    }

    public async Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetCategoryDetailAsync(new CategoryRequest { Id = id }, cancellationToken: cancellationToken);
            return MapCategory(response.Category);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("CatalogService reported category {CategoryId} not found.", id);
            return null;
        }
        catch
        {
            _logger.LogError("Failed to fetch category detail for category {CategoryId} from CatalogService.", id);
            return null;
        }
    }

    public async Task<List<Product>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.ListProductsAsync(new CatalogRequest(), cancellationToken: cancellationToken);
        return response.Products.Select(MapProduct).ToList();
    }

    public async Task<List<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.ListCategoriesAsync(new CatalogRequest(), cancellationToken: cancellationToken);
        return response.Categories.Select(MapCategory).ToList();
    }

    public async Task<bool> UpsertProductAsync(Product product, string categoryName, CancellationToken cancellationToken = default)
    {
        var response = await _client.UpsertProductAsync(new UpsertProductRequest
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Discount = product.Discount ?? 0,
            Tax = product.Tax ?? 0,
            ImageUrl = product.ImageUrl,
            CategoryName = categoryName
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteProductAsync(new DeleteRequest { Id = id }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);
        return response.Success;
    }

    public async Task<bool> UpsertCategoryAsync(Category category, string grandCategoryName, CancellationToken cancellationToken = default)
    {
        var response = await _client.UpsertCategoryAsync(new UpsertCategoryRequest
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description ?? string.Empty,
            GrandCategoryName = grandCategoryName
        }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteCategoryAsync(new DeleteRequest { Id = id }, headers: CreateAuthHeaders(), cancellationToken: cancellationToken);
        return response.Success;
    }

    private static Product MapProduct(ProductSummary dto)
    {
        return new Product
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            Discount = dto.Discount,
            Tax = dto.Tax,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            Category = new Category
            {
                Id = dto.CategoryId,
                Name = dto.CategoryName,
                Description = string.Empty,
                GrandCategoryId = 0,
                GrandCategory = new GrandCategory { Id = 0, Name = string.Empty }
            },
            AvailableOptions = dto.AvailableOptions.Select(o => new ProductOption
            {
                Id = o.Id,
                Value = o.Value,
                ProductId = dto.Id
            }).ToList()
        };
    }

    private static Category MapCategory(CategorySummary dto)
    {
        return new Category
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            GrandCategoryId = dto.GrandCategoryId,
            GrandCategory = new GrandCategory
            {
                Id = dto.GrandCategoryId,
                Name = dto.GrandCategoryName
            },
            Products = dto.Products.Select(MapProduct).ToList()
        };
    }

    private static GrandCategory MapGrandCategory(GrandCategorySummary dto)
    {
        return new GrandCategory
        {
            Id = dto.Id,
            Name = dto.Name,
            Categories = dto.Categories.Select(MapCategory).ToList()
        };
    }

    public void Dispose()
    {
        _channel.Dispose();
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
                new Claim(ClaimTypes.Name, "WebDevStd2531"),
                new Claim(ClaimTypes.Role, "Service")
            },
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
