using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Diagrid.Aspire.Hosting.Catalyst;

internal static class Commands
{
    public static async Task UseCatalyst(CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "diagrid",
            Arguments = string.Join(" ",
            [
                "product",
                "use",
                "catalyst",
            ]),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);

        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        }
    }

    public static async Task CreateProject(
        string projectName,
        CreateProjectOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var arguments = new List<string>
        {
            "project",
            "create",
            projectName,
        };

        options ??= new();

        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            arguments.Add("--region");
            arguments.Add(options.Region);
        }

        if (options.DeployManagedPubsub)
        {
            arguments.Add("--deploy-managed-pubsub");
        }

        if (options.DeployManagedKv)
        {
            arguments.Add("--deploy-managed-kv");
        }

        if (options.EnableManagedWorkflow)
        {
            arguments.Add("--enable-managed-workflow");
        }

        if (options.Wait)
        {
            arguments.Add("--wait");
        }

        if (options.Use)
        {
            arguments.Add("--use");
        }

        if (options.DisableAppTunnels)
        {
            arguments.Add("--disable-app-tunnels");
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "diagrid",
            Arguments = string.Join(" ", arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);

        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to create project: {error}");
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start diagrid process");
        }
    }

    public static async Task UseProject(string projectName, CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "diagrid",
            Arguments = string.Join(" ",
            [
                "project",
                "use",
                projectName,
            ]),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);

        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        }
    }

    public static async Task<ProjectDetails> GetProjectDetails(string projectName, CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "diagrid",
            Arguments = string.Join(" ",
            [
                "project",
                "get",
                projectName,
                "--output",
                "json",
            ]),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);

        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            if (process.ExitCode == 0 && ! string.IsNullOrWhiteSpace(output))
            {
                var projectDetails = JsonSerializer.Deserialize<ProjectDetails>(output);
                return projectDetails ?? throw new InvalidOperationException("Failed to deserialize project details");
            }

            throw new InvalidOperationException($"Failed to get project details: {error}");
        }

        throw new InvalidOperationException("Failed to start diagrid process");
    }

    public static async Task<AppDetails> GetAppDetails(string appId, CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "diagrid",
            Arguments = string.Join(" ",
            [
                "appid",
                "get",
                appId,
                "--output",
                "json",
            ]),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);

        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var appIdentityDetails = JsonSerializer.Deserialize<AppDetails>(output);
                return appIdentityDetails ?? throw new InvalidOperationException("Failed to deserialize app identity details");
            }

            throw new InvalidOperationException($"Failed to get app identity details: {error}");
        }

        throw new InvalidOperationException("Failed to start diagrid process");
    }

    public static async Task CreateApp(
        string appId,
        CreateAppOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var arguments = new List<string>
        {
            "appid",
            "create",
            appId,
        };

        options ??= new();

        if (!string.IsNullOrWhiteSpace(options.Project))
        {
            arguments.Add("--project");
            arguments.Add(options.Project);
        }

        if (!string.IsNullOrWhiteSpace(options.AppEndpoint))
        {
            arguments.Add("--app-endpoint");
            arguments.Add(options.AppEndpoint);
        }

        if (!string.IsNullOrWhiteSpace(options.AppToken))
        {
            arguments.Add("--app-token");
            arguments.Add(options.AppToken);
        }

        if (!string.IsNullOrWhiteSpace(options.AppProtocol))
        {
            arguments.Add("--app-protocol");
            arguments.Add(options.AppProtocol);
        }

        if (!string.IsNullOrWhiteSpace(options.AppConfig))
        {
            arguments.Add("--app-config");
            arguments.Add(options.AppConfig);
        }

        if (options.EnableAppHealthCheck)
        {
            arguments.Add("--enable-app-health-check");
        }

        if (!string.IsNullOrWhiteSpace(options.AppHealthCheckPath))
        {
            arguments.Add("--app-health-check-path");
            arguments.Add(options.AppHealthCheckPath);
        }

        if (options.AppHealthProbeInterval.HasValue)
        {
            arguments.Add("--app-health-probe-interval");
            arguments.Add(options.AppHealthProbeInterval.Value.ToString());
        }

        if (options.AppHealthProbeTimeout.HasValue)
        {
            arguments.Add("--app-health-probe-timeout");
            arguments.Add(options.AppHealthProbeTimeout.Value.ToString());
        }

        if (options.AppHealthThreshold.HasValue)
        {
            arguments.Add("--app-health-threshold");
            arguments.Add(options.AppHealthThreshold.Value.ToString());
        }

        if (options.AppChannelTimeoutSeconds.HasValue)
        {
            arguments.Add("--app-channel-timeout-seconds");
            arguments.Add(options.AppChannelTimeoutSeconds.Value.ToString());
        }

        if (options.Wait)
        {
            arguments.Add("--wait");
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "diagrid",
            Arguments = string.Join(" ", arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);

        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to create app ID: {error}");
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start diagrid process");
        }
    }
}

