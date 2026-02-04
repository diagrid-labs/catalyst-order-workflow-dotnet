using System.IO;
using System.Reflection;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.Development.AppHost;

public static class AppHostExtensions
{
    public static readonly string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new("Where am I?");

    public static void ConfigureForLocal(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> orderManager, IResourceBuilder<ProjectResource> inventoryService, IResourceBuilder<ProjectResource> notificationService)
    {
        // Configure a cache to hold our state and workflows.
        var cachePassword = builder.AddParameter("cache-password", "zxczxc123", secret: true);
        var cache = builder
            .AddValkey("cache", 16379, cachePassword)
            .WithContainerName("catalyst-order-workflow-cache")
            .WithDataVolume("catalyst-order-workflow-cache-data")
        ;

        orderManager.WaitFor(cache);
        inventoryService.WaitFor(cache);
        notificationService.WaitFor(cache);

        orderManager.WithDaprSidecar(new DaprSidecarOptions
        {
            LogLevel = "debug",
            ResourcesPaths =
            [
                Path.Join(ExecutingPath, "Resources"),
            ],
        });

        inventoryService.WithDaprSidecar(new DaprSidecarOptions
        {
            LogLevel = "debug",
            ResourcesPaths =
            [
                Path.Join(ExecutingPath, "Resources"),
            ],
        });

        notificationService.WithDaprSidecar(new DaprSidecarOptions
        {
            LogLevel = "debug",
            ResourcesPaths =
            [
                Path.Join(ExecutingPath, "Resources"),
            ],
        });

        builder
            .AddContainer("diagrid-dashboard", "ghcr.io/diagridio/diagrid-dashboard:latest")
            .WithContainerName("catalyst-order-workflow-diagrid-dashboard")
            .WithBindMount(Path.Join(ExecutingPath, "Resources"), "/app/components")
            .WithEnvironment("COMPONENT_FILE", "/app/components/inventory-store-diagrid-dashboard.yaml")
            .WithEnvironment("APP_ID", "diagrid-dashboard")
            .WithHttpEndpoint(targetPort: 8080)
            .WithReference(cache)
        ;
    }

