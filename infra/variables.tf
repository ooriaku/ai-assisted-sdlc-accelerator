variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "eastus"
}

variable "environment" {
  description = "Deployment environment label (dev | prod)."
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "environment must be 'dev' or 'prod'."
  }
}

variable "prefix" {
  description = "Short name prefix applied to every resource."
  type        = string
  default     = "aiharness"
}

variable "image_tag" {
  description = "Container image tag to deploy (git SHA or 'latest')."
  type        = string
  default     = "latest"
}
