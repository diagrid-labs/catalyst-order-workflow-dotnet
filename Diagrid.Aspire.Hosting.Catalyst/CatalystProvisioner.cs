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

    Task<AppDetails> GetAppDetails(string name, CancellationToken cancellationToken);
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
