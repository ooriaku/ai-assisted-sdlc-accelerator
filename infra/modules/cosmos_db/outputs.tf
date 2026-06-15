output "account_id" {
  value = azurerm_cosmosdb_account.main.id
}

output "account_name" {
  value = azurerm_cosmosdb_account.main.name
}

output "account_endpoint" {
  value = azurerm_cosmosdb_account.main.endpoint
}
