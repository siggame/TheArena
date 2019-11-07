#!/bin/bash

#basically just scripting the manual part of the readme



# install .NET according to https://dotnet.microsoft.com/download/linux-package-manager/debian10/sdk-current
# wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
# mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
# wget -q https://packages.microsoft.com/config/debian/10/prod.list
# mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
# chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
# chown root:root /etc/apt/sources.list.d/microsoft-prod.list
# apt -y install apt-transport-https
# apt -y install dotnet-sdk-3.0
./dotnet-install.sh
./client-install.sh