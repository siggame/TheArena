#!/bin/bash

# Install Node.js v11.x in Debian as root (source: https://github.com/nodesource/distributions)
curl -sL https://deb.nodesource.com/setup_11.x | bash - 

# Install many compilers and softwares necessary for The Arena to function
apt-get install -y luajit luarocks nodejs g++ cmake default-jre default-jdk libncursesw5-dev libssl-dev libsqlite3-dev tk-dev libgdbm-dev libc6-dev libbz2-dev build-essential zlib1g-dev libbz2-dev liblzma-dev libncurses5-dev libreadline6-dev libsqlite3-dev libssl-dev libgdbm-dev liblzma-dev tk8.5-dev lzma lzma-dev libgdbm-dev uuid-dev python3-dev python3-setuptools libffi-dev libunwind-dev maven

# Use the lua package manager to install a socket module
luarocks install luasocket

# Use Node Package Manager to install a tool which compiles native Node.js modules (written in C or C++ and therefore need to be compiled)
npm install -g node-gyp

# Download Python 3.8.0 as a compressed TAR file
wget https://www.python.org/ftp/python/3.8.0/Python-3.8.0.tgz

# Unzip the Python files
tar -xzvf Python-3.8.0.tgz

# Enter the Python folder
cd Python-3.8.0

# Create a directory called build and enter it
mkdir build
cd build

# Configures Python to optimize code on and ensures pip is installed
# ../configure --enable-optimizations --with-ensurepip=install
../configure --with-ensurepip=install

# Configures the make command to run using 8 simultaneous jobs
echo "STATUS - J8"
make -j8

# Builds Python without masquerading the python3 binary (source: https://docs.python.org/3/using/unix.html#building-python)
echo "STATUS - altinstall"
make altinstall

# Forces python to refer to python3.8 with a priority of 50 (source: https://linux.die.net/man/8/update-alternatives)
update-alternatives --install /usr/bin/python python /usr/local/bin/python3.8 50

# Adds pip and python aliases
alias pip=pip3.8
alias pip3=pip3.8
alias python3=python
