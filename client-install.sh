#!/bin/bash
curl -sL https://deb.nodesource.com/setup_11.x | bash -
apt-get install -y luajit luarocks nodejs g++ cmake default-jre default-jdk libncursesw5-dev libssl-dev libsqlite3-dev tk-dev libgdbm-dev libc6-dev libbz2-dev build-essential zlib1g-dev libbz2-dev liblzma-dev libncurses5-dev libreadline6-dev libsqlite3-dev libssl-dev libgdbm-dev liblzma-dev tk8.5-dev lzma lzma-dev libgdbm-dev uuid-dev python3-dev python3-setuptools libffi-dev libunwind-dev maven
luarocks install luasocket
npm install -g node-gyp
wget https://www.python.org/ftp/python/3.7.3/Python-3.7.3.tgz
tar -xzvf Python-3.7.3.tgz
cd Python-3.7.3
mkdir build
cd build
../configure --with-ensurepip=install
make -j8
make altinstall
update-alternatives --install /usr/bin/python python /usr/local/bin/python3.7 50
alias pip=pip3.7
alias pip3=pip3.7
alias python3=python
