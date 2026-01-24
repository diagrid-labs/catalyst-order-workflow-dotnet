namespace Diagrid.Aspire.Hosting.Catalyst;

public class CatalystProjectResource : IResource
{
    public string Name => "catalyst";

    public required string ProjectName { get; init; }

    public ResourceAnnotationCollection Annotations { get; init; } = [];

    public TaskCompletionSource<string> HttpEndpoint { get; } = new();
    public TaskCompletionSource<string> GrpcEndpoint { get; } = new();

    public Dictionary<Resource, TaskCompletionSource<AppDetails>> AppDetails { get; init; } = new();
}
