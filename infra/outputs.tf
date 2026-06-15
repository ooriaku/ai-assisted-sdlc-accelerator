output "api_url" {
  description = "Public HTTPS URL of the API Container App."
  value       = "https://${module.container_apps.api_fqdn}"
}

output "key_vault_uri" {
  description = "URI of the Key Vault."
  value       = module.key_vault.key_vault_uri
}

output "acr_login_server" {
  description = "Login server hostname of the Container Registry."
  value       = module.container_registry.login_server
}

output "resource_group_name" {
  description = "Name of the provisioned resource group."
  value       = azurerm_resource_group.main.name
}
