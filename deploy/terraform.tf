################
# Google Cloud
################
# Set project attributes
provider "google" {
  project = "arena-team"
  credentials = "${file("~/Documents/siggame/terraform.json")}"
  region  = "us-central1"
  zone    = "us-central1-c"
}

# Set config for an instance
resource "google_compute_instance" "vm_instance" {
  # Don't use the same name for new instances
  name         = "arena-worker-${count.index}"
  machine_type = "n1-standard-2"

  # How many instances to start
  count=16

  tags = ["allow-all"]

  boot_disk {
    initialize_params {
      image = "ubuntu-os-cloud/ubuntu-1804-lts"
      size = 20
    }
  }

  network_interface {
    # A default network is created for all GCP projects
    network       = "default"
    access_config {
    }
  }
  
  # Change machine-project configuration
  # oslogin is used for ansible to ssh into instances
  metadata = {
    enable-oslogin="true"
  }
}

# Make new firewall rules
resource "google_compute_firewall" "default" {
  name    = "allow-webservices"
  network = "default"

  allow {
    protocol = "icmp"
  }

  allow {
    protocol = "tcp"

    # 80=http, 443=https, 8080 and 8000 are standard dev http
    ports    = ["80", "443", "8080", "8000"]
  }

  target_tags = ["allow-webservices"]
  source_ranges = ["0.0.0.0/0"]
}
