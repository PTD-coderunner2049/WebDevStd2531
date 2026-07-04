namespace CatalogService.Services;

public static class CatalogSeeder
{
    public static Task EnsureSeededAsync(IServiceProvider serviceProvider)
    {
        return Task.CompletedTask;
    }
}
