# -*- mode: ruby -*-
# vi: set ft=ruby :

VAGRANTFILE_API_VERSION = "2"

Vagrant.configure(VAGRANTFILE_API_VERSION) do |config|
  config.vm.box_url = "https://vagrantcloud.com/ubuntu/boxes/trusty64"
  config.vm.box = "ubuntu/trusty64"
  
  config.vm.define "redis" do |v|
    v.vm.provider "docker" do |d|
      d.image = "dockerfile/redis"
      d.volumes = ["/var/docker/redis:/data"]
      d.ports = ["6379:6379"]
      d.vagrant_vagrantfile = "./Vagrantfile"
    end
  end
  config.vm.network :forwarded_port, guest: 6379, host: 6379

end
