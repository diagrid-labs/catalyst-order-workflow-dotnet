using Microsoft.Extensions.DependencyInjection;

namespace Diagrid.Aspire.Hosting.Catalyst;

public static class ResourceBuilderExtensions
{
    /// <summary>
    ///     Associates an Aspire orchestration to a Catalyst project.
    /// </summary>
    /// <param name="applicationBuilder"></param>
    /// <param name="customProjectName"></param>
    public static void AddCatalystProject(this IDistributedApplicationBuilder applicationBuilder, string? customProjectName = null)
    {
        applicationBuilder.Services.AddSingleton<CatalystProvisioner, CliCatalystProvisioner>();

        // todo: Either replace `"aspire"` here with a default inferred from `applicationBuilder`, or make `projectName` a required param.
        var projectName = customProjectName ?? "aspire";

        // todo: Custom icon, support pending https://github.com/dotnet/aspire/issues/8684
        var catalystProject = applicationBuilder.AddResource(new CatalystProjectResource
        {
            ProjectName = projectName,
        });

        catalystProject.WithAnnotation(new ResourceUrlAnnotation
        {
            Url = "https://google.com",
            DisplayText = "Project Dashboard",
        });

        applicationBuilder.Eventing.Subscribe<BeforeStartEvent>(Events.EnsureCatalystProvisioning);
    }

    /// <summary>
    ///     Configures a project to use Catalyst.
    /// </summary>
    /// <param name="projectResource"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IResourceBuilder<ProjectResource> WithCatalyst(this IResourceBuilder<ProjectResource> projectResource)
    {
        var applicationBuilder = projectResource.ApplicationBuilder;
        var catalystProjectBuilder = applicationBuilder.CreateResourceBuilder(applicationBuilder.EnsureCatalystResource());

        projectResource.WaitForCompletion(catalystProjectBuilder);

        var httpEndpoint = projectResource.Resource.GetEndpoints().FirstOrDefault((endpoint) => endpoint.IsHttp);
        if (httpEndpoint is not null)
        {
            var catalystProxy = applicationBuilder.AddExecutable(
                $"{projectResource.Resource.Name}-catalyst-proxy",
                "diagrid",
                // todo: I want a better, more explicit PWD than this.
                ".",
                [
                    "dev", "run", "--approve",
                    "--project", catalystProjectBuilder.Resource.ProjectName,
                    "--app-id", projectResource.Resource.Name,
                    "--app-port", httpEndpoint.Property(EndpointProperty.Port),
                ]
            )
            .WithReference(projectResource)
            .WaitForCompletion(catalystProjectBuilder);

            projectResource
                .WaitFor(catalystProxy)
                .WithChildRelationship(catalystProxy);

            catalystProjectBuilder.Resource.AppDetails[projectResource.Resource] = new();
        }

        projectResource.WithEnvironment(async (context) =>
        {
            var applicationModel = (DistributedApplicationModel) context.ExecutionContext.ServiceProvider
                .GetRequiredService(typeof(DistributedApplicationModel));

            var catalystProject = applicationModel.Resources.FirstOrDefault((resource) => resource is CatalystProjectResource) as CatalystProjectResource
                ?? throw new("This project is missing a Catalyst project resource.");

            var appDetails = await catalystProject.AppDetails[projectResource.Resource].Task;

            context.EnvironmentVariables["DAPR_GRPC_ENDPOINT"] = await catalystProject.GrpcEndpoint.Task;
            context.EnvironmentVariables["DAPR_HTTP_ENDPOINT"] = await catalystProject.HttpEndpoint.Task;
            context.EnvironmentVariables["DAPR_API_TOKEN"] = appDetails.ApiToken;
        });

        return projectResource;
    }

    /// <summary>
    ///     Configures a container to use Catalyst.
    /// </summary>
    /// <param name="containerResource"></param>
    /// <returns></returns>
    public static IResourceBuilder<ContainerResource> WithCatalyst(this IResourceBuilder<ContainerResource> containerResource)
    {
        var applicationBuilder = containerResource.ApplicationBuilder;

        applicationBuilder.EnsureCatalystResource();

        return containerResource;
    }

    /// <summary>
    ///     Ensures the user has configured a Catalyst project as part of their orchestration.
    /// </summary>
    /// <param name="applicationBuilder"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static CatalystProjectResource EnsureCatalystResource(this IDistributedApplicationBuilder applicationBuilder)
    {
        if (applicationBuilder.Resources.SingleOrDefault((resource) => resource is CatalystProjectResource) is not CatalystProjectResource catalystProject)
            throw new($"Remember to configure your Catalyst project by calling {nameof(AddCatalystProject)}.");

        return catalystProject;
    }
}
