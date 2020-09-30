# Variables
variable "access_key" {
  default = "ACCESS_KEY_HERE"
}
variable "secret_key" {
  default = "SECRET_KEY_HERE"
}
variable "region" {
  default = "us-west-2"
}
variable "cidr_vpc" {
  description = "CIDR block for the VPC"
  default     = "10.0.0.0/16"
}
variable "cidr_subnet" {
  description = "CIDR block for the subnet"
  default     = "10.0.3.0/24"
}
variable "availability_zone" {
  description = "availability zone to create subnet"
  default     = "us-west-2a"
}
variable "public_key_path" {
  description = "Public key path"
  default     = "~/.ssh/id_rsa.pub"
}
variable "instance_ami" {
  description = "AMI for aws EC2 instance"
  #blank windows server
  #default     = "ami-0afb7a78e89642197"
  #sentry license server
  default     = ami-0afb7a78e89642197
}
variable "instance_type" {
  description = "type for aws EC2 instance"
  default     = "t2.micro"
}
variable "environment_tag" {
  description = "Environment tag"
  default     = "Production"
}
variable "ec2_instance_name" {
  description = "EC2 instance name"
  default = "dk-test-instance"
}
variable "vpc_name" {
  description = "name of vpc"
  default = "zemax-k8s"
}
variable "subnet_name" {
  description = "name of subnet"
  default = "zemax-k8s"
}
