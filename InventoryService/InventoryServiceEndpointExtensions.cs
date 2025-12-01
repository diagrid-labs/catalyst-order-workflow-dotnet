using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapr.Client;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.Domain;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.InventoryService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.InventoryService;

public static class InventoryServiceEndpointExtensions
{
    private static readonly IDictionary<string, int> SampleInventory = new Dictionary<string, int>
    {
        { "prod-001", 50 },
        { "prod-002", 30 },
        { "prod-003", 25 },
        { "prod-004", 40 },
        { "prod-005", 15 },
    };

    public static void MapInventoryServiceEndpoints(this WebApplication app)
    {
        app.MapPost("inventory/search", SearchInventory);
        app.MapGet("inventory/{productId}", ShowProduct);
        app.MapPost("inventory/initialize", InitializeInventory);
        app.MapPost("inventory/update", UpdateInventory);

        app
            .MapPost("order-notification", CreateOrder)
            .WithTopic(ShopActivityPubSub.ResourceName, ShopActivityPubSub.OrderTopic);

        app
            .MapPost("promotion", CreatePromotion)
            .WithTopic(ShopActivityPubSub.ResourceName, ShopActivityPubSub.PromotionsTopic);
    }

    public static async Task<Results<Ok<InventorySearchResult>, NotFound>> SearchInventory(
        [FromServices] DaprClient daprClient,
        [FromBody] InventorySearchRequest request
    )
    {
        var currentInventoryItems = new List<ItemStatus>();
        foreach (var item in request.Items)
        {
            var key = $"inventory:{item.ProductId}";

            var inventoryData = await daprClient.GetStateAsync<Product?>(ResourceNames.InventoryStore, key);

            if (inventoryData is null) return TypedResults.NotFound();

            var currentQuantity = inventoryData.Quantity;
            currentInventoryItems.Add(item with { Quantity = currentQuantity });
        }

        return TypedResults.Ok(new InventorySearchResult
        {
            Statuses = currentInventoryItems,
        });
    }

    public static async Task<Results<Ok<Product>, NotFound>> ShowProduct(
        [FromServices] DaprClient daprClient,
        [FromRoute] string productId
    )
    {
        var inventoryKey = $"inventory:{productId}";
        var inventoryData = await daprClient.GetStateAsync<Product?>(ResourceNames.InventoryStore, inventoryKey);

        if (inventoryData is null) return TypedResults.NotFound();

        return TypedResults.Ok(new Product
        {
            ProductId = inventoryData.ProductId,
            Quantity = inventoryData.Quantity,
            LastUpdated = inventoryData.LastUpdated,
        });
    }

    public static async Task<Ok> InitializeInventory([FromServices] DaprClient daprClient)
    {
        foreach (var item in SampleInventory)
        {
            var inventoryKey = $"inventory:{item.Key}";
            var inventoryData = new Product
            {
                ProductId = item.Key,
                Quantity = item.Value,
                LastUpdated = DateTime.UtcNow,
            };

            await daprClient.SaveStateAsync(ResourceNames.InventoryStore, inventoryKey, inventoryData);
        }

        return TypedResults.Ok();
    }

    public static async Task<Ok<UpdateInventoryResult>> UpdateInventory(
        [FromServices] DaprClient daprClient,
        [FromBody] UpdateInventoryRequest request
    )
    {
        foreach (var item in request.Items)
        {
            var inventoryKey = $"inventory:{item.ProductId}";

            var currentInventory = await daprClient.GetStateAsync<Product?>(ResourceNames.InventoryStore, inventoryKey);

            var currentQuantity = currentInventory?.Quantity ?? 0;

            var newQuantity = request.Operation.ToLower() switch
            {
                "reserve" => Math.Max(0, currentQuantity - item.Quantity),
                "release" or "restock" => currentQuantity + item.Quantity,
                _ => currentQuantity,
            };

            var updatedInventory = new Product
            {
                ProductId = item.ProductId,
                Quantity = newQuantity,
                LastUpdated = DateTime.UtcNow,
            };

            await daprClient.SaveStateAsync(ResourceNames.InventoryStore, inventoryKey, updatedInventory);
        }

        return TypedResults.Ok(new UpdateInventoryResult
        {
            Success = true,
            UpdatedAt = DateTime.UtcNow,
        });
    }

    public static async Task<NoContent> CreatePromotion([FromBody] PromotionNotification notification)
    {
        await Task.CompletedTask;

        switch (notification.PromotionType.ToLower())
        {
            case "flash_sale":
                // ProcessFlashSale(notification);
                break;

            case "seasonal_discount":
                // ProcessSeasonalDiscount(notification);
                break;

            case "loyalty_reward":
                // ProcessLoyaltyReward(notification);
                break;
        }

        return TypedResults.NoContent();
    }
    public static async Task<NoContent> CreateOrder([FromBody] OrderStatusNotification notification)
    {
        await Task.CompletedTask;

        switch (notification.Status.ToLower())
        {
            case "created":
                // ProcessOrderCreated(notification);
                break;
            case "payment_processed":
                // ProcessPaymentProcessed(notification);
                break;
            case "shipped":
                // ProcessOrderShipped(notification);
                break;
            case "delivered":
                // ProcessOrderDelivered(notification);
                break;
            case "completed":
                // ProcessOrderCompleted(notification);
                break;
            default:
                throw new($"Unknown order status: {notification.Status}");
        }

        return TypedResults.NoContent();
    }
}
