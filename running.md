## Running this Project

![Running](/running.webm)

This solution runs via a dotnet Aspire AppHost.  Aspire takes care of launching your application and any of its
dependencies under a single run profile.

Running Aspire projects is is familiar and can be done via CLI or within your favourite IDE.

The AppHost project is configured with two run profiles in `launchSettings.json.

- **Local** - `http-local` - Run everything locally on your workstation, using local Dapr instances for all Dapr functionality.
- **Catalyst** - `http-local-catalyst` - Run the services locally on your workstation, connecting to Catalyst for all Dapr functionality.

### Prerequisites

- A compatible [Docker](https://www.docker.com/products/docker-desktop) or [Podman](https://podman.io) installation
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli)
- [.NET SDK](https://dotnet.microsoft.com/en-us)

## Running

1. Install the three prerequisites above
2. Run `dapr init` to initialize Dapr locally
3. Run `docker rm dapr_redis dapr_zipkin --force` -- this cleans up some default containers we'll be replacing

Finally, you can start the project either via your IDE by selecting the `http-local` run profile, or via the CLI:

```bash
cd AppHost
dotnet run
```

---

## Catalyst Profile

An alternative run profile provided as this project allows you to run services locally while also connecting to
live Catalyst resources. This scenario communicates from your laptop to Diagrids Catalyst services, instead of running
the Dapr sidecars locally.

Running with the Diagrid Catalyst profile has the following prerequisites:

- [Diagrid CLI](https://docs.diagrid.io/catalyst/references/cli-reference/intro)
- A diagrid Account, visit [catalyst.diagrid.io](https://catalyst.diagrid.io)
- [.NET SDK](https://dotnet.microsoft.com/en-us)

Before running the project, grab the values for your Catalyst project and apps, and run the following commands in
the `AppHost` project directory, substituting the values as appropriate:

```bash
cd AppHost
dotnet user-secrets set "OrderManagerCatalystApiToken" "YOUR_CATALYST_ORDER_MANAGER_API_TOKEN_HERE"
dotnet user-secrets set "OrderManagerCatalystGrpcEndpoint" "YOUR_CATALYST_ORDER_MANAGER_GRPC_ENDPOINT_HERE"
dotnet user-secrets set "OrderManagerCatalystHttpEndpoint" "YOUR_CATALYST_ORDER_MANAGER_HTTP_ENDPOINT_HERE"
dotnet user-secrets set "InventoryServiceCatalystApiToken" "YOUR_CATALYST_INVENTORY_SERVICE_API_TOKEN_HERE"
dotnet user-secrets set "InventoryServiceCatalystGrpcEndpoint" "YOUR_CATALYST_INVENTORY_SERVICE_GRPC_ENDPOINT_HERE"
dotnet user-secrets set "InventoryServiceCatalystHttpEndpoint" "YOUR_CATALYST_INVENTORY_SERVICE_HTTP_ENDPOINT_HERE"
```

Then, all you need to do is run the project with the `http-local-catalyst` profile either from the CLI or using your IDE.

```bash
cd AppHost
dotnet run --profile http-local-catalyst
```
