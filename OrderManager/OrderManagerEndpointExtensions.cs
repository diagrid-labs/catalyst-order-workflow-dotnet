using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.Domain;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager;

public static class OrderManagerEndpointExtensions
{
    public static void MapWorkerEndpoints(this WebApplication app)
    {
        app.MapPost("promotion", SendPromotion);

        app.MapPost("inventory/search", SearchInventory);
        app.MapGet("inventory/{productId}", ShowProduct);

        app.MapPost("order", CreateOrder);
        app.MapGet("order/{orderId}", ShowOrder);
    }

    public static async Task<Ok<CreateOrderResult>> CreateOrder(
        [FromServices] DaprWorkflowClient workflowClient,
        [FromBody] CreateOrderRequest request
    )
    {
        var orderId = request.OrderId ?? Guid.CreateVersion7().ToString();

        var orderPayload = new OrderPayload
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            Items = request.Items,
            TotalAmount = request.Items.Sum(i => i.Price * i.Quantity),
            CreatedAt = DateTime.UtcNow,
        };

        await workflowClient.ScheduleNewWorkflowAsync(name: nameof(OrderProcessingWorkflow), input: orderPayload, instanceId: orderId);

        return TypedResults.Ok(new CreateOrderResult
        {
            OrderId = orderId,
            Message = "Order processing started",
            WorkflowInstanceId = orderId,
        });
    }

    public static async Task<Ok<SendPromotionResult>> SendPromotion(
        [FromServices] DaprClient daprClient,
        [FromBody] SendPromotionRequest request
    )
    {
        var promotion = new PromotionNotification
        {
            PromotionId = Guid.NewGuid().ToString(),
            PromotionType = request.PromotionType,
            Message = request.Message,
            TargetAudience = request.TargetAudience,
            SentAt = DateTime.UtcNow,
        };

        await daprClient.PublishEventAsync(ShopActivityPubSub.PubSubName, ShopActivityPubSub.PromotionsTopic, promotion);

        return TypedResults.Ok(new SendPromotionResult
        {
            Id = promotion.PromotionId,
        });
    }

    public static async Task<Ok<InventorySearchResult>> SearchInventory(
        [FromServices] DaprClient daprClient,
        [FromBody] SearchInventoryRequest request
    )
    {
        var httpClient = daprClient.CreateInvokableHttpClient(ResourceNames.InventoryService);

        var response = await httpClient.PostAsJsonAsync("/inventory/search", request);

        if (! response.IsSuccessStatusCode) throw new("Failed to retrieve inventory.");

        var inventoryResponse = await response.Content.ReadFromJsonAsync<InventorySearchResult>(JsonSerializerOptions.Default);

        return TypedResults.Ok(inventoryResponse);
    }

    public static async Task<Results<Ok<Product>, NotFound>> ShowProduct(
        [FromServices] DaprClient daprClient,
        [FromRoute] string productId
    )
    {
        var httpClient = daprClient.CreateInvokableHttpClient(ResourceNames.InventoryService);

        var response = await httpClient.GetAsync($"/inventory/{productId}");

        if (! response.IsSuccessStatusCode) throw new(await response.Content.ReadAsStringAsync());

        var inventoryData = await response.Content.ReadFromJsonAsync<Product>(JsonSerializerOptions.Default);

        return TypedResults.Ok(inventoryData);
    }

    public static async Task<Results<Ok<ShowOrderResult>, NotFound>> ShowOrder(
        [FromServices] DaprWorkflowClient workflowClient,
        [FromRoute] string orderId
    )
    {
        var state = await workflowClient.GetWorkflowStateAsync(orderId);

        if (! state.Exists) return TypedResults.NotFound();

        return TypedResults.Ok(new ShowOrderResult
        {
            OrderId = orderId,
            Status = state.RuntimeStatus.ToString(),
            State = state,
        });
    }
}
