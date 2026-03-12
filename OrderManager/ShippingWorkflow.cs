using System;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager;

public class ShippingWorkflow : Workflow<ShippingWorkflowInput, ShippingWorkflowOutput>
{
    public override async Task<ShippingWorkflowOutput> RunAsync(WorkflowContext context, ShippingWorkflowInput input)
    {
        var orderId = input.OrderId;

        await context.CreateTimer(TimeSpan.FromSeconds(10));

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "label_created", $"Shipping label created for order {orderId}")
        );

        await context.CreateTimer(TimeSpan.FromSeconds(5));

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "picked_up", $"Order {orderId} has been picked up by carrier")
        );

        await context.CreateTimer(TimeSpan.FromSeconds(5));

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "out_for_delivery", $"Order {orderId} is out for delivery")
        );

        await context.CreateTimer(TimeSpan.FromSeconds(5));

        await context.CallActivityAsync(
            nameof(SendNotificationActivity),
            new NotificationRequest(orderId, "delivered", $"Order {orderId} has been delivered successfully")
        );

        return new()
        {
        };
    }
}

public record ShippingWorkflowInput
{
    public required string OrderId { get; init; }
}

public record ShippingWorkflowOutput
{
}
