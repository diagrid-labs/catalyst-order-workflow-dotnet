using System;
using System.Text.Json;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.Configure<JsonOptions>((options) =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddDaprClient((daprBuilder) =>
{
    daprBuilder.UseJsonSerializationOptions(new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    });
});

builder.Services.AddDaprWorkflow((options) =>
{
    options.RegisterWorkflow<OrderProcessingWorkflow>();
    options.RegisterActivity<ValidateOrderActivity>();
    options.RegisterActivity<ProcessPaymentActivity>();
    options.RegisterActivity<CheckInventoryActivity>();
    options.RegisterActivity<UpdateInventoryActivity>();
    options.RegisterActivity<SendNotificationActivity>();
});

var app = builder.Build();

Console.WriteLine("Starting Order Manager Service...");
Console.WriteLine("Workflow engine configured with 5 activities");
Console.WriteLine("API available at http://localhost:8081");

app.UseCloudEvents();

app.MapHealthChecks("/healthz");
app.MapOpenApi();
app.MapScalarApiReference();

app.MapWorkerEndpoints();

Console.WriteLine("Order Manager Service ready!");

app.Run();
