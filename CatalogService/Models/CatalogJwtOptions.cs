namespace CatalogService.Models;

public class CatalogJwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "CatalogService";
    public string Audience { get; set; } = "WebDevStd2531";
    public string Key { get; set; } = "DevOnly_ChangeThis_For_Real_Projects_1234567890!";
    public int ExpiresMinutes { get; set; } = 120;
}
