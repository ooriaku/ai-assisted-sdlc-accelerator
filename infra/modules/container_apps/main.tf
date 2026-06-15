resource "azurerm_container_app_environment" "main" {
  name                       = "${var.name}-env"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = var.log_analytics_workspace_id
  tags                       = var.tags
}

resource "azurerm_container_app" "api" {
  name                         = "${var.name}-api"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  tags                         = var.tags

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = var.acr_login_server
    identity = "System"
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 5

    container {
      name   = "api"
      image  = "${var.acr_login_server}/aiharness-api:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }
      env {
        name  = "KeyVault__Uri"
        value = var.key_vault_uri
      }
      env {
        name  = "CosmosDb__AccountEndpoint"
        value = var.cosmos_endpoint
      }
      env {
        name  = "ServiceBus__FullyQualifiedNamespace"
        value = var.service_bus_namespace
      }
    }
  }
}

resource "azurerm_container_app" "worker" {
  name                         = "${var.name}-worker"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  tags                         = var.tags

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = var.acr_login_server
    identity = "System"
  }

  template {
    min_replicas = 0
    max_replicas = 3

    container {
      name   = "worker"
      image  = "${var.acr_login_server}/aiharness-worker:${var.image_tag}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "KeyVault__Uri"
        value = var.key_vault_uri
      }
      env {
        name  = "CosmosDb__AccountEndpoint"
        value = var.cosmos_endpoint
      }
      env {
        name  = "ServiceBus__FullyQualifiedNamespace"
        value = var.service_bus_namespace
      }
    }
  }
}
