variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "log_analytics_workspace_id" {
  type = string
}

variable "acr_login_server" {
  type = string
}

variable "image_tag" {
  type    = string
  default = "latest"
}

variable "cosmos_endpoint" {
  type = string
}

variable "service_bus_namespace" {
  type = string
}

variable "key_vault_uri" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
