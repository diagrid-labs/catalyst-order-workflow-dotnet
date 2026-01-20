using System.Collections.Generic;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

public record SearchInventoryRequest(List<InventoryItem> Items);

public record InventoryItem(string ProductId, int Quantity);

public record InventoryCheckRequest
{
    public string OrderId { get; set; } = string.Empty;
    public List<InventoryItem> Items { get; set; } = new();
}
