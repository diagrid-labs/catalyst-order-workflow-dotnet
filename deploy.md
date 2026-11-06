# Deployment Guide

This guide explains how to deploy the Diagrid Order Workflow demo application to Kubernetes using Diagrid.

## Prerequisites

- [Terraform](https://developer.hashicorp.com/terraform/downloads)
- [Kubectl](https://kubernetes.io/docs/tasks/tools/)
- A Diagrid account and API Key
- A Kubernetes cluster (e.g., AKS, EKS, GKE, or local like Kind/Minikube with Ingress)

## Cluster Entrypoint (Ingress)

For this demo, we exposed our services using a public Ingress to allow Diagrid to reach them for callbacks and subscriptions. You will need to configure your own public endpoints and update the Terraform configuration accordingly.

## Part 1: Provisioning Diagrid Resources with Terraform

The `terraform/` directory contains the configuration to provision the necessary Diagrid resources:
- Project: `order-workflow`
- App IDs: `inventory-service`, `notification-service`, `order-manager`
- Components: `shop-activity` (Pub/Sub), `inventory-store` (KV Store)
- Subscription: `order-notifications`

### Step 1: Update Application Endpoints

Before running Terraform, open `terraform/main.tf` and replace the placeholder URLs in the `app_endpoint` blocks with your public endpoints (e.g., `https://inventory.your-domain.com`).

### Step 2: Obtain Diagrid API Key

You will need a Diagrid API Key to authenticate with the Diagrid provider. You can generate one in your Diagrid organization settings.

### Step 3: Apply Configuration

1. Navigate to the terraform directory:
   ```bash
   cd terraform
   ```

2. Initialize Terraform:
   ```bash
   terraform init
   ```

3. Apply the configuration:
   ```bash
   export CATALYST_API_KEY==<your-api-key>
   terraform apply
   ```

## Part 2: Deploying to Kubernetes

The `k8s/` directory contains the Kubernetes manifests for the application services.

### Step 1: Create Namespace

```bash
kubectl create namespace catalyst-order-workflow-demo
```

### Step 2: Configure Diagrid Project Endpoints

Your application needs to connect to the Diagrid managed runtime to perform Dapr operations (like saving state or publishing events). These endpoints provide that connection address.

1. Go to your **Project Overview** in the Diagrid Console.
2. Locate the **Dapr Endpoints** section.
3. Copy the **HTTP Endpoint** and **gRPC Endpoint** URLs.
4. Open `k8s/demo-services/catalyst-env.yaml` and replace the `DAPR_HTTP_ENDPOINT` and `DAPR_GRPC_ENDPOINT` values with your project's specific endpoints.

### Step 3: Configure App ID Tokens

Each application service needs a unique token to authenticate its requests to Diagrid.

1. In the Diagrid Console, navigate to the **App IDs** tab.
2. Click on the `inventory-service` App ID to view its details.
3. Copy the **API Token**.
4. Repeat this for `notification-service` and `order-manager`.
5. Create the Kubernetes secret using these tokens:

```bash
kubectl create secret generic catalyst-app-tokens \
  -n catalyst-order-workflow-demo \
  --from-literal=inventory-service="<INVENTORY_SERVICE_TOKEN>" \
  --from-literal=notification-service="<NOTIFICATION_SERVICE_TOKEN>" \
  --from-literal=order-manager="<ORDER_MANAGER_TOKEN>"
```

### Step 4: Apply Manifests

```bash
kubectl apply -f k8s/demo-services/
```

## Part 3: Security Recommendations

Since your application endpoints are public, it is critical to secure the connection. Diagrid supports authentication via an Application Token.

- **Configure Application Tokens**: Ensure that your application verifies the token sent by Diagrid in the `dapr-api-token` header.
- **TLS**: Always use HTTPS for your Ingress endpoints.

For detailed security recommendations and configuration steps, please refer to the official documentation:
[Securing the Application Connection](https://docs.diagrid.io/develop/connect/#securing-the-application-connection)
