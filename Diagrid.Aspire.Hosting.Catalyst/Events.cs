using Microsoft.Extensions.DependencyInjection;

namespace Diagrid.Aspire.Hosting.Catalyst;

internal static class Events
{
    public static Task EnsureCatalystProvisioning(BeforeStartEvent beforeStartEvent, CancellationToken cancellationToken)
    {
        var notifications = beforeStartEvent.Services.GetRequiredService<ResourceNotificationService>();
        var applicationModel = beforeStartEvent.Services.GetRequiredService<DistributedApplicationModel>();
        var provisioner = beforeStartEvent.Services.GetRequiredService<CatalystProvisioner>();

        var catalystProject = applicationModel.Resources.Single((resource) => resource is CatalystProject)
            as CatalystProject ?? throw new("Huh?");
        var projectName = catalystProject.ProjectName;

        // todo: This is going to run after the event completes so that it doesn't hang AppHost start.
        _ = Task.Run(async () => {

            using var runawayCancellationSource = new CancellationTokenSource();

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new(KnownResourceStates.Starting, KnownResourceStateStyles.Info),
            });

            await provisioner.Init(runawayCancellationSource.Token);

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Ensuring project", KnownResourceStateStyles.Info),
            });

            await provisioner.CreateProject(projectName, runawayCancellationSource.Token);

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Selecting project", KnownResourceStateStyles.Info),
            });

            await provisioner.UseProject(projectName, runawayCancellationSource.Token);

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Loading project details", KnownResourceStateStyles.Info),
            });

            var projectDetails = await provisioner.GetProjectDetails(projectName, runawayCancellationSource.Token);

            catalystProject.HttpEndpoint.SetResult(projectDetails.HttpEndpoint.ToString());
            catalystProject.GrpcEndpoint.SetResult(projectDetails.GrpcEndpoint.ToString());

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Ensuring applications", KnownResourceStateStyles.Info),
            });

            foreach (var pair in catalystProject.AppDetails)
            {
                await provisioner.CreateApp(pair.Key.Name, runawayCancellationSource.Token);
                var app = await provisioner.GetAppDetails(pair.Key.Name, runawayCancellationSource.Token);

                pair.Value.SetResult(app);
            }

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Ensuring services", KnownResourceStateStyles.Info),
            });

            foreach (var pair in catalystProject.PubSubs)
            {
                await provisioner.CreatePubSub(pair.Key, new()
                {
                    Project = projectName,
                    Scopes = pair.Value.Scopes,
                }, runawayCancellationSource.Token);
            }

            foreach (var pair in catalystProject.PubSubs)
            {
                await provisioner.CreatePubSub(pair.Key, pair.Value, runawayCancellationSource.Token);
            }

            foreach (var pair in catalystProject.KvStores)
            {
                await provisioner.CreateKvStore(pair.Key, pair.Value, runawayCancellationSource.Token);
            }

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new("Ensuring components", KnownResourceStateStyles.Info),
            });

            foreach (var pair in catalystProject.Components)
            {
                await provisioner.CreateComponent(pair.Value, projectName, runawayCancellationSource.Token);
            }

            await notifications.PublishUpdateAsync(catalystProject, (previous) => previous with
            {
                State = new(KnownResourceStates.Finished, KnownResourceStateStyles.Success),
            });
        });

        return Task.CompletedTask;
    }
}
