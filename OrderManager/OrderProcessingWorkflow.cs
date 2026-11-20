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
        var retryPolicy = new WorkflowRetryPolicy(
            maxNumberOfAttempts: 8,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromSeconds(60)
        );
        var activityOptions = new WorkflowTaskOptions(retryPolicy);

        var orderId = order.OrderId;

        context.SetCustomStatus(new OrderProgress("created", "Order received"));

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "created", $"Order {orderId} has been created and is being processed"),
            activityOptions
        );

        context.SetCustomStatus(new OrderProgress("validating", "Validating order details"));

        var validationResult = await context.CallActivityAsync<ValidationResult>(
            nameof(ValidateOrderActivity),
            new OrderValidationRequest(orderId, order.CustomerId, order.Items),
            activityOptions
        );

        if (! validationResult.IsValid)
        {
            context.SetCustomStatus(new OrderProgress("validation_failed", validationResult.Reason));

            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} validation failed: {validationResult.Reason}"),
                activityOptions
            );

            return new(false, $"Order validation failed: {validationResult.Reason}");
        }

        context.SetCustomStatus(new OrderProgress("processing_payment", "Charging customer"));

        // Payment gets a shorter retry policy so the failed-workflow demo reaches FAILED in ~10s.
        var paymentOptions = new WorkflowTaskOptions(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 4,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromSeconds(10)
        ));

        PaymentResult paymentResult;
        try
        {
            paymentResult = await context.CallActivityAsync<PaymentResult>(
                nameof(ProcessPaymentActivity),
                new PaymentRequest(orderId, order.CustomerId, order.TotalAmount),
                paymentOptions
            );
        }
        catch (WorkflowTaskFailedException ex)
        {
            context.SetCustomStatus(new OrderProgress("payment_failed", "Payment service unavailable after retries"));

            // Retries exhausted: tell the customer, then rethrow so the workflow ends as FAILED.
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} could not be processed: payment service unavailable after retries"),
                activityOptions
            );

            throw new InvalidOperationException($"Order {orderId} failed: {ex.FailureDetails.ErrorMessage}", ex);
        }

        if (! paymentResult.Success)
        {
            context.SetCustomStatus(new OrderProgress("payment_declined", paymentResult.Reason));

            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} payment failed: {paymentResult.Reason}"),
                activityOptions
            );

            return new(false, $"Payment processing failed: {paymentResult.Reason}");
        }

        context.SetCustomStatus(new OrderProgress("payment_processed", "Payment accepted, starting campaign"));

        var paymentNotificationActivity = context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "payment_processed", $"Payment for order {orderId} has been processed successfully"),
            activityOptions
        );

        var subscribeToCampaignActivity = context.CallActivityAsync(nameof(StartCampaignActivity), new StartCampaignInput
        {
            OrderId = orderId,
        });

        await Task.WhenAll([
            paymentNotificationActivity,
            subscribeToCampaignActivity,
        ]);

        context.SetCustomStatus(new OrderProgress("checking_inventory", "Checking stock for requested items"));

        var inventoryCheckRequest = new InventoryCheckRequest
        {
            OrderId = orderId,
            Items = order.Items.Select(item => new InventoryItem(item.ProductId, item.Quantity)).ToList(),
        };

        var inventoryCheckResponse = await context.CallActivityAsync<InventorySearchResult>(
            nameof(CheckInventoryActivity),
            inventoryCheckRequest,
            activityOptions
        );

        if (inventoryCheckResponse.OutOfStockItems is { Count: >= 1 } outOfStockItems)
        {
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Items {string.Join(", ", outOfStockItems.Select(item => item.ProductId))} are out of stock for order {orderId}"),
                activityOptions
            );
        }

        context.SetCustomStatus(new OrderProgress("reserving_inventory", "Reserving stock"));

        var inventoryResult = await context.CallActivityAsync<InventoryUpdateResult>(
            nameof(UpdateInventoryActivity),
            new InventoryUpdateRequest(orderId, order.Items, "reserve"),
            activityOptions
        );

        if (! inventoryResult.Success)
        {
            context.SetCustomStatus(new OrderProgress("inventory_failed", inventoryResult.Reason));

            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                new NotificationRequest(orderId, "failed", $"Order {orderId} inventory update failed: {inventoryResult.Reason}"),
                activityOptions
            );

            return new(false, $"Inventory update failed: {inventoryResult.Reason}");
        }

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "shipped", $"Order {orderId} has been shipped and is on its way"),
            activityOptions
        );

        context.SetCustomStatus(new OrderProgress("shipping", "Handed to shipping workflow"));

        await context.CallChildWorkflowAsync<ShippingWorkflowOutput>(nameof(ShippingWorkflow), new ShippingWorkflowInput
        {
            OrderId = orderId,
        });

        context.SetCustomStatus(new OrderProgress("awaiting_feedback", "Waiting for customer feedback"));

        // note: This demonstrates the importance of keeping any volatile state inside activities.
        var delay = await context.CallActivityAsync<int>(nameof(CustomerFeedbackDelay), true, activityOptions);

        await context.CreateTimer(TimeSpan.FromSeconds(delay));

        // todo: We could generate this using AI in the future!
        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "feedback", $"Order {orderId} received customer feedback!"),
            activityOptions
        );

        context.SetCustomStatus(new OrderProgress("completed", "Order processed successfully"));

        Console.WriteLine($"Order processing completed successfully for Order ID: {orderId}");

        return new(true, $"Order {order.OrderId} processed successfully");
    }
}
