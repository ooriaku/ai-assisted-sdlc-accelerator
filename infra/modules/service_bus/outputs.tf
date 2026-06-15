output "namespace_id" {
  value = azurerm_servicebus_namespace.main.id
}

output "fully_qualified_namespace" {
  value = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
}
