#!/bin/bash

ip=$1
game=$2
tourney=$3

sudo dotnet build /home/TheArena/TheArena/TheArena --configuration Release
sudo dotnet /home/TheArena/TheArena/TheArena/bin/Release/netcoreapp2.0/TheArena.dll $ip $game $tourney