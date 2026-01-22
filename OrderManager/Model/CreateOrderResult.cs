namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

public record CreateOrderResult
{
    public required string OrderId { get; init; }
    public required string Message { get; init; }
    public required string WorkflowInstanceId { get; init; }
}
