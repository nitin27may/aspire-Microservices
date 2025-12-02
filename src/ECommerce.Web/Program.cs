using Blazored.LocalStorage;
using ECommerce.Web.Components;
using ECommerce.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredLocalStorage();

// Register HTTP clients for APIs
builder.Services.AddHttpClient<ProductService>(client =>
{
    client.BaseAddress = new Uri("https+http://productcatalogapi");
});

builder.Services.AddHttpClient<AuthService>(client =>
{
    client.BaseAddress = new Uri("https+http://usermanagementapi");
});

builder.Services.AddHttpClient<OrderService>(client =>
{
    client.BaseAddress = new Uri("https+http://ordersapi");
});

builder.Services.AddHttpClient<InventoryService>(client =>
{
    client.BaseAddress = new Uri("https+http://inventoryapi");
});

builder.Services.AddScoped<CartService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
