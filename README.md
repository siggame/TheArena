
    ████████╗██╗  ██╗███████╗     █████╗ ██████╗ ███████╗███╗   ██╗ █████╗ 
    ╚══██╔══╝██║  ██║██╔════╝    ██╔══██╗██╔══██╗██╔════╝████╗  ██║██╔══██╗
       ██║   ███████║█████╗      ███████║██████╔╝█████╗  ██╔██╗ ██║███████║
       ██║   ██╔══██║██╔══╝      ██╔══██║██╔══██╗██╔══╝  ██║╚██╗██║██╔══██║
       ██║   ██║  ██║███████╗    ██║  ██║██║  ██║███████╗██║ ╚████║██║  ██║
       ╚═╝   ╚═╝  ╚═╝╚══════╝    ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝╚═╝  ╚═╝
                                                                       
                 Run Sig-Game AI Battles for Multiple Languages.
                 Developed by Seth Kitchen and Tanner May
                 August-November 2018 and January-April 2019
                            @Missouri S&T
                 
Currently builds and runs on Windows and Linux, but Windows functionality is only 25-40% done. Could be updated in future semesters.
Instructions for Linux on Google Cloud are below!!!!

There are two forms of TheArena - Host and Client. The form runs automatically based on the given IPs (more specificially described in instructions).

The Host runs no battles, but receives AI zip files from the webserver through FTP,
generates brackets for tournaments, keeps track of all available and clients, and sends battles to run to the clients to advance the
bracket.

The client starts and continously waits for the host to send it files. When the host sends files, it will eventually send a RUN_GAME signal
and the client will run all the AIs sent to it. After it compiles the results, it sends it to the web team for record keeping and to the host
to update the bracket.

## Easy (GUI)
 1. Install python 2.7 (for gcloud)
 2. Install python 3.x (for the gui)
 3. Install gcloud command line tool -> https://cloud.google.com/sdk/gcloud/
 4. Run `gcloud init`
 5. Run `gcloud auth application-default login` -> https://cloud.google.com/compute/docs/tutorials/python-guide
 6. Make sure all requirements for tkinter are installed
 7. Install pip
 8. Run `pip install --user --upgrade google-api-python-client`
 9. Run `git clone https://github.com/siggame/TheArena.git`
 10. Run `python gui.py`

To make a new image for the disk:
1. Create a server with the old image using the "Create similar" option.
2. 2a: (If you are simply adding to the old image) Do the additions to the server created in step one. Shut off machine.
2b: (If you are doing something completely different) In the create similar tab scroll to the disk section and change it to the Debian GNU/Linux 9 (stretch). Then do the needed operations. Shut off machine.
3. Create a new image based off of the newly modified server.
4. (Not needed but useful in case the image is deleted) Create a similar server to that created in step 2 but select the new image to use.
5. Change the IMAGE_NAME value in code.

Regarding imports:
If the googleapiclient is giving an error use `pip3` instead of `pip` in the install command.

Startup script info:
`nano /var/log/syslog` on cloud server to see if startup script ran.

Google Cloud Service Project Attributes:

PROJECT: the id (not name) of your project in Google Cloud Console, found in upper left of window, to the right of "Google Cloud Platform"

ZONE: the zone (not region!) you selected when installing gcloud, if you forget: https://cloud.google.com/compute/docs/gcloud-compute/#set_default_zone_and_region_in_your_local_client

REGION: same process as ZONE

Note: zone and region can also be changed when creating new servers. If you have done this then that is what you want to use when inputting values into the gui.


## Manual

**SOME OF THESE STEPS MAY NOT APPLY WHILE WE ARE UNDERGOING MODIFICATIONS**

Make sure if you are running on Google Cloud that you enable IP Forwarding, and enable all UDP/TCP connections inbound and outbound in firewall rules.

1) First install git and Clone the repo:

    sudo apt-get install git
      
    git clone https://github.com/siggame/TheArena.git

2) Open Main.cs and Change the IPAddresses to match the host and the your own computer. If you are the host, the HOST_ADDR should be an
   internal IP (ie my internal IP listed on Google Cloud - 10 dot 128 dot 0 dot 2). If you are a client the HOST_ADDR should be the external IP address
   of the HOST - NOT YOUR COMPUTER - (ie the external IP listed on Google Cloud for the server is 35 dot 239 dot 194 dot 206 ).

3) Also in Main.cs change the ARENA_FILES_PATH to something local on your machine (ie mine is /home/sjkyv5/ArenaFiles). The path up to the last
   directory must exist before starting the program (ie /home/sjkyv5 must exist before starting, but ArenaFiles would be created or used whether
   or not it exists.

4 -- prob not neccessary)  If the ports given are already being used or are blocked you can change them, but they must also be changed on the host.

5) Install dotnet. Pick correct linux distro and Follow Directions here: https://www.microsoft.com/net/download/linux-package-manager/sles/sdk-current

6 -- only for client) Install Compilers - LuaJit, LuaRocks, LuaSocket, Node, NodeGyp, Python3, g++, cmake, and java8 -- You can check out the
                      commands in Cpp.cs, Java.cs, Lua.cs, etc where originally we installed automatically, but the Ubuntu commands were not the same
                      as the Debian commands and we deprecated automatic install.

recently worked install commands (use pip3.7 install ....):

    sudo bash
    curl -sL https://deb.nodesource.com/setup_11.x | bash -
    apt-get install -y luajit luarocks nodejs g++ cmake default-jre default-jdk libncursesw5-dev libssl-dev libsqlite3-dev tk-dev libgdbm-dev libc6-dev libbz2-dev build-essential zlib1g-dev libbz2-dev liblzma-dev libncurses5-dev libreadline6-dev libsqlite3-dev libssl-dev libgdbm-dev liblzma-dev tk8.5-dev lzma lzma-dev libgdbm-dev uuid-dev python3-dev python3-setuptools libffi-dev libunwind-dev maven
    luarocks install luasocket
    npm install -g node-gyp
    wget https://www.python.org/ftp/python/3.7.3/Python-3.7.3.tgz
    tar -xzvf Python-3.7.3.tgz
    cd Python-3.7.3
    mkdir build
    cd build
    ../configure --enable-optimizations --with-ensurepip=install
    make -j8
    make altinstall
    update-alternatives --install /usr/bin/python python /usr/local/bin/python3.7 50
    alias pip=pip3.7
    alias pip3=pip3.7
    alias python3=python
                      
7) cd to the root directory (ie /home/sjkyv5/TheArena/TheArena/TheArena) and run command

         sudo dotnet build --configuration Release
    
8) On successful build cd into the compilation folder (ie /home/sjkyv5/TheArena/TheArena/TheArena/bin/Release/netcoreapp2.0) and run using

         sudo dotnet TheArena.dll

Sudo access must be given to do networking otherwise errors will occur. In the event of ANY error, check the Log file

    nano Log.txt
    
    
 To keep persistant do:
 
     screen -S arena
 
 then run the game in the screen and when it's running do CTRL+A+D and it will return to the original linux and you can close the application.
 
 When you come back and want to view again do:
 
     screen -r arena
    

