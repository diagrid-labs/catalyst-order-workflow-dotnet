using Dapr;
using Dapr.Client;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService.Hubs;
using Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService.Models;
using Microsoft.AspNetCore.SignalR;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService;

public static class NotificationServiceEndpointExtensions
{
    private static readonly List<NotificationViewModel> NotificationHistory = new();
    private static readonly object LockObject = new();

    public static IEndpointRouteBuilder MapNotificationServiceEndpoints(this IEndpointRouteBuilder app)
    {
        // Pub/Sub subscription handler for order notifications
        app.MapPost("/order-notification", 
            [Topic(ShopActivityPubSub.PubSubName, ShopActivityPubSub.OrderTopic)]
            async (OrderStatusNotification notification, IHubContext<NotificationHub> hubContext) =>
        {
            Console.WriteLine($"Received order notification - Order ID: {notification.OrderId}, Status: {notification.Status}");
            var viewModel = new NotificationViewModel
            {
                Type = "order",
                Title = $"Order {notification.Status}",
                Message = notification.Message,
                Timestamp = notification.Timestamp,
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", notification.OrderId },
                    { "Status", notification.Status }
                }
            };

            lock (LockObject)
            {
                NotificationHistory.Add(viewModel);
                // Keep only the last 100 notifications
                if (NotificationHistory.Count > 100)
                {
                    NotificationHistory.RemoveAt(0);
                }
            }

            // Broadcast to all connected clients
            await hubContext.Clients.All.SendAsync("ReceiveNotification", viewModel);
            Console.WriteLine($"Broadcasted order notification to all connected clients");

            return Results.Ok();
        })
        .WithName("OrderNotification")
        .WithOpenApi();

        // Get notification history
        app.MapGet("/notifications/history", () =>
        {
            lock (LockObject)
            {
                var count = NotificationHistory.Count;
                Console.WriteLine($"History requested - Returning {count} notifications");
                return Results.Ok(NotificationHistory.OrderByDescending(n => n.Timestamp).ToList());
            }
        })
        .WithName("GetNotificationHistory")
        .WithOpenApi();

        // Create a new order by calling OrderManager via Dapr service invocation
        app.MapPost("/order", async (CreateOrderRequest request, DaprClient daprClient) =>
        {
            Console.WriteLine($"Creating new order - Customer: {request.CustomerId}, Items: {request.Items.Count}");
            
            var httpClient = daprClient.CreateInvokableHttpClient(ResourceNames.OrderManager);
            
            try
            {
                var response = await httpClient.PostAsJsonAsync("/order", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateOrderResult>();
                    Console.WriteLine($"Order created successfully - Order ID: {result?.OrderId}");
                    return Results.Ok(result);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create order - Status: {response.StatusCode}, Error: {errorContent}");
                    return Results.Problem(
                        detail: errorContent,
                        statusCode: (int)response.StatusCode,
                        title: "Failed to create order"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating order: {ex.Message}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating order"
                );
            }
        })
        .WithName("CreateOrder")
        .WithOpenApi();

        return app;
    }
}
