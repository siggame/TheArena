using Logger;
using System;
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
                while(true)
                {
                    Thread.Sleep(10000);
                    Console.WriteLine("Sending FTP Zip Test");
                    FTPTester.Test_FTP();
                }
            }
            catch (Exception er)
            {
                Log.TraceMessage(Log.Nav.NavOut, er);
            }
        }

        static void RunHost()
        {
            StartFTPServer();
        }

        static void RunClient()
        {

        }

        static void Main(string[] args)
        {
            Log.TraceMessage(Log.Nav.NavIn, "START", Log.LogType.Info);
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
            var myIP = Dns.GetHostEntry(hostName).AddressList;
            IPAddress arena_host_address = IPAddress.Parse(HOST_ADDR);
            if(myIP.ToList().Contains(arena_host_address))
            {
                RunHost();
            }
            else
            {
                RunClient();
            }
        }
    }
}
