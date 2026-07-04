using System.Text;
using CatalogService.AppData;
using CatalogService.Models;
using CatalogService.Services;
using CatalogService.Services.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();

var connectionString = builder.Configuration.GetConnectionString("CatalogConnection")
    ?? builder.Configuration.GetConnectionString("IdentityContextConnection")
    ?? throw new InvalidOperationException("Connection string for catalog database not found.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddSingleton<RabbitMqEventPublisher>();

builder.Services.Configure<CatalogJwtOptions>(builder.Configuration.GetSection(CatalogJwtOptions.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(CatalogJwtOptions.SectionName).Get<CatalogJwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    await CatalogSeeder.EnsureSeededAsync(scope.ServiceProvider);
}

app.MapGrpcService<CatalogGrpcService>();
app.MapGet("/", () => "CatalogService is running.");
app.MapGet("/health", async (CatalogDbContext db) =>
{
    var databaseHealthy = await db.Database.CanConnectAsync();
    return databaseHealthy
        ? Results.Ok(new { status = "Healthy", database = "Healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.Run();
