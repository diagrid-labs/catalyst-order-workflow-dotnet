using System;
using System.Collections.Generic;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

public record CreateOrderRequest(string? OrderId, string CustomerId, List<OrderItem> Items);

public record OrderItem(string ProductId, int Quantity, decimal Price);

public record OrderPayload
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
