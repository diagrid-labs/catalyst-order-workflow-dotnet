using System;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.InventoryService.Models;

public record OrderStatusNotification
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
