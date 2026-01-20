using System;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.InventoryService.Models;

public record OrderStatusNotification
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public record PromotionNotification
{
    public string PromotionId { get; set; } = string.Empty;
    public string PromotionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}