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
var productCatalogApi = builder.AddProject<Projects.ECommerce_ProductCatalog_Api>("productcatalogapi")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WaitFor(catalogDb)
    .WaitFor(redis);

var userManagementApi = builder.AddProject<Projects.ECommerce_UserManagement_Api>("usermanagementapi")
    .WithReference(usersDb)
    .WaitFor(usersDb);

var inventoryApi = builder.AddProject<Projects.ECommerce_Inventory_Api>("inventoryapi")
    .WithReference(inventoryDb)
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WaitFor(inventoryDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

var ordersApi = builder.AddProject<Projects.ECommerce_Orders_Api>("ordersapi")
    .WithReference(ordersDb)
    .WithReference(rabbitMq)
    .WithReference(inventoryApi)
    .WithReference(userManagementApi)
    .WithReference(productCatalogApi)
    .WaitFor(ordersDb)
    .WaitFor(rabbitMq);

var notificationsApi = builder.AddProject<Projects.ECommerce_Notifications_Api>("notificationsapi")
    .WithReference(notificationsDb)
    .WithReference(rabbitMq)
    .WithReference(userManagementApi)
    .WaitFor(notificationsDb)
    .WaitFor(rabbitMq);

// Web UI
builder.AddProject<Projects.ECommerce_Web>("webfrontend")
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
