# Catalyst E-Commerce Demo

![Architecture Diagram](/images/architecture.png)

This demo application showcases workflows and local development using Dapr and the Diagrid Dashboard using a fictional
e-commerce scenario.  The scenario consists of two microservices:

- **Order Management Service** (`order-management`) - A controller app that orchestrates order processing
- **Inventory Service** (`inventory-service`) - A worker app that manages inventory and processes notifications

Under its default configuration, the applications in this solution do not depend on any external services.

The project features three main Dapr building blocks:

 - [Workflows](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview)
 - [Pub/Sub](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview)
 - [Service Invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview)

### Technologies Used

- [Dapr](https://dapr.io/)
- [Diagrid Dashboard](https://diagrid.io)
- [Diagrid Catalyst](https://www.diagrid.io/catalyst)
- [ASP.NET](https://dotnet.microsoft.com/apps/aspnet)
    - [Aspire](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
    - [Tracing](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
    - [Health checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)
    - [Secrets management](https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-10.0&tabs=linux)
    - [Minimal APIs](https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio)
    - [Toplevel Statements](https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/top-level-statements)
- [Scalar API browser](https://scalar.com)

Workflow visibility is provided by the Diagrid Dashboard, a utility container created to enhance the Dapr developer
experience.

![Diagrid Dashboard](/images/diagrid-dashboard-workflow.png)

An API tester is also included for the Order Manager service which can be accessed by visiting `/scalar` on the Order
Manager service address.

![Scalar](/images/scalar.png)

## Running

This project uses .NET Aspire to provide an easy local development experience.

Take a look at the [running documentation](/images/running.md) for a complete guide.

## Deploying

> WIP

## Business Flow

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

### Order Management Service Endpoints

- `POST /order` - Start an order processing workflow
- `GET /order/{orderId}` - Get the status of a workflow by ID

To try using the Pub/Sub and Service Invocation APIs directly vs. through a workflow:

- `POST /promotion` - Send a promotional notification (simple pub/sub example)
- `POST /inventory/search` - Check inventory for multiple items via service invocation
- `GET /inventory/{productId}` - Show current inventory for a single product via service invocation

### Inventory Service Endpoints

- `POST /inventory/search` - Get current inventory from the state store
- `GET /inventory/{productId}` - Get current inventory for a single product
- `POST /inventory/initialize` - Initialize state store inventory with sample data
- `POST /inventory/update` - Update inventory levels in state store
- `POST /order-notification` - Pubsub subscription handler for order notifications
- `POST /promotion` - Pubsub subscription handler for promotion notifications

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

#### Send Promotion (Pub/Sub)

```bash
curl -X POST http://localhost:8080/promotions/send \
  -H "Content-Type: application/json" \
  -d '{
    "promotionType": "flash_sale",
    "message": "50% off all items for the next 2 hours!",
    "targetAudience": "all_customers"
  }'
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
