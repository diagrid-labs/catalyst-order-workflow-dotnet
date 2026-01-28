using System.Diagnostics;
using System.Text.Json;
using Diagrid.Aspire.Hosting.Catalyst.Cli.Options;
using Diagrid.Aspire.Hosting.Catalyst.Cli.Output;
using Diagrid.Aspire.Hosting.Catalyst.Model;

namespace Diagrid.Aspire.Hosting.Catalyst.Cli;

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

        var deployManagedPubsub = options.DeployManagedPubsub ? "true" : "false";
        arguments.AddRange([$"--deploy-managed-pubsub={deployManagedPubsub}"]);

        var deployManagedKv = options.DeployManagedKv ? "true" : "false";
        arguments.AddRange([$"--deploy-managed-kv={deployManagedKv}"]);

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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
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

    public static async Task<CliProjectDetails> GetProjectDetails(string projectName, CancellationToken cancellationToken)
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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
            }

            var projectDetails = JsonSerializer.Deserialize<CliProjectDetails>(output);
            return projectDetails ?? throw new InvalidOperationException("Failed to deserialize project details");
        }

        throw new InvalidOperationException("Failed to start diagrid process");
    }

    public static async Task<CliAppDetails> GetAppDetails(string appId, CancellationToken cancellationToken)
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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
            }

            var appIdentityDetails = JsonSerializer.Deserialize<CliAppDetails>(output);
            return appIdentityDetails ?? throw new InvalidOperationException("Failed to deserialize app identity details");
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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start diagrid process");
        }
    }

    public static async Task CreateComponent(
        ComponentDescriptor descriptor,
        CreateComponentOptions options,
        CancellationToken cancellationToken = default
    )
    {
        var arguments = new List<string>
        {
            "component",
            "create",
        };

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            throw new ArgumentException("Component name required", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Type))
        {
            throw new ArgumentException("Component type required", nameof(descriptor));
        }

        arguments.Add(descriptor.Name);
        arguments.Add("--type");
        arguments.Add(descriptor.Type);

        var hasMetadata = false;

        foreach (var pair in descriptor.Metadata)
        {
            if (pair.Value is null) continue;

            hasMetadata = true;
            arguments.Add("--metadata");
            arguments.Add($"{pair.Key}={pair.Value}");
        }

        if (! hasMetadata)
        {
            throw new ArgumentException("Component metadata required", nameof(descriptor));
        }

        if (descriptor.Scopes.Count > 0)
        {
            arguments.Add("--scopes");
            arguments.Add(string.Join(",", descriptor.Scopes));
        }

        if (!string.IsNullOrWhiteSpace(options.Project))
        {
            arguments.Add("--project");
            arguments.Add(options.Project);
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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start diagrid process");
        }
    }

    public static async Task CreatePubSub(
        string pubsubName,
        CreatePubSubOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(pubsubName))
        {
            throw new ArgumentException("Pub/Sub broker name required", nameof(pubsubName));
        }

        var arguments = new List<string>
        {
            "pubsub",
            "create",
            pubsubName,
        };

        if (!string.IsNullOrWhiteSpace(options.Project))
        {
            arguments.Add("--project");
            arguments.Add(options.Project);
        }

        if (options.Scopes.Count > 0)
        {
            arguments.Add("--scopes");
            arguments.Add(string.Join(",", options.Scopes));
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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start diagrid process");
        }
    }

    public static async Task CreateKvStore(
        string kvStoreName,
        CreateKvStoreOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(kvStoreName))
        {
            throw new ArgumentException("KV Store name required", nameof(kvStoreName));
        }

        var arguments = new List<string>
        {
            "kv",
            "create",
            kvStoreName,
        };

        if (!string.IsNullOrWhiteSpace(options.Project))
        {
            arguments.Add("--project");
            arguments.Add(options.Project);
        }

        if (options.Scopes.Count > 0)
        {
            arguments.Add("--scopes");
            arguments.Add(string.Join(",", options.Scopes));
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

            if (process.ExitCode != 0 && ! output.Contains("already exists"))
            {
                throw new InvalidOperationException(output);
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start diagrid process");
        }
    }

    public static async Task<bool> CheckKvStoreExists(
        string kvStoreName,
        string projectName,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(kvStoreName))
        {
            throw new ArgumentException("KV Store name required", nameof(kvStoreName));
        }

        var arguments = new List<string>
        {
            "kv",
            "list",
            "--output",
            "json",
        };

        arguments.Add("--project");
        arguments.Add(projectName);

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
                throw new InvalidOperationException(output);
            }

            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            return root.GetProperty("items")
                .EnumerateArray()
                .Any((kv) => kv
                    .GetProperty("metadata")
                    .GetProperty("name")
                    .GetString() == kvStoreName
            );
        }

        throw new InvalidOperationException("Failed to start diagrid process");
    }
}
