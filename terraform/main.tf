terraform {
  required_providers {
    catalyst = {
      source = "diagridio/catalyst"
      version = "0.0.10"
    }
  }
}

provider "catalyst" {
}

resource "catalyst_project" "order_workflow" {
  name = "order-workflow"
}

resource "catalyst_appid" "inventory_service" {
  project_id = catalyst_project.order_workflow.name
  name       = "inventory-service"
  protocol   = "http"

  app_endpoint = {
    url                    = "http://demo-production-catalyst.westeurope.cloudapp.azure.com/inventory-service"
    client_timeout_seconds = 30
  }

  health_check = {
    enabled           = true
    path              = "/healthz"
    interval_seconds  = 5
    timeout_ms        = 500
    failure_threshold = 3
  }
}

resource "catalyst_appid" "notification_service" {
  project_id = catalyst_project.order_workflow.name
  name       = "notification-service"
  protocol   = "http"

  app_endpoint = {
    url                    = "http://demo-production-catalyst.westeurope.cloudapp.azure.com/notification-service"
    client_timeout_seconds = 30
  }

  health_check = {
    enabled           = true
    path              = "/healthz"
    interval_seconds  = 5
    timeout_ms        = 500
    failure_threshold = 3
  }
}

resource "catalyst_appid" "order_manager" {
  project_id = catalyst_project.order_workflow.name
  name       = "order-manager"
  protocol   = "http"

  app_endpoint = {
    url                    = "http://demo-production-catalyst.westeurope.cloudapp.azure.com/order-manager"
    client_timeout_seconds = 30
  }

  health_check = {
    enabled           = true
    path              = "/healthz"
    interval_seconds  = 5
    timeout_ms        = 500
    failure_threshold = 3
  }
}

resource "catalyst_pubsub" "shop_activity" {
  project_name   = catalyst_project.order_workflow.name
  name           = "shop-activity"
  component_name = "shop-activity"
  create_component = true
}

resource "catalyst_subscription" "order_notifications" {
  project_name = catalyst_project.order_workflow.name
  name         = "order-notifications"
  pubsub_name  = catalyst_pubsub.shop_activity.name
  topic        = "orders"
  scopes       = [catalyst_appid.inventory_service.name]
  spec = {
    routes = {
      default = "/order-notification"
    }
    bulk_subscribe = {
      enabled = false
    }
  }
}

resource "catalyst_kvstore" "inventory_store" {
  project_name   = catalyst_project.order_workflow.name
  name           = "inventory-store"
  component_name = "inventory-store"
  create_component = true
}
