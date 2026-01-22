using System;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.Domain;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;
using InventoryItem = Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model.InventoryItem;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager;

public class OrderProcessingWorkflow : Workflow<OrderPayload, OrderResult>
{
    public override async Task<OrderResult> RunAsync(WorkflowContext context, OrderPayload order)
    {
        var orderId = order.OrderId;

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "created", $"Order {orderId} has been created and is being processed")
        );

        var validationResult = await context.CallActivityAsync<ValidationResult>(
            nameof(ValidateOrderActivity),
            new OrderValidationRequest(orderId, order.CustomerId, order.Items)
        );

        if (! validationResult.IsValid)
        {
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} validation failed: {validationResult.Reason}")
            );

            return new(false, $"Order validation failed: {validationResult.Reason}");
        }

        var paymentResult = await context.CallActivityAsync<PaymentResult>(
            nameof(ProcessPaymentActivity),
            new PaymentRequest(orderId, order.CustomerId, order.TotalAmount)
        );

        if (! paymentResult.Success)
        {
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} payment failed: {paymentResult.Reason}")
            );

            return new(false, $"Payment processing failed: {paymentResult.Reason}");
        }

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "payment_processed", $"Payment for order {orderId} has been processed successfully")
        );

        var inventoryCheckRequest = new InventoryCheckRequest
        {
            OrderId = orderId,
            Items = order.Items.Select(item => new InventoryItem(item.ProductId, item.Quantity)).ToList(),
        };

        var inventoryCheckResponse = await context.CallActivityAsync<InventorySearchResult>(
            nameof(CheckInventoryActivity),
            inventoryCheckRequest
        );

        if (inventoryCheckResponse.OutOfStockItems is { Count: >= 1 } outOfStockItems)
        {
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Items {string.Join(", ", outOfStockItems.Select(item => item.ProductId))} are out of stock for order {orderId}")
            );
        }

        var inventoryResult = await context.CallActivityAsync<InventoryUpdateResult>(
            nameof(UpdateInventoryActivity),
            new InventoryUpdateRequest(orderId, order.Items, "reserve")
        );

        if (! inventoryResult.Success)
        {
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} inventory update failed: {inventoryResult.Reason}")
            );

            return new(false, $"Inventory update failed: {inventoryResult.Reason}");
        }

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "shipped", $"Order {orderId} has been shipped and is on its way")
        );

        await context.CreateTimer(TimeSpan.FromSeconds(20));

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "delivered", $"Order {orderId} has been delivered successfully")
        );

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "completed", $"Order {orderId} processing completed successfully")
        );

        return new(true, "Order processed successfully");
    }
}
