# ── API role assignments ───────────────────────────────────────────────────────

resource "azurerm_role_assignment" "api_kv_secrets_user" {
  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.api_principal_id
}

resource "azurerm_cosmosdb_sql_role_assignment" "api_cosmos" {
  resource_group_name = var.resource_group_name
  account_name        = var.cosmos_account_name
  role_definition_id  = "${var.cosmos_account_id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002"
  principal_id        = var.api_principal_id
  scope               = var.cosmos_account_id
}

resource "azurerm_role_assignment" "api_acr_pull" {
  scope                = var.acr_id
  role_definition_name = "AcrPull"
  principal_id         = var.api_principal_id
}

resource "azurerm_role_assignment" "api_sb_sender" {
  scope                = var.service_bus_namespace_id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = var.api_principal_id
}

resource "azurerm_role_assignment" "api_blob_contributor" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.api_principal_id
}

# ── Worker role assignments ────────────────────────────────────────────────────

resource "azurerm_role_assignment" "worker_sb_receiver" {
  scope                = var.service_bus_namespace_id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = var.worker_principal_id
}

resource "azurerm_role_assignment" "worker_kv_secrets_user" {
  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.worker_principal_id
}

resource "azurerm_role_assignment" "worker_acr_pull" {
  scope                = var.acr_id
  role_definition_name = "AcrPull"
  principal_id         = var.worker_principal_id
}
