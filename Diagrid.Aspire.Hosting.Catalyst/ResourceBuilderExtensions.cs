using System.Text.Json;
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
        var catalystProject = applicationBuilder.AddResource(new CatalystProject
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
                        "dev", "run",
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

            var catalystProject = applicationModel.Resources.FirstOrDefault((resource) => resource is CatalystProject) as CatalystProject
                ?? throw new("This project is missing a Catalyst project resource.");

            var appDetails = await catalystProject.AppDetails[resourceBuilder.Resource].Task;

            context.EnvironmentVariables["DAPR_GRPC_ENDPOINT"] = await catalystProject.GrpcEndpoint.Task;
            context.EnvironmentVariables["DAPR_HTTP_ENDPOINT"] = await catalystProject.HttpEndpoint.Task;
            context.EnvironmentVariables["DAPR_API_TOKEN"] = appDetails.ApiToken;
        });

        return resourceBuilder;
    }

    /// <summary>
    ///     Adds a weakly-typed component to the Catalyst project.
    /// </summary>
    /// <param name="applicationBuilder"></param>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="metadata"></param>
    /// <param name="scopes"></param>
    public static void AddCatalystComponent(
        this IDistributedApplicationBuilder applicationBuilder,
        string name,
        string type,
        IDictionary<string, object> metadata,
        IList<string> scopes
    )
    {
        var catalystProject = applicationBuilder.EnsureCatalystResource();

        catalystProject.Components.Add(name, new()
        {
            Name = name,
            Type = type,
            Scopes = scopes,
            Metadata = metadata,
        });
    }

    /// <summary>
    ///     Adds a strongly-typed component to the Catalyst project.
    /// </summary>
    /// <param name="applicationBuilder"></param>
    /// <param name="name"></param>
    /// <param name="component"></param>
    /// <typeparam name="MetadataType"></typeparam>
    /// <exception cref="Exception"></exception>
    public static void AddCatalystComponent<MetadataType>(
        this IDistributedApplicationBuilder applicationBuilder,
        string name,
        CatalystComponent<MetadataType> component
    )
    {
        var catalystProject = applicationBuilder.EnsureCatalystResource();

        var metadataSerializerOptions = new JsonSerializerOptions
        {
        };

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(component.Metadata, metadataSerializerOptions))
            ?? throw new("Failed to prepare component metadata.");

        catalystProject.Components.Add(name, new()
        {
            Name = name,
            Type = component.Type,
            Scopes = component.Scopes,
            Metadata = metadata,
        });
    }

    /// <summary>
    ///     Ensures the user has configured a Catalyst project as part of their orchestration.
    /// </summary>
    /// <param name="applicationBuilder"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static CatalystProject EnsureCatalystResource(this IDistributedApplicationBuilder applicationBuilder)
    {
        if (applicationBuilder.Resources.SingleOrDefault((resource) => resource is CatalystProject) is not CatalystProject catalystProject)
            throw new($"Remember to configure your Catalyst project by calling {nameof(AddCatalystProject)}.");

        return catalystProject;
    }
}
