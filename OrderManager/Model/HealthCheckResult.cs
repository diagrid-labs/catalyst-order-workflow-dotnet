namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

public record HealthCheckResult
{
    public required string Status { get; init; }
    public required string Message { get; init; }
    public required string Service { get; init; }
}
