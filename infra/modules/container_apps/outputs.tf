output "api_principal_id" {
  value = azurerm_container_app.api.identity[0].principal_id
}

output "worker_principal_id" {
  value = azurerm_container_app.worker.identity[0].principal_id
}

output "api_fqdn" {
  value = azurerm_container_app.api.latest_revision_fqdn
}
