using System;
using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class SendNotificationActivity(DaprClient daprClient) : WorkflowActivity<NotificationRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, NotificationRequest request)
    {
        Console.WriteLine($"Sent '{request.Status}' notification for Order ID: {request.OrderId}");
        
        var orderNotification = new OrderStatusNotification
        {
            OrderId = request.OrderId,
            Status = request.Status,
            Message = request.Message,
            Timestamp = DateTime.UtcNow,
        };

        await daprClient.PublishEventAsync(ShopActivityPubSub.PubSubName, ShopActivityPubSub.OrderTopic, orderNotification);

        return true;
    }
}