public record ProjectDetails
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("metadata")]
    public ProjectMetadata Metadata { get; init; } = new();

    [JsonPropertyName("spec")]
    public ProjectSpec Spec { get; init; } = new();

    [JsonPropertyName("status")]
    public ProjectStatus Status { get; init; } = new();
}

public record ProjectMetadata
{
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("resourceVersion")]
    public int ResourceVersion { get; init; }

    [JsonPropertyName("uid")]
    public string Uid { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; init; } = string.Empty;
}

public record ProjectSpec
{
    [JsonPropertyName("defaultWorkflowStoreEnabled")]
    public bool DefaultWorkflowStoreEnabled { get; init; }

    [JsonPropertyName("disableAppTunnels")]
    public bool DisableAppTunnels { get; init; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("privateRegion")]
    public bool PrivateRegion { get; init; }

    [JsonPropertyName("region")]
    public string Region { get; init; } = string.Empty;
}

public record ProjectStatus
{
    [JsonPropertyName("endpoints")]
    public ProjectEndpoints Endpoints { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; init; } = string.Empty;
}

public record ProjectEndpoints
{
    [JsonPropertyName("grpc")]
    public required ProjectEndpointDetails Grpc { get; init; }

    [JsonPropertyName("http")]
    public required ProjectEndpointDetails Http { get; init; }
}

public record ProjectEndpointDetails
{
    [JsonPropertyName("port")]
    public int Port { get; init; }

    [JsonPropertyName("url")]
    public Uri Uri { get; init; }
}

public record CreateProjectOptions
{
    public string? Region { get; init; }
    public bool DeployManagedPubsub { get; init; }
    public bool DeployManagedKv { get; init; }
    public bool EnableManagedWorkflow { get; init; }

    public bool Wait { get; init; } = true;
    public bool Use { get; init; }
    public bool DisableAppTunnels { get; init; }
}

public record CreateAppOptions
{
    public string? Project { get; init; }
    public string? AppEndpoint { get; init; }
    public string? AppToken { get; init; }
    public string? AppProtocol { get; init; }
    public string? AppConfig { get; init; }
    public bool EnableAppHealthCheck { get; init; }
    public string? AppHealthCheckPath { get; init; }
    public int? AppHealthProbeInterval { get; init; }
    public int? AppHealthProbeTimeout { get; init; }
    public int? AppHealthThreshold { get; init; }
    public int? AppChannelTimeoutSeconds { get; init; }

    public bool Wait { get; init; } = true;
}

public record AppDetails
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("metadata")]
    public AppIdentityMetadata Metadata { get; init; } = new();

    [JsonPropertyName("spec")]
    public AppIdentitySpec Spec { get; init; } = new();

    [JsonPropertyName("status")]
    public AppIdentityStatus Status { get; init; } = new();
}

public record AppIdentityMetadata
{
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("resourceVersion")]
    public int ResourceVersion { get; init; }

    [JsonPropertyName("uid")]
    public string Uid { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; init; } = string.Empty;
}

public record AppIdentitySpec
{
    [JsonPropertyName("apiTokenRevision")]
    public int ApiTokenRevision { get; init; }

    [JsonPropertyName("healthCheck")]
    public HealthCheck HealthCheck { get; init; } = new();

    [JsonPropertyName("projectId")]
    public string ProjectId { get; init; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; init; } = string.Empty;
}

public record HealthCheck
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("probe")]
    public HealthCheckProbe Probe { get; init; } = new();
}

public record HealthCheckProbe
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("failureThreshold")]
    public int FailureThreshold { get; init; }

    [JsonPropertyName("intervalInSec")]
    public int IntervalInSec { get; init; }

    [JsonPropertyName("timeoutInMs")]
    public int TimeoutInMs { get; init; }
}

public record AppIdentityStatus
{
    [JsonPropertyName("apiToken")]
    public string ApiToken { get; init; } = string.Empty;

    [JsonPropertyName("spiffeId")]
    public string SpiffeId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; init; } = string.Empty;
}
