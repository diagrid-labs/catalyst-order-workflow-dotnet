using Microsoft.AspNetCore.SignalR;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService.Hubs;

public class NotificationHub : Hub
{
    public async Task SendNotification(string type, string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", type, message);
    }
}
