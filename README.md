    ████████╗██╗  ██╗███████╗     █████╗ ██████╗ ███████╗███╗   ██╗ █████╗ 
    ╚══██╔══╝██║  ██║██╔════╝    ██╔══██╗██╔══██╗██╔════╝████╗  ██║██╔══██╗
       ██║   ███████║█████╗      ███████║██████╔╝█████╗  ██╔██╗ ██║███████║
       ██║   ██╔══██║██╔══╝      ██╔══██║██╔══██╗██╔══╝  ██║╚██╗██║██╔══██║
       ██║   ██║  ██║███████╗    ██║  ██║██║  ██║███████╗██║ ╚████║██║  ██║
       ╚═╝   ╚═╝  ╚═╝╚══════╝    ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝╚═╝  ╚═╝
                                                                       
                 Run Sig-Game AI Battles for Multiple Languages.
                 Developed by Seth Kitchen August-November 2018
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

# Linux

1) First install git and Clone the repo

    sudo apt-get install git
    git clone https://github.com/scoobyDooIT/TheArena.git

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
                      
7) cd to the root directory (ie /home/sjkyv5/TheArena/TheArena/TheArena) and run command

         sudo dotnet build --configuration Release
    
8) On successful build cd into the compilation folder (ie /home/sjkyv5/TheArena/TheArena/TheArena/bin/Release/netcoreapp2.0) and run using

         sudo dotnet TheArena.dll

Sudo access must be given to do networking otherwise errors will occur. In the event of ANY error, check the Log file

    nano Log.txt
