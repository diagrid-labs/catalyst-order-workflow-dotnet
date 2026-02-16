# Catalyst E-Commerce Demo

![Architecture Diagram](/images/architecture.png)

This demo application showcases workflows and local development using Dapr and the Diagrid Dashboard using an e-commerce scenario.  The scenario consists of three microservices:

- **Order Management Service** (`order-manager`) - A controller app that orchestrates order processing
- **Inventory Service** (`inventory-service`) - A worker app that manages inventory and processes notifications
- **Notification Service** (`notification-service`) - A web app with a UI dashboard that displays real-time order notifications

Under its default configuration, the applications in this solution do not depend on any external services.

The project features three Dapr building block APIs:

 - [Workflow](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview)
 - [Pub/Sub](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview)
 - [Service Invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview)

## Run locally 

This project uses .NET Aspire to provide an easy local development experience. Use the [running documentation](./running.md) for a step-by-step guide.

## Deploy to Kubernetes

This deployment targets Catalyst (no Dapr sidecars). You must provide App ID tokens and Catalyst endpoints.

1) Create the namespace:

```bash
kubectl create namespace catalyst-order-workflow-demo
```

2) Create the secret with App ID tokens:

```bash
kubectl create secret generic catalyst-app-tokens \
  -n catalyst-order-workflow-demo \
  --from-literal=inventory-service="$INVENTORY_SERVICE_TOKEN" \
  --from-literal=notification-service="$NOTIFICATION_SERVICE_TOKEN" \
  --from-literal=order-manager="$ORDER_MANAGER_TOKEN"
```

3) Update the Catalyst endpoints in [k8s/catalyst-env.yaml](k8s/catalyst-env.yaml), then apply manifests:

```bash
kubectl apply -f k8s/
```

4) Verify pods are running:

```bash
kubectl get pods -n catalyst-order-workflow-demo
```

## Technologies Used

- [Dapr](https://dapr.io/)
- [Diagrid Catalyst](https://www.diagrid.io/catalyst)
- [ASP.NET](https://dotnet.microsoft.com/apps/aspnet)
    - [Aspire](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
    - [Tracing](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
    - [Health checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)
    - [Secrets management](https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-10.0&tabs=linux)
    - [Minimal APIs](https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio)
    - [Toplevel Statements](https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/top-level-statements)
- [Scalar API browser](https://scalar.com)

Workflow visibility is provided by the [Diagrid Dashboard](https://docs.diagrid.io/develop/diagrid-dashboard), a utility container created to enhance the local Dapr developer experience.

![Diagrid Dashboard](/images/diagrid-dashboard-workflow.png)

An API tester is also included for the Order Manager service which can be accessed by visiting `/scalar` on the Order
Manager service address.

![Scalar](/images/scalar.png)

## Notification UI

The Notification Service provides a real-time web dashboard for viewing order notifications. Access it at `http://localhost:8083` when running the application.

Features:
- **Real-time Updates**: Notifications appear instantly using SignalR
- **Statistics Dashboard**: View counts of total notifications and orders
- **Notification History**: See all past notifications with metadata
- **Create Orders**: Submit new orders through the UI form

## Workflow flow diagram

![Workflow](/images/workflow.png)

1. **Order Processing Workflow**: Complete order lifecycle from validation to fulfillment
2. **Pub/Sub Notifications**: Real-time order status updates (created, payment processed, shipped, delivered)
3. **Service Invocation**: Real-time inventory checks and order updates
4. **State Store**: Persistent inventory management using Dapr state store

## Environment Variables

The services in this project use [a set of well-known environment variables for configuring Dapr](https://docs.dapr.io/reference/environment/).

- `APP_ID` - Name of the application, often in kebab-case
- `APP_PORT` - Port on which the service listens
- `DAPR_API_TOKEN` - Dapr API token
- `DAPR_GRPC_ENDPOINT` - Dapr gRPC endpoint
- `DAPR_HTTP_ENDPOINT` - Dapr HTTP endpoint

### Kubernetes secret for Catalyst tokens

See [Deploy to Kubernetes](README.md#deploy-to-kubernetes) for secret creation steps.

### Order Management Service Endpoints

- `POST /order` - Start an order processing workflow
- `GET /order/{orderId}` - Get the status of a workflow by ID

To try using the Service Invocation API directly vs. through a workflow:

- `POST /inventory/search` - Check inventory for multiple items via service invocation
- `GET /inventory/{productId}` - Show current inventory for a single product via service invocation

### Inventory Service Endpoints

- `POST /inventory/search` - Get current inventory from the state store
- `GET /inventory/{productId}` - Get current inventory for a single product
- `POST /inventory/initialize` - Initialize state store inventory with sample data
- `POST /inventory/update` - Update inventory levels in state store
- `POST /order-notification` - Pubsub subscription handler for order notifications

### Notification Service Endpoints

- `GET /` - Web UI dashboard displaying real-time notifications
- `POST /order-notification` - Pubsub subscription handler for order notifications (displays in UI)
- `POST /order` - Create a new order (via Dapr service invocation to OrderManager)
- `GET /notifications/history` - Get historical notifications
- `/notificationHub` - SignalR hub for real-time notification updates

The Notification Service provides a modern web interface at `http://localhost:8083` where you can view all order notifications in real-time.

When running targeting a local Dapr, these environment variables are automatically set by the Aspire Dapr integration. When
running targeting Catalyst, they are set based on values from Catalyst that you will provide.


### Testing the APIs

With the application running, use the following commands to call the APIs.

#### Initialize Inventory (State Store)

```bash
curl -X POST http://localhost:8081/inventory/initialize
```

### Process an Order (Workflow)

```bash
curl -X POST http://localhost:8080/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cust-001",
    "items": [
      {"productId": "prod-001", "quantity": 2, "price": 29.99},
      {"productId": "prod-002", "quantity": 1, "price": 49.99}
    ]
  }'
```

#### Check Workflow Status

```bash
curl http://localhost:8080/orders/{orderId}/status
```

#### Check Inventory (Service Invocation)

**Check multiple items:**

```bash
curl -X POST http://localhost:8080/orders/check-inventory \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": "prod-001"},
      {"productId": "prod-002"}
    ]
  }'
```

**Check single product:**

```bash
curl http://localhost:8080/orders/check-inventory/prod-001
```

### Testing the Deployed Applications

Once deployed, you'll receive URLs for both services. Use these URLs to test:

#### Initialize Inventory

```bash
curl -X POST https://$INVENTORY_URL/inventory/initialize
```

#### Process an Order

```bash
curl -X POST https://$ORDER_URL/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cust-001",
    "items": [
      {"productId": "prod-001", "quantity": 2, "price": 29.99},
      {"productId": "prod-002", "quantity": 1, "price": 49.99}
    ]
  }'
```

Retrieve the `orderId` from the payload returned and replace it in the curl command below.

#### Check Status of Workflow

```bash
curl https://$ORDER_URL/orders/{orderId}/status
```
