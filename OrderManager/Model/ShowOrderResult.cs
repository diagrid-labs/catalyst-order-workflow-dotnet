using Dapr.Workflow;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

public record ShowOrderResult
{
    public required string OrderId { get; init; }
    public required string Status { get; init; }
    public required WorkflowState State { get; init; }
}
