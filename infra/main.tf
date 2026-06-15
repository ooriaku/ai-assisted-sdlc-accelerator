locals {
  name                 = "${var.prefix}-${var.environment}"
  acr_name             = "${replace(var.prefix, "-", "")}acr${var.environment}"
  storage_account_name = "${replace("${var.prefix}${var.environment}", "-", "")}stor"

  tags = {
    environment = var.environment
    project     = "ai-sdlc"
    managed_by  = "terraform"
  }
}

resource "azurerm_resource_group" "main" {
  name     = "${local.name}-rg"
  location = var.location
  tags     = local.tags
}

# ── Log Analytics ──────────────────────────────────────────────────────────────
module "log_analytics" {
  source              = "./modules/log_analytics"
  name                = "${local.name}-logs"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

# ── Key Vault ──────────────────────────────────────────────────────────────────
module "key_vault" {
  source              = "./modules/key_vault"
  key_vault_name      = "${local.name}-kv"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

# ── Cosmos DB ──────────────────────────────────────────────────────────────────
module "cosmos_db" {
  source              = "./modules/cosmos_db"
  account_name        = "${local.name}-cosmos"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

# ── Service Bus ────────────────────────────────────────────────────────────────
module "service_bus" {
  source              = "./modules/service_bus"
  namespace_name      = "${local.name}-sb"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

# ── Container Registry ─────────────────────────────────────────────────────────
module "container_registry" {
  source              = "./modules/container_registry"
  registry_name       = local.acr_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

# ── Storage Account ────────────────────────────────────────────────────────────
module "storage" {
  source              = "./modules/storage"
  account_name        = local.storage_account_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

# ── Container Apps ─────────────────────────────────────────────────────────────
module "container_apps" {
  source                     = "./modules/container_apps"
  name                       = local.name
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = module.log_analytics.workspace_id
  acr_login_server           = module.container_registry.login_server
  image_tag                  = var.image_tag
  cosmos_endpoint            = module.cosmos_db.account_endpoint
  service_bus_namespace      = module.service_bus.fully_qualified_namespace
  key_vault_uri              = module.key_vault.key_vault_uri
  tags                       = local.tags
}

# ── Role Assignments ───────────────────────────────────────────────────────────
# depends_on ensures managed identities exist before role assignments are applied
module "role_assignments" {
  source                    = "./modules/role_assignments"
  resource_group_name       = azurerm_resource_group.main.name
  key_vault_id              = module.key_vault.key_vault_id
  cosmos_account_id         = module.cosmos_db.account_id
  cosmos_account_name       = module.cosmos_db.account_name
  service_bus_namespace_id  = module.service_bus.namespace_id
  acr_id                    = module.container_registry.registry_id
  storage_account_id        = module.storage.storage_account_id
  api_principal_id          = module.container_apps.api_principal_id
  worker_principal_id       = module.container_apps.worker_principal_id

  depends_on = [module.container_apps]
}
