using Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using static TheArena.ClientConnection;

namespace TheArena
{
    class Runner
    {
        const string HOST_ADDR = "192.168.0.13";
        const int HOST_PORT = 21;
        static FtpServer server;

        static void StartFTPServer()
        {
            try
            {
                Log.TraceMessage(Log.Nav.NavIn, "Starting FTP Server", Log.LogType.Info);
                server = new FtpServer(IPAddress.Parse(HOST_ADDR), HOST_PORT, @"C:\Users\ArianaGrande\Documents\ArenaFiles");
                server.Start();
                Log.TraceMessage(Log.Nav.NavIn, "Started -- now sleeping forever on this thread", Log.LogType.Info);
                while (true)
                {
                    Thread.Sleep(10000);
                    //Console.WriteLine("Sending FTP Zip Test");
                    //FTPTester.Test_FTP();
                }
            }
            catch (Exception er)
            {
                Log.TraceMessage(Log.Nav.NavOut, er);
            }
        }

        static void StartOneVOneTourney()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Starting One V One Tourney", Log.LogType.Info);
            Tournament t = new Tournament(new List<string> { "a", "b", "c", "d", "e", "f", "g","h","i","j","k","l","m","n","o" }, 7);
        }

        static void RunHost()
        {
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Host.", Log.LogType.Info);
            StartFTPServer();
            //StartOneVOneTourney();
        }

        static void RunClient()
        {
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Client.", Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be C++...", Log.LogType.Info);
            Cpp.InstallCpp();
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Python...", Log.LogType.Info);
            Python.InstallPython();
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Java...", Log.LogType.Info);
            Java.InstallJava();
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Javascript...", Log.LogType.Info);
            Javascript.InstallJavascript();
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Lua...", Log.LogType.Info);
            Lua.InstallLua();
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be C#...", Log.LogType.Info);
            CSharp.InstallCSharp();

        }

        static void Main(string[] args)
        {
            Log.TraceMessage(Log.Nav.NavIn, "START", Log.LogType.Info);
            /*string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
            var myIP = Dns.GetHostEntry(hostName).AddressList;
            IPAddress arena_host_address = IPAddress.Parse(HOST_ADDR);
            if(myIP.ToList().Contains(arena_host_address))
            {
                RunHost();
            }
            else
            {*/
                RunClient();
           // }
        }
    }
}
