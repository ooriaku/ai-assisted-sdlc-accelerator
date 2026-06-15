resource "azurerm_servicebus_namespace" "main" {
  name                = var.namespace_name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_servicebus_queue" "agent_tasks" {
  name         = "agent-tasks"
  namespace_id = azurerm_servicebus_namespace.main.id

  max_delivery_count  = 5
  lock_duration       = "PT10M"
  default_message_ttl = "P1D"
}

resource "azurerm_servicebus_queue" "agent_results" {
  name         = "agent-results"
  namespace_id = azurerm_servicebus_namespace.main.id

  max_delivery_count  = 5
  lock_duration       = "PT5M"
  default_message_ttl = "P1D"
}
