using System;
using System.Collections.Generic;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

public record OrderResult(bool Success, string Message);

public record OrderValidationRequest(string OrderId, string CustomerId, List<OrderItem> Items);

public record ValidationResult(bool IsValid, string Reason);

public record PaymentRequest(string OrderId, string CustomerId, decimal Amount);

public record PaymentResult(bool Success, string Reason);

public record InventoryUpdateRequest(string OrderId, List<OrderItem> Items, string Operation = "reserve");

public record InventoryUpdateResult(bool Success, string Reason);

public record NotificationRequest(string OrderId, string Status, string Message);

public record OrderStatusNotification
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public record InventoryUpdateResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}