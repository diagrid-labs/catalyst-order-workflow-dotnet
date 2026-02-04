# Notification Service

Real-time notification dashboard for the Catalyst Order Workflow application. Displays order status updates using SignalR WebSockets and Dapr Pub/Sub.

## Quick Start

### Run with Aspire (Recommended)

```bash
cd AppHost
dotnet run
```

Open the dashboard at http://localhost:8083

### Run Standalone with Dapr

```bash
cd NotificationService
dotnet build
dapr run --app-id notification-service --app-port 8083 --dapr-http-port 6004 --resources-path ../components -- dotnet run
```

## Features

- Real-time notifications via SignalR
- Dapr pub/sub integration (orders topic)
- Live statistics dashboard
- Notification history
- Create new orders via UI form

## API Endpoints

- `GET /` - Web dashboard
- `GET /notifications/history` - Get all notifications
- `POST /order` - Create a new order (via Dapr service invocation to OrderManager)
- `GET /healthz` - Health check
- `GET /scalar` - API documentation
- `/notificationHub` - SignalR hub (WebSocket)

## Testing

### Use the Create Order Form
1. Open http://localhost:8083
2. Click "Create New Order"
3. Enter customer ID and order items
4. Watch notifications appear in real-time as the workflow progresses

### Trigger from Order Manager

```bash
curl -X POST http://localhost:8081/order \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": null,
    "customerId": "alice",
    "items": [{"productId": "prod-001", "quantity": 2, "price": 29.99}]
  }'
```

## Troubleshooting

- **Dashboard not loading?** Check service is running: `curl http://localhost:8083/healthz`
- **No notifications?** Verify Dapr sidecar is running and shop-activity pub/sub component is configured
- **SignalR disconnected?** Check browser console (F12) for connection errors

## Tech Stack

- ASP.NET Core 9.0
- SignalR (WebSockets)
- Dapr Pub/Sub
- Vanilla JavaScript
