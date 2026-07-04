using CatalogService.AppData;
using CatalogService.Contracts;
using CatalogService.Models;
using CatalogService.Services.Messaging;
using Microsoft.AspNetCore.Authorization;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Services;

public class CatalogGrpcService : Catalog.CatalogBase
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<CatalogGrpcService> _logger;
    private readonly RabbitMqEventPublisher _eventPublisher;

    public CatalogGrpcService(CatalogDbContext db, ILogger<CatalogGrpcService> logger, RabbitMqEventPublisher eventPublisher)
    {
        _db = db;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public override async Task<HomeCatalogReply> GetHomeCatalog(CatalogRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetHomeCatalog requested.");
        var featuredProducts = await _db.Products
            .AsNoTracking()
            .Include(p => p.AvailableOptions)
            .ToListAsync(context.CancellationToken);

        var grandCategories = await LoadGrandCategoriesAsync(context.CancellationToken);
        var categories = grandCategories.SelectMany(gc => gc.Categories ?? Array.Empty<Category>()).ToList();

        return new HomeCatalogReply
        {
            FeaturedProducts = { featuredProducts.Select(MapProductSummary) },
            GrandCategories = { grandCategories.Select(MapGrandCategorySummary) },
            Categories = { categories.Select(MapCategorySummary) }
        };
    }

    public override async Task<ProductDetailReply> GetProductDetail(ProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetProductDetail requested for product {ProductId}.", request.Id);
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.AvailableOptions)
            .FirstOrDefaultAsync(p => p.Id == request.Id, context.CancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found.", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, "Product not found."));
        }

        var related = await _db.Products
            .AsNoTracking()
            .Include(p => p.AvailableOptions)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
            .Take(4)
            .ToListAsync(context.CancellationToken);

        return new ProductDetailReply
        {
            Product = MapProductSummary(product),
            RelatedProducts = { related.Select(MapProductSummary) }
        };
    }

    public override async Task<CategoryDetailReply> GetCategoryDetail(CategoryRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetCategoryDetail requested for category {CategoryId}.", request.Id);
        var category = await _db.Categories
            .AsNoTracking()
            .Include(c => c.GrandCategory)
            .Include(c => c.Products)
                .ThenInclude(p => p.AvailableOptions)
            .FirstOrDefaultAsync(c => c.Id == request.Id, context.CancellationToken);

        if (category == null)
        {
            _logger.LogWarning("Category {CategoryId} not found.", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, "Category not found."));
        }

        return new CategoryDetailReply
        {
            Category = MapCategorySummary(category)
        };
    }

    public override async Task<ProductListReply> ListProducts(CatalogRequest request, ServerCallContext context)
    {
        _logger.LogInformation("ListProducts requested.");
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.AvailableOptions)
            .ToListAsync(context.CancellationToken);

        return new ProductListReply
        {
            Products = { products.Select(MapProductSummary) }
        };
    }

    public override async Task<CategoryListReply> ListCategories(CatalogRequest request, ServerCallContext context)
    {
        _logger.LogInformation("ListCategories requested.");
        var categories = await _db.Categories
            .AsNoTracking()
            .Include(c => c.GrandCategory)
            .Include(c => c.Products)
            .ToListAsync(context.CancellationToken);

        return new CategoryListReply
        {
            Categories = { categories.Select(MapCategorySummary) }
        };
    }

    [Authorize]
    public override async Task<CatalogMutationReply> UpsertProduct(UpsertProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("UpsertProduct requested for product {ProductId} ({ProductName}).", request.Id, request.Name);
        var categoryName = request.CategoryName?.Trim();
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return Fail("Category name is required.");
        }

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Name.ToUpper() == categoryName.ToUpper(), context.CancellationToken);

        if (category == null)
        {
            _logger.LogWarning("UpsertProduct rejected for product {ProductName}: category {CategoryName} not found.", request.Name, categoryName);
            return Fail($"Category '{categoryName}' does not exist.");
        }

        Product product;
        if (request.Id > 0)
        {
            product = await _db.Products.Include(p => p.AvailableOptions).FirstAsync(p => p.Id == request.Id, context.CancellationToken);
            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.Discount = request.Discount;
            product.Tax = request.Tax;
            product.ImageUrl = request.ImageUrl;
            product.CategoryId = category.Id;
        }
        else
        {
            product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                Discount = request.Discount,
                Tax = request.Tax,
                ImageUrl = request.ImageUrl,
                CategoryId = category.Id,
                AvailableOptions = new List<ProductOption>()
            };
            _db.Products.Add(product);
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Product {ProductId} saved successfully.", product.Id);
        await _eventPublisher.PublishAsync("CatalogChanged", new
        {
            action = request.Id > 0 ? "ProductUpdated" : "ProductCreated",
            entity = "Product",
            entityId = product.Id,
            name = product.Name,
            categoryId = product.CategoryId,
            stock = product.Stock
        }, context.CancellationToken);
        return Success(product.Id, $"Product '{product.Name}' saved.");
    }

    [Authorize]
    public override async Task<CatalogMutationReply> DeleteProduct(DeleteRequest request, ServerCallContext context)
    {
        _logger.LogInformation("DeleteProduct requested for product {ProductId}.", request.Id);
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, context.CancellationToken);
        if (product == null)
        {
            _logger.LogWarning("DeleteProduct rejected: product {ProductId} not found.", request.Id);
            return Fail("Product not found.");
        }

        var orderProductsInPendingCarts = await _db.OrderProducts
            .Where(op => op.ProductId == request.Id && op.Order!.Status == "Pending")
            .ToListAsync(context.CancellationToken);

        if (orderProductsInPendingCarts.Any())
        {
            _db.OrderProducts.RemoveRange(orderProductsInPendingCarts);
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Product {ProductId} deleted successfully.", request.Id);
        await _eventPublisher.PublishAsync("CatalogChanged", new
        {
            action = "ProductDeleted",
            entity = "Product",
            entityId = request.Id,
            name = product.Name
        }, context.CancellationToken);
        return Success(request.Id, $"Product '{product.Name}' deleted.");
    }

    [Authorize]
    public override async Task<AdjustStockReply> AdjustStock(AdjustStockRequest request, ServerCallContext context)
    {
        _logger.LogInformation("AdjustStock requested for product {ProductId} with delta {QuantityDelta}.", request.ProductId, request.QuantityDelta);
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, context.CancellationToken);
        if (product == null)
        {
            _logger.LogWarning("AdjustStock rejected: product {ProductId} not found.", request.ProductId);
            return new AdjustStockReply
            {
                Success = false,
                Message = "Product not found."
            };
        }

        var nextStock = product.Stock + request.QuantityDelta;
        if (nextStock < 0)
        {
            _logger.LogWarning("AdjustStock rejected: insufficient stock for product {ProductId}. Requested delta {QuantityDelta}, current stock {CurrentStock}.", request.ProductId, request.QuantityDelta, product.Stock);
            return new AdjustStockReply
            {
                Success = false,
                Message = $"Insufficient stock for product '{product.Name}'.",
                NewStock = product.Stock
            };
        }

        product.Stock = nextStock;
        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Stock updated for product {ProductId}. New stock {NewStock}.", request.ProductId, product.Stock);
        await _eventPublisher.PublishAsync("CatalogChanged", new
        {
            action = "StockAdjusted",
            entity = "Product",
            entityId = product.Id,
            name = product.Name,
            quantityDelta = request.QuantityDelta,
            newStock = product.Stock
        }, context.CancellationToken);
        return new AdjustStockReply
        {
            Success = true,
            Message = $"Stock updated for product '{product.Name}'.",
            NewStock = product.Stock
        };
    }

    [Authorize]
    public override async Task<CatalogMutationReply> UpsertCategory(UpsertCategoryRequest request, ServerCallContext context)
    {
        _logger.LogInformation("UpsertCategory requested for category {CategoryId} ({CategoryName}).", request.Id, request.Name);
        var grandCategoryName = request.GrandCategoryName?.Trim();
        if (string.IsNullOrWhiteSpace(grandCategoryName))
        {
            return Fail("Grand category name is required.");
        }

        var grandCategory = await _db.GrandCategories
            .FirstOrDefaultAsync(gc => gc.Name.ToUpper() == grandCategoryName.ToUpper(), context.CancellationToken);

        if (grandCategory == null)
        {
            grandCategory = new GrandCategory { Name = grandCategoryName };
            _db.GrandCategories.Add(grandCategory);
            await _db.SaveChangesAsync(context.CancellationToken);
        }

        Category category;
        if (request.Id > 0)
        {
            category = await _db.Categories.FirstAsync(c => c.Id == request.Id, context.CancellationToken);
            category.Name = request.Name;
            category.Description = request.Description;
            category.GrandCategoryId = grandCategory.Id;
        }
        else
        {
            category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                GrandCategoryId = grandCategory.Id
            };
            _db.Categories.Add(category);
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Category {CategoryId} saved successfully.", category.Id);
        await _eventPublisher.PublishAsync("CatalogChanged", new
        {
            action = request.Id > 0 ? "CategoryUpdated" : "CategoryCreated",
            entity = "Category",
            entityId = category.Id,
            name = category.Name,
            grandCategoryId = grandCategory.Id
        }, context.CancellationToken);
        return Success(category.Id, $"Category '{category.Name}' saved.");
    }

    [Authorize]
    public override async Task<CatalogMutationReply> DeleteCategory(DeleteRequest request, ServerCallContext context)
    {
        _logger.LogInformation("DeleteCategory requested for category {CategoryId}.", request.Id);
        var category = await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == request.Id, context.CancellationToken);

        if (category == null)
        {
            _logger.LogWarning("DeleteCategory rejected: category {CategoryId} not found.", request.Id);
            return Fail("Category not found.");
        }

        if ((category.Products?.Count ?? 0) > 0)
        {
            _logger.LogWarning("DeleteCategory rejected: category {CategoryId} still has products.", request.Id);
            return Fail("Cannot delete a category that still has products.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Category {CategoryId} deleted successfully.", request.Id);
        await _eventPublisher.PublishAsync("CatalogChanged", new
        {
            action = "CategoryDeleted",
            entity = "Category",
            entityId = request.Id,
            name = category.Name
        }, context.CancellationToken);
        return Success(request.Id, $"Category '{category.Name}' deleted.");
    }

    public override async Task StreamProducts(CatalogRequest request, IServerStreamWriter<ProductSummary> responseStream, ServerCallContext context)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.AvailableOptions)
            .ToListAsync(context.CancellationToken);

        foreach (var product in products)
        {
            await responseStream.WriteAsync(MapProductSummary(product));
        }
    }

    private async Task<List<GrandCategory>> LoadGrandCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _db.GrandCategories
            .AsNoTracking()
            .Include(gc => gc.Categories)
                .ThenInclude(c => c!.Products)
                    .ThenInclude(p => p!.AvailableOptions)
            .ToListAsync(cancellationToken);
    }

    private static ProductSummary MapProductSummary(Product product)
    {
        return new ProductSummary
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Discount = product.Discount ?? 0,
            Tax = product.Tax ?? 0,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            AvailableOptions = { (product.AvailableOptions ?? Array.Empty<ProductOption>()).Select(option => new ProductOptionDto
            {
                Id = option.Id,
                Value = option.Value
            }) }
        };
    }

    private static CategorySummary MapCategorySummary(Category category)
    {
        return new CategorySummary
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description ?? string.Empty,
            GrandCategoryId = category.GrandCategoryId,
            GrandCategoryName = category.GrandCategory?.Name ?? string.Empty,
            Products = { (category.Products ?? Array.Empty<Product>()).Select(MapProductSummary) }
        };
    }

    private static GrandCategorySummary MapGrandCategorySummary(GrandCategory grandCategory)
    {
        return new GrandCategorySummary
        {
            Id = grandCategory.Id,
            Name = grandCategory.Name,
            Categories = { (grandCategory.Categories ?? Array.Empty<Category>()).Select(MapCategorySummary) }
        };
    }

    private static CatalogMutationReply Success(int id, string message) => new()
    {
        Success = true,
        Message = message,
        Id = id
    };

    private static CatalogMutationReply Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
