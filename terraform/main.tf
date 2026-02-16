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
}

resource "catalyst_appid" "notification_service" {
  project_id = catalyst_project.order_workflow.name
  name       = "notification-service"
}

resource "catalyst_appid" "order_manager" {
  project_id = catalyst_project.order_workflow.name
  name       = "order-manager"
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
