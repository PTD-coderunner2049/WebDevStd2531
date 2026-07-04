using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.AppData;
using OrderService.Settings;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();

var connectionString = builder.Configuration.GetConnectionString("OrderConnection")
    ?? builder.Configuration.GetConnectionString("IdentityContextConnection")
    ?? throw new InvalidOperationException("Connection string for order database not found.");

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<OrderJwtOptions>(builder.Configuration.GetSection(OrderJwtOptions.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(OrderJwtOptions.SectionName).Get<OrderJwtOptions>()
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
builder.Services.AddSingleton<CatalogGrpcClient>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<OrderGrpcService>();
app.MapGet("/", () => "OrderService is running.");
app.MapGet("/health", async (OrderDbContext db) =>
{
    var databaseHealthy = await db.Database.CanConnectAsync();
    return databaseHealthy
        ? Results.Ok(new { status = "Healthy", database = "Healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.Run();
