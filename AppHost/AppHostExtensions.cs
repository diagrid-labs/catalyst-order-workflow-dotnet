using System.IO;
using System.Reflection;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.Development.AppHost;

public static class AppHostExtensions
{
    public static readonly string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new("Where am I?");

    public static void ConfigureForCatalyst(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> worker, IResourceBuilder<ProjectResource> inventoryService)
    {
        // note: To configure this project to use Catalyst, run the following commands at the root of the AppHost project:
        //
        //     dotnet user-secrets set "WorkerCatalystApiToken" "YOUR_CATALYST_WORKER_API_TOKEN_HERE"
        //     dotnet user-secrets set "WorkerCatalystGrpcEndpoint" "YOUR_CATALYST_WORKER_GRPC_ENDPOINT_HERE"
        //     dotnet user-secrets set "WorkerCatalystHttpEndpoint" "YOUR_CATALYST_WORKER_HTTP_ENDPOINT_HERE"
        //     dotnet user-secrets set "InventoryServiceCatalystApiToken" "YOUR_CATALYST_INVENTORY_SERVICE_API_TOKEN_HERE"
        //     dotnet user-secrets set "InventoryServiceCatalystGrpcEndpoint" "YOUR_CATALYST_INVENTORY_SERVICE_GRPC_ENDPOINT_HERE"
        //     dotnet user-secrets set "InventoryServiceCatalystHttpEndpoint" "YOUR_CATALYST_INVENTORY_SERVICE_HTTP_ENDPOINT_HERE"

        var workerCatalystApiToken = builder.Configuration["WorkerCatalystApiToken"] ?? throw new("Missing worker catalyst API token!");
        var workerCatalystGrpcEndpoint = builder.Configuration["WorkerCatalystGrpcEndpoint"] ?? throw new("Missing worker catalyst GRPC endpoint!");
        var workerCatalystHttpEndpoint = builder.Configuration["WorkerCatalystHttpEndpoint"] ?? throw new("Missing worker catalyst HTTP endpoint!");
        var inventoryServiceCatalystApiToken = builder.Configuration["InventoryServiceCatalystApiToken"] ?? throw new("Missing InventoryService catalyst API token!");
        var inventoryServiceCatalystGrpcEndpoint = builder.Configuration["InventoryServiceCatalystGrpcEndpoint"] ?? throw new("Missing InventoryService catalyst GRPC endpoint!");
        var inventoryServiceCatalystHttpEndpoint = builder.Configuration["InventoryServiceCatalystHttpEndpoint"] ?? throw new("Missing InventoryService catalyst HTTP endpoint!");

        var workerCatalystApiTokenParameter = builder.AddParameter("catalyst-worker-api-token", workerCatalystApiToken, secret: true);
        var workerCatalystGrpcEndpointParameter = builder.AddParameter("catalyst-worker-grpc-endpoint", workerCatalystGrpcEndpoint);
        var workerCatalystHttpEndpointParameter = builder.AddParameter("catalyst-worker-http-endpoint", workerCatalystHttpEndpoint);

        // Instead of running a dapr sidecar, we provide credentials to connect to Catalyst as a sidecar.
        worker
            .WithEnvironment("APP_ID", "worker")
            .WithEnvironment("APP_PORT", "8081")
            .WithEnvironment("DAPR_API_TOKEN", workerCatalystApiTokenParameter)
            .WithEnvironment("DAPR_GRPC_ENDPOINT", workerCatalystGrpcEndpointParameter)
            .WithEnvironment("DAPR_HTTP_ENDPOINT", workerCatalystHttpEndpointParameter)
        ;

        builder
            .AddExecutable("catalyst-worker-proxy",
                "diagrid",
                ExecutingPath,
                [
                    "dev", "run", "--approve",
                    "--project", "catalyst-order-workflow-local",
                    "--app-id", "worker",
                    "--app-port", "8081",
                ])
            .WaitFor(worker)
            .WithParentRelationship(worker)
        ;

        var inventoryServiceCatalystApiTokenParameter = builder.AddParameter("catalyst-inventory-service-api-token", inventoryServiceCatalystApiToken, secret: true);
        var inventoryServiceCatalystGrpcEndpointParameter = builder.AddParameter("catalyst-inventory-service-grpc-endpoint", inventoryServiceCatalystGrpcEndpoint);
        var inventoryServiceCatalystHttpEndpointParameter = builder.AddParameter("catalyst-inventory-service-http-endpoint", inventoryServiceCatalystHttpEndpoint);

        inventoryService
            .WithEnvironment("APP_ID", "inventory-service")
            .WithEnvironment("APP_PORT", "8082")
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
                    "--app-port", "8082",
                ])
            .WaitFor(inventoryService)
            .WithParentRelationship(inventoryService)
        ;
    }

    public static void ConfigureForLocal(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> worker, IResourceBuilder<ProjectResource> inventoryService)
    {
        // Configure a cache to hold our state and workflows.
        var cachePassword = builder.AddParameter("cache-password", "zxczxc123", secret: true);
        var cache = builder
            .AddValkey("cache", 6379, cachePassword)
            .WithContainerName("catalyst-order-workflow-cache")
            .WithDataVolume("catalyst-order-workflow-cache-data")
        ;

        worker.WaitFor(cache);
        inventoryService.WaitFor(cache);

        worker.WithDaprSidecar(new DaprSidecarOptions
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

        var diagridDashboard = builder
            .AddContainer("diagrid-dashboard", "public.ecr.aws/d3f9w4q8/local-dash-temp:latest")
            .WithBindMount(Path.Join(ExecutingPath, "Resources"), "/app/components")
            .WithEnvironment("COMPONENT_FILE", "/app/components/inventory-store-diagrid-dashboard.yaml")
            .WithEnvironment("APP_ID", "diagrid-dashboard")
            .WithHttpEndpoint(targetPort: 8080)
            .WithReference(cache)
        ;
    }
}