    public static void ConfigureForCatalyst(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> orderManager, IResourceBuilder<ProjectResource> inventoryService, IResourceBuilder<ProjectResource> notificationService)
    {
        // note: To configure this project to use Catalyst, run the following commands at the root of the AppHost project:
        //
        //     dotnet user-secrets set "OrderManagerCatalystApiToken" "YOUR_CATALYST_ORDER_MANAGER_API_TOKEN_HERE"
        //     dotnet user-secrets set "OrderManagerCatalystGrpcEndpoint" "YOUR_CATALYST_ORDER_MANAGER_GRPC_ENDPOINT_HERE"
        //     dotnet user-secrets set "OrderManagerCatalystHttpEndpoint" "YOUR_CATALYST_ORDER_MANAGER_HTTP_ENDPOINT_HERE"
        //     dotnet user-secrets set "InventoryServiceCatalystApiToken" "YOUR_CATALYST_INVENTORY_SERVICE_API_TOKEN_HERE"
        //     dotnet user-secrets set "InventoryServiceCatalystGrpcEndpoint" "YOUR_CATALYST_INVENTORY_SERVICE_GRPC_ENDPOINT_HERE"
        //     dotnet user-secrets set "InventoryServiceCatalystHttpEndpoint" "YOUR_CATALYST_INVENTORY_SERVICE_HTTP_ENDPOINT_HERE"

        var orderManagerCatalystApiToken = builder.Configuration["OrderManagerCatalystApiToken"] ?? throw new("Missing order manager catalyst API token!");
        var orderManagerCatalystGrpcEndpoint = builder.Configuration["OrderManagerCatalystGrpcEndpoint"] ?? throw new("Missing order manager catalyst GRPC endpoint!");
        var orderManagerCatalystHttpEndpoint = builder.Configuration["OrderManagerCatalystHttpEndpoint"] ?? throw new("Missing order manager catalyst HTTP endpoint!");
        var inventoryServiceCatalystApiToken = builder.Configuration["InventoryServiceCatalystApiToken"] ?? throw new("Missing Inventory Service catalyst API token!");
        var inventoryServiceCatalystGrpcEndpoint = builder.Configuration["InventoryServiceCatalystGrpcEndpoint"] ?? throw new("Missing Inventory Service catalyst GRPC endpoint!");
        var inventoryServiceCatalystHttpEndpoint = builder.Configuration["InventoryServiceCatalystHttpEndpoint"] ?? throw new("Missing Inventory Service catalyst HTTP endpoint!");

        var orderManagerCatalystApiTokenParameter = builder.AddParameter("catalyst-order-manager-api-token", orderManagerCatalystApiToken, secret: true);
        var orderManagerCatalystGrpcEndpointParameter = builder.AddParameter("catalyst-order-manager-grpc-endpoint", orderManagerCatalystGrpcEndpoint);
        var orderManagerCatalystHttpEndpointParameter = builder.AddParameter("catalyst-order-manager-http-endpoint", orderManagerCatalystHttpEndpoint);

        // Instead of running a dapr sidecar, we provide credentials to connect to Catalyst as a sidecar.
        orderManager
            .WithEnvironment("APP_ID", "order-manager")
            .WithEnvironment("APP_PORT", "8081")
            .WithEnvironment("DAPR_API_TOKEN", orderManagerCatalystApiTokenParameter)
            .WithEnvironment("DAPR_GRPC_ENDPOINT", orderManagerCatalystGrpcEndpointParameter)
            .WithEnvironment("DAPR_HTTP_ENDPOINT", orderManagerCatalystHttpEndpointParameter)
        ;

        builder
            .AddExecutable("catalyst-order-manager-proxy",
                "diagrid",
                ExecutingPath,
                [
                    "dev", "run", "--approve",
                    "--project", "catalyst-order-workflow-local",
                    "--app-id", "order-manager",
                    "--app-port", "8081",
                ])
            .WaitFor(orderManager)
            .WithParentRelationship(orderManager)
        ;

        var inventoryServiceCatalystApiTokenParameter = builder.AddParameter("catalyst-inventory-service-api-token", inventoryServiceCatalystApiToken, secret: true);
        var inventoryServiceCatalystGrpcEndpointParameter = builder.AddParameter("catalyst-inventory-service-grpc-endpoint", inventoryServiceCatalystGrpcEndpoint);
        var inventoryServiceCatalystHttpEndpointParameter = builder.AddParameter("catalyst-inventory-service-http-endpoint", inventoryServiceCatalystHttpEndpoint);

        inventoryService
            .WithEnvironment("APP_ID", "inventory-service")
            .WithEnvironment("APP_PORT", "8081")
            .WithEnvironment("DAPR_API_TOKEN", inventoryServiceCatalystApiTokenParameter)
            .WithEnvironment("DAPR_GRPC_ENDPOINT", inventoryServiceCatalystGrpcEndpointParameter)
            .WithEnvironment("DAPR_HTTP_ENDPOINT", inventoryServiceCatalystHttpEndpointParameter)
        ;

        builder
            .AddExecutable("catalyst-inventory-service-proxy",
                "diagrid",
                ExecutingPath,
                [
                    "dev", "run", "--approve",
                    "--project", "catalyst-order-workflow-local",
                    "--app-id", "inventory-service",
                    "--app-port", "8081",
                ])
            .WaitFor(inventoryService)
            .WithParentRelationship(inventoryService)
        ;

        // Configure notification service for Catalyst
        // Note: Add your notification service Catalyst credentials to user-secrets if needed
        notificationService
            .WithEnvironment("APP_ID", "notification-service")
            .WithEnvironment("APP_PORT", "8083")
        ;
    }
}
