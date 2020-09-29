
variable "public_key_path" {
  description = "Public key path"
  default     = "~/.ssh/id_rsa.pub"
}

variable "environment_tag" {
  description = "Environment tag"
  default     = "Production"
}

variable "instance_ami" {
  description = "AMI for aws EC2 instance"
  default     = "ami-0afb7a78e89642197"
}

variable "instance_type" {
  description = "type for aws EC2 instance"
  default     = "t2.micro"
}


variable "ec2_instance_name" {
  description = "EC2 instance name"
  default = "zemax-license-server"
}

resource "aws_security_group" "sg_22" {
  name   = "sg_22"
  vpc_id = module.vpc.vpc_id

  # SSH access from the VPC
  ingress {
    from_port   = 3389 
    to_port     = 3389 
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 3389
    to_port     = 3389
    protocol    = "udp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    "Environment" = var.environment_tag
  }
}

resource "aws_key_pair" "ec2key" {
  key_name   = "publicKey"
  public_key = file(var.public_key_path)
}

resource "aws_instance" "testInstance" {
  ami                    = var.instance_ami
  instance_type          = var.instance_type
  subnet_id              = module.vpc.public_subnets[0]
  vpc_security_group_ids = [aws_security_group.sg_22.id]
  key_name               = aws_key_pair.ec2key.key_name

  tags = {
    "Environment" = var.environment_tag
    "Name" = var.ec2_instance_name
  }
}
