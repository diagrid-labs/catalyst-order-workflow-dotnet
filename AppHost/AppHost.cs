using System;
using Aspire.Hosting;
using Diagrid.Labs.Catalyst.OrderWorkflow.Development.AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var useCatalyst = Environment.GetEnvironmentVariable("USE_CATALYST") switch
{
    "1" or "true" => true,
    _ => false,
};

builder.AddDapr();

var orderManager = builder.AddProject<OrderManager>("order-manager");
var inventoryService = builder.AddProject<InventoryService>("inventory-service");

if (useCatalyst) builder.ConfigureForCatalyst(orderManager, inventoryService);
else builder.ConfigureForLocal(orderManager, inventoryService);

builder.Build().Run();
