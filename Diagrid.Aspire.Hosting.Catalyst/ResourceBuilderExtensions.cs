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
    /// <param name="resourceBuilder"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IResourceBuilder<ResourceType> WithCatalyst<ResourceType>(this IResourceBuilder<ResourceType> resourceBuilder)
    where ResourceType : Resource, IResourceWithEnvironment, IResourceWithWaitSupport
    {
        var applicationBuilder = resourceBuilder.ApplicationBuilder;
        var catalystProjectBuilder = applicationBuilder.CreateResourceBuilder(applicationBuilder.EnsureCatalystResource());

        resourceBuilder.WaitForCompletion(catalystProjectBuilder);

        if (
            resourceBuilder.Resource is IResourceWithEndpoints resourceWithEndpoints
            && resourceWithEndpoints.GetEndpoints().FirstOrDefault((endpoint) => endpoint.IsHttp) is {} httpEndpoint
        )
        {
            var catalystProxy = applicationBuilder
                .AddExecutable(
                    $"{resourceBuilder.Resource.Name}-catalyst-proxy",
                    "diagrid",
                    // todo: I want a better, more explicit PWD than this.
                    ".",
                    [
                        "dev", "run", "--approve",
                        "--project", catalystProjectBuilder.Resource.ProjectName,
                        "--app-id", resourceBuilder.Resource.Name,
                        "--app-port", httpEndpoint.Property(EndpointProperty.Port),
                    ]
                )
                .WaitForCompletion(catalystProjectBuilder);

            resourceBuilder
                .WaitFor(catalystProxy)
                .WithChildRelationship(catalystProxy);

            catalystProjectBuilder.Resource.AppDetails[resourceBuilder.Resource] = new();
        }

        resourceBuilder.WithEnvironment(async (context) =>
        {
            var applicationModel = (DistributedApplicationModel) context.ExecutionContext.ServiceProvider
                .GetRequiredService(typeof(DistributedApplicationModel));

            var catalystProject = applicationModel.Resources.FirstOrDefault((resource) => resource is CatalystProjectResource) as CatalystProjectResource
                ?? throw new("This project is missing a Catalyst project resource.");

            var appDetails = await catalystProject.AppDetails[resourceBuilder.Resource].Task;

            context.EnvironmentVariables["DAPR_GRPC_ENDPOINT"] = await catalystProject.GrpcEndpoint.Task;
            context.EnvironmentVariables["DAPR_HTTP_ENDPOINT"] = await catalystProject.HttpEndpoint.Task;
            context.EnvironmentVariables["DAPR_API_TOKEN"] = appDetails.ApiToken;
        });

        return resourceBuilder;
    }

    public static void AddCatalystComponent(
        this IDistributedApplicationBuilder applicationBuilder,
        string name,
        string type,
        IDictionary<string, object> spec,
        IList<string> scopes
    )
    {

    }

    public static void AddCatalystComponent<MetadataType>(
        this IDistributedApplicationBuilder applicationBuilder,
        string name,
        CatalystComponent<MetadataType> component
    )
    {
        var catalystProject = applicationBuilder.EnsureCatalystResource();

        catalystProject.Components.Add(name, component);
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
