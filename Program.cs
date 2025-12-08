using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("IdentityContextConnection") ?? throw new InvalidOperationException("Connection string 'IdentityContextConnection' not found.");;

builder.Services.AddDbContext<AppDBContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()// Enable Roles so I can use RoleManager: lines 21-25
    .AddEntityFrameworkStores<AppDBContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Roles Seeding
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedRoles(roleManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages(); // I added this line to enable Razor Pages after Identity scaffolding added.
app.Run();




async Task SeedRoles(RoleManager<IdentityRole> roleManager)
{
    // doesn't exist, create
    string userRole = "User";
    string adminRole = "Admin";

    if (!await roleManager.RoleExistsAsync(userRole))
        await roleManager.CreateAsync(new IdentityRole(userRole));
    
    if (!await roleManager.RoleExistsAsync(adminRole))
        await roleManager.CreateAsync(new IdentityRole(adminRole));
}