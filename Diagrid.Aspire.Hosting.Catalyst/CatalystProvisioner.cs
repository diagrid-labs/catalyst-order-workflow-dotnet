namespace Diagrid.Aspire.Hosting.Catalyst;

/// <summary>
///     A swappable abstraction for interacting with Catalyst.
/// </summary>
public interface CatalystProvisioner
{
    /// <summary>
    ///     In case any provisioner requires some setup, fine to noop.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Init(CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a project in Catalyst.
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="cancellationToken"></param>
    Task CreateProject(string projectName, CancellationToken cancellationToken);

    /// <summary>
    ///     Use an existing project in Catalyst.
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UseProject(string projectName, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets details about a Catalyst project.
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ProjectDetails> GetProjectDetails(string projectName, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates an app in Catalyst.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreateApp(string name, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets details about an app in Catalyst.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AppDetails> GetAppDetails(string name, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a component in Catalyst.
    /// </summary>
    /// <param name="descriptor"></param>
    /// <param name="projectName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreateComponent(ComponentDescriptor descriptor, string projectName, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a Catalyst pubsub service.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreatePubSub(string name, PubSubDescriptor options, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a Catalyst KV store.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreateKvStore(string name, KvStoreDescriptor options, CancellationToken cancellationToken);
}

public record ProjectDetails
{
    public required Uri HttpEndpoint { get; init; }

    public required Uri GrpcEndpoint { get; init; }
}

public record AppDetails
{
    public required string ApiToken { get; init; }
}

public record PubSubDescriptor
{
    public required string Project { get; init; }
    public IList<string> Scopes { get; init; } = [];
}

public record KvStoreDescriptor
{
    public required string Project { get; init; }
    public IList<string> Scopes { get; init; } = [];
}

public record ComponentDescriptor
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public IList<string> Scopes { get; init; } = [];
    public required IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
