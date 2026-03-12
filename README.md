# Catalyst E-Commerce Demo

![Architecture Diagram](/images/architecture.png)

This demo application showcases workflows and local development with Dapr using an e-commerce scenario. The scenario consists of three microservices:

- **Order Management Service** (`order-manager`) - A controller app that orchestrates order processing
- **Inventory Service** (`inventory-service`) - A worker app that manages inventory and processes notifications
- **Notification Service** (`notification-service`) - A web app with a UI dashboard that displays real-time order notifications

Under its default configuration, the applications in this solution do not depend on any external services.

The project features these Dapr building blocks:

 - [Workflow](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview)
 - [Pub/Sub](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview)
 - [Service Invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview)
 - [State management](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview)

## Run, deploy, and release notes

- Local development: use [running.md](./running.md).
- External Kubernetes deployment: use [deploy.md](./deploy.md) (external/manual flow).
- Internal environments use Argo CD to sync this repository (see [Argo/demo-app.yaml](./Argo/demo-app.yaml)).
- Internal reliability testing also uses Chaos Mesh (see [chaos-mesh.md](./chaos-mesh.md)).
- Release updates: bump `.github/workflows/build-push-ecr.yml` (`IMAGE_VERSION`) and update image tags in:
  - `k8s/demo-services/order-manager.yaml`
  - `k8s/demo-services/inventory-service.yaml`
  - `k8s/demo-services/notification-service.yaml`

Argo CD access (internal):

To inspect internal Argo CD state:

```bash
kubectl -n argocd port-forward svc/argocd-server 8084:80
argocd admin initial-password -n argocd
```

Then open `http://localhost:8084` and sign in with user `admin` and the password from the command above.

## Technologies Used

- [Dapr](https://dapr.io/)
- [Diagrid Catalyst](https://www.diagrid.io/catalyst)
- [.NET and Aspire](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)

## Required Environment Variables

These Dapr environment variables are required by the services:

- `APP_ID`
- `APP_PORT`
- `DAPR_API_TOKEN`
- `DAPR_HTTP_ENDPOINT`
- `DAPR_GRPC_ENDPOINT`

For local Aspire runs, these are set automatically. For Catalyst-backed runs/deployments, provide values from your Catalyst project and App IDs.

## Notification UI

The Notification Service provides a real-time web dashboard for viewing order notifications. Access it at `http://localhost:8083` when running the application.

Features:
- **Real-time Updates**: Notifications appear instantly using SignalR
- **Statistics Dashboard**: View counts of total notifications and orders
- **Notification History**: See all past notifications with metadata
- **Create Orders**: Submit new orders through the UI form

## Workflow flow diagram

![Workflow](/images/workflow.png)

### Workflow steps used in the demo

The `OrderProcessingWorkflow` runs this sequence:

1. Send `created` notification.
2. Validate the order.
3. Process payment.
  - If successful, send `payment_processed` notification.
4. Check inventory for requested items.
5. Reserve inventory.
6. Send `shipped` notification.
7. Wait 40 seconds to simulate shipping/delivery time.
8. Send `delivered` notification.
9. Send `completed` notification.

### Failure behavior

- Validation failure sends `failed` and ends workflow.
- Payment failure sends `failed` and ends workflow.
- Inventory update failure sends `failed` and ends workflow.

### Shipping delay

The demo intentionally includes a **40-second shipping delay** between `shipped` and `delivered` using a workflow timer. This is why notifications appear in sequence with a noticeable pause before delivery/completion.

## API and Test References

For endpoint and request examples, use:

- [order-manager.openapi.json](./order-manager.openapi.json)
- [endpoints.http](./endpoints.http)

For full run/deploy instructions, use:

- [running.md](./running.md)
- [deploy.md](./deploy.md)
