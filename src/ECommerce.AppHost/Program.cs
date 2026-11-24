using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var catalogDb = postgres.AddDatabase("catalogdb");
var usersDb = postgres.AddDatabase("usersdb");
var ordersDb = postgres.AddDatabase("ordersdb");
var inventoryDb = postgres.AddDatabase("inventorydb");
var notificationsDb = postgres.AddDatabase("notificationsdb");

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// APIs
var productCatalogApi = builder.AddDotnetProject("productcatalogapi", "../ECommerce.ProductCatalog.Api/ECommerce.ProductCatalog.Api.csproj")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WaitFor(catalogDb)
    .WaitFor(redis);

var userManagementApi = builder.AddDotnetProject("usermanagementapi", "../ECommerce.UserManagement.Api/ECommerce.UserManagement.Api.csproj")
    .WithReference(usersDb)
    .WaitFor(usersDb);

var inventoryApi = builder.AddDotnetProject("inventoryapi", "../ECommerce.Inventory.Api/ECommerce.Inventory.Api.csproj")
    .WithReference(inventoryDb)
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WaitFor(inventoryDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

var ordersApi = builder.AddDotnetProject("ordersapi", "../ECommerce.Orders.Api/ECommerce.Orders.Api.csproj")
    .WithReference(ordersDb)
    .WithReference(rabbitMq)
    .WithReference(inventoryApi)
    .WithReference(userManagementApi)
    .WithReference(productCatalogApi)
    .WaitFor(ordersDb)
    .WaitFor(rabbitMq);

var notificationsApi = builder.AddDotnetProject("notificationsapi", "../ECommerce.Notifications.Api/ECommerce.Notifications.Api.csproj")
    .WithReference(notificationsDb)
    .WithReference(rabbitMq)
    .WithReference(userManagementApi)
    .WaitFor(notificationsDb)
    .WaitFor(rabbitMq);

// Web UI
builder.AddDotnetProject("webfrontend", "../ECommerce.Web/ECommerce.Web.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(productCatalogApi)
    .WithReference(userManagementApi)
    .WithReference(ordersApi)
    .WithReference(inventoryApi)
    .WaitFor(productCatalogApi)
    .WaitFor(userManagementApi)
    .WaitFor(ordersApi)
    .WaitFor(inventoryApi);

builder.Build().Run();
