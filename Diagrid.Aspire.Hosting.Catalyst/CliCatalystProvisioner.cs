namespace Diagrid.Aspire.Hosting.Catalyst;

public class CliCatalystProvisioner : CatalystProvisioner
{
    public async Task Init(CancellationToken cancellationToken = default)
    {
        // todo: Need to not have to do this.
        await Commands.UseCatalyst(cancellationToken);
    }

    public async Task CreateProject(string projectName, CancellationToken cancellationToken)
    {
        try
        {
            await Commands.CreateProject(projectName, cancellationToken: cancellationToken);
        }
        catch
        {
            // todo: Would like a silent/idempotent create.
        }
    }

    public async Task UseProject(string projectName, CancellationToken cancellationToken)
    {
        await Commands.UseProject(projectName, cancellationToken);
    }

    public async Task<ProjectDetails> GetProjectDetails(string projectName, CancellationToken cancellationToken)
    {
        var cliDetails = await Commands.GetProjectDetails(projectName, cancellationToken);

        return new()
        {
            HttpEndpoint = cliDetails.Status.Endpoints.Http.Uri,
            GrpcEndpoint = cliDetails.Status.Endpoints.Grpc.Uri,
        };
    }

    public async Task CreateApp(string name, CancellationToken cancellationToken)
    {
        try
        {
            await Commands.CreateApp(name, cancellationToken: cancellationToken);
        }
        catch
        {
            // todo: Would like a silent/idempotent create.
        }
    }

    public async Task<AppDetails> GetAppDetails(string name, CancellationToken cancellationToken)
    {
        var appDetails = await Commands.GetAppDetails(name, cancellationToken);

        return new()
        {
            ApiToken = appDetails.Status.ApiToken,
        };
    }
}
