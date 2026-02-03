## Prerequisites

- A compatible [Docker](https://www.docker.com/products/docker-desktop) or [Podman](https://podman.io) installation
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli) and Dapr initialized locally
- [.NET SDK](https://dotnet.microsoft.com/en-us)

## Run with Aspire

![Running](/images/running.webp)

This solution runs via a dotnet Aspire AppHost.  Aspire takes care of launching your application and any of its dependencies under a single run profile.

Running Aspire projects is familiar and can be done via CLI or within your favourite IDE. The AppHost project is configured with two run profiles in `launchSettings.json`.
  - **Local** - `http-local` - Run everything on your workstation, using local Dapr instances for all Dapr functionality.
  - **Catalyst** - `http-local-catalyst` - Run the services locally on your workstation, connecting to Catalyst for all Dapr functionality.

1. Install the three prerequisites above
2. Run `dapr init` to initialize Dapr locally
3. Run `docker rm dapr_redis dapr_zipkin --force` -- this cleans up some default containers we'll be replacing

Finally, you can start the project either via your IDE by selecting the `http-local` run profile, or via the CLI:

```bash
cd AppHost
dotnet run
```

---

## Run with Aspire & Catalyst

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

---

## Run with the Dapr CLI

You can also run the services locally using the Dapr CLI.

1. Open three terminals and build the apps.

```bash
# Terminal one
cd InventoryService
dotnet build

# Terminal two
cd OrderManager
dotnet build

# Terminal three
cd NotificationService
dotnet build  
```

2. Run the inventory service:

```bash
dapr run --app-id inventory-service --app-port 8081 --dapr-http-port 6002 --resources-path ../components -- dotnet run --project InventoryService.csproj
```

3. Run the order manager workflow:

```bash
dapr run --app-id order-manager --app-port 8080 --dapr-http-port 6003 --resources-path ../components -- dotnet run --project OrderManager.csproj
```

4. Run the notification service with web UI:

```bash
dapr run --app-id notification-service --app-port 8083 --dapr-http-port 6004 --resources-path ../components -- dotnet run --project NotificationService.csproj
```

5. Open the Notification Dashboard in your browser at `http://localhost:8083` to see real-time notifications

6. Start a new order workflow using the VSCode Extension REST Client Extension using the [endpoints.http](./endpoints.http) file or curl commands

---

## Run with the Diagrid CLI and Catalyst

1. Open three terminals and build the apps.

```bash
# Terminal one
cd InventoryService
dotnet build

# Terminal two
cd OrderManager
dotnet build

# Terminal three
cd NotificationService
dotnet build  
```

2. Login to your Diagrid Catalyst account. Ensure you are logged into the correct organization and project.

```bash
diagrid login
```

3. Use the Diagrid CLI to launch the project on Catalyst

```bash
diagrid dev run --file dapr.yaml --project order-manager-workflow 
```

4. Import the [subscription.yaml](components/catalyst/subsccription.yaml) file into Catalyst subscriptions: https://catalyst.r1.diagrid.io/subscriptions.

5. Navigate to the Catalyst UI and kick off an order in the UI [http://localhost:8083/](http://localhost:8083/) to see the workflow running.

6. Run the following to stop the dev tunnels:

```bash
diagrid dev stop -f dapr.yaml  
```