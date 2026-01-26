using Microsoft.Extensions.DependencyInjection;

namespace Diagrid.Aspire.Hosting.Catalyst;

internal static class Events
{
    public static async Task EnsureCatalystProvisioning(BeforeStartEvent beforeStartEvent, CancellationToken cancellationToken)
    {
        var notifications = beforeStartEvent.Services.GetRequiredService<ResourceNotificationService>();
        var applicationModel = beforeStartEvent.Services.GetRequiredService<DistributedApplicationModel>();

        var catalystProject = applicationModel.Resources.Single((resource) => resource is CatalystProjectResource)
            as CatalystProjectResource ?? throw new("Huh?");
        var projectName = catalystProject.ProjectName;

        // todo: This is going to run after the event completes so that it doesn't hang AppHost start.
        _ = Task.Run(async () => {

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Starting", KnownResourceStateStyles.Info),
            });

            // todo: Need to not have to do this.
            await Commands.UseCatalyst(cancellationToken);

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Ensuring project", KnownResourceStateStyles.Info),
            });

            try
            {
                await Commands.CreateProject(projectName, cancellationToken: cancellationToken);
            }
            catch
            {
                // todo: Would like a silent/idempotent create.
            }

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Selecting project", KnownResourceStateStyles.Info),
            });

            await Commands.UseProject(projectName, cancellationToken);

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Loading project details", KnownResourceStateStyles.Info),
            });

            var projectDetails = await Commands.GetProjectDetails(projectName, cancellationToken);

            catalystProject.HttpEndpoint.SetResult(projectDetails.Status.Endpoints.Http.Uri.ToString());
            catalystProject.GrpcEndpoint.SetResult(projectDetails.Status.Endpoints.Grpc.Uri.ToString());

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Creating applications", KnownResourceStateStyles.Info),
            });

            foreach (var pair in catalystProject.AppDetails)
            {
                try
                {
                    await Commands.CreateApp(pair.Key.Name, cancellationToken: cancellationToken);
                }
                catch
                {
                    // todo: Would like a silent/idempotent create.
                }

                pair.Value.SetResult(await Commands.GetAppDetails(pair.Key.Name, cancellationToken));
            }

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new(KnownResourceStates.Finished, KnownResourceStateStyles.Success),
            });
        });
    }
}
