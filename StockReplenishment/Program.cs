using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using StockReplenishment.Background;
using StockReplenishment.Components;
using StockReplenishment.Data;
using StockReplenishment.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC / Controllers
builder.Services.AddControllers();

// EF Core In-Memory
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("StockReplenishmentDb");
});

// Business services
builder.Services.AddScoped<IReplenishmentService,
    ReplenishmentService>();

builder.Services.AddScoped<IStockAvailabilityService,
    FakeStockAvailabilityService>();

// API Client
builder.Services.AddHttpClient<ReplenishmentApiClient>();

// Background queue
builder.Services.AddSingleton<IStockValidationQueue,
    StockValidationQueue>();

// Background worker
builder.Services.AddHostedService<StockValidationWorker>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

// IMPORTANT: serve static assets
app.MapStaticAssets();

// REST API
app.MapControllers();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await DbSeeder.SeedAsync(db);
}

app.Run();