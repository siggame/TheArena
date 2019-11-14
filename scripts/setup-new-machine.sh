#!/bin/bash

#basically just scripting the manual part of the readme

#install and upgrade pip
apt-get -y install python3-pip
pip3 install upgrade pip

echo "STATUS - INSTALLING DOTNET"

#install .NET according to https://dotnet.microsoft.com/download/linux-package-manager/debian10/sdk-current
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
wget -q https://packages.microsoft.com/config/debian/9/prod.list
mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
chown root:root /etc/apt/sources.list.d/microsoft-prod.list
apt-get -y install apt-transport-https
apt-get -y update
apt-get -y install dotnet-sdk-2.0

echo "STATUS - RUNNING client-install.sh"

bash /home/TheArena/scripts/client-install.sh