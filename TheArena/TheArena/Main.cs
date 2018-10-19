using Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static TheArena.ClientConnection;

namespace TheArena
{
    public enum Languages
    {
        Cpp,
        CSharp,
        Java,
        Javascript,
        Python,
        Lua,
        None
    }

    public class PlayerInfo
    {
        public Languages lang { get; set; }
        public string TeamName { get; set; }
        public string Submission { get; set; }
    }

    class Runner
    {
        const string HOST_ADDR = "10.106.68.140";
        const string ARENA_FILES_PATH = @"ArenaFiles";
        const int HOST_PORT = 21;
        const int UDP_ASK_PORT = 234;
        const int UDP_CONFIRM_PORT = 1100;
        static FtpServer server;
        static List<PlayerInfo> eligible_players = new List<PlayerInfo>();
        static ConcurrentQueue<IPAddress> clients = new ConcurrentQueue<IPAddress>();

        static void StartFTPServer(bool is_host)
        {
            try
            {
                Log.TraceMessage(Log.Nav.NavIn, "Starting FTP Server", Log.LogType.Info);
                if (!Directory.Exists(ARENA_FILES_PATH))
                {
                    Directory.CreateDirectory(ARENA_FILES_PATH);
                }
                if (is_host)
                {
                    server = new FtpServer(IPAddress.Parse(HOST_ADDR), HOST_PORT, ARENA_FILES_PATH);
                }
                else
                {
                    server = new FtpServer(IPAddress.Any, HOST_PORT, ARENA_FILES_PATH);
                }
                server.Start();
                if (is_host)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Started -- now waiting for commands forever on this thread", Log.LogType.Info);
                    while (true)
                    {
                        Console.WriteLine("Type T2 to start a tourney with 2 people per game. T3 to start a tourney with 3 people per game etc.");
                        string command = Console.ReadLine();
                        if (command == "T2")
                        {
                            StartTourney(2);
                        }
                    }
                }
            }
            catch (Exception er)
            {
                Log.TraceMessage(Log.Nav.NavOut, er);
            }
        }

        private static void SetUpWatcher()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Setting up file system watcher...", Log.LogType.Info);
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = ARENA_FILES_PATH;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(ConvertNewFileToPlayerInfo);
            watcher.EnableRaisingEvents = true;
        }

        private static void SetUpClientListener()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Setting up client listener...", Log.LogType.Info);
            Thread clientListener = new Thread(ForeverQueueClients);
            clientListener.Start();
        }

        private static void ForeverQueueClients()
        {
            using (UdpClient listener = new UdpClient(UDP_CONFIRM_PORT))
            {
                using (Socket start_game = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    listener.Client.ReceiveTimeout = 30;
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    long counter = 0;
                    while (true)
                    {
                        try
                        {
                            byte[] bytes = listener.Receive(ref anyIP);
                            IPAddress newClient = anyIP.Address;
                            if (!clients.Contains(newClient))
                            {
                                clients.Enqueue(newClient);
                                start_game.SendTo(new byte[1], new IPEndPoint(newClient,UDP_ASK_PORT));
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        private static void ConvertNewFileToPlayerInfo(object sender, FileSystemEventArgs e)
        {
            Log.TraceMessage(Log.Nav.NavIn, "Directory changed!! Changed file: " + e.FullPath, Log.LogType.Info);
            var fs = e.FullPath.Substring(e.FullPath.LastIndexOf('\\') + 1);
            string[] split = fs.Split('_');
            if (split.Length == 3)
            {
                AddPlayerToArena(split[0], split[1], split[2]);
            }
        }

        public static void AddPlayerToArena(string TeamName, string Submission, string lang)
        {
            PlayerInfo to_add = new PlayerInfo();
            to_add.TeamName = TeamName;
            to_add.Submission = Submission;
            if (lang.ToLower().Contains("cpp"))
            {
                to_add.lang = Languages.Cpp;
            }
            else if (lang.ToLower().Contains("javascript"))
            {
                to_add.lang = Languages.Javascript;
            }
            else if (lang.ToLower().Contains("csharp"))
            {
                to_add.lang = Languages.CSharp;
            }
            else if (lang.Contains("java"))
            {
                to_add.lang = Languages.Java;
            }
            else if (lang.Contains("lua"))
            {
                to_add.lang = Languages.Lua;
            }
            else if (lang.Contains("python"))
            {
                to_add.lang = Languages.Python;
            }
            if (!eligible_players.Contains(to_add))
            {
                Log.TraceMessage(Log.Nav.NavOut, "Didn't exist adding " + to_add.TeamName + " " + to_add.Submission + " " + to_add.lang.ToString(), Log.LogType.Info);
                eligible_players.Add(to_add);
            }
            else
            {
                Log.TraceMessage(Log.Nav.NavOut, "Already existed -- didn't add again.", Log.LogType.Info);
            }
        }

        static void StartTourney(int people_per_game)
        {
            Log.TraceMessage(Log.Nav.NavOut, "Starting Tourney with " + people_per_game + " per game.", Log.LogType.Info);
            Tournament t = new Tournament(eligible_players, people_per_game);
        }

        static void FillEligiblePlayers()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Checking in Arena Directory for files to create eligible players...", Log.LogType.Info);
            if (!Directory.Exists(ARENA_FILES_PATH))
            {
                Directory.CreateDirectory(ARENA_FILES_PATH);
            }
            var files = Directory.GetFiles(ARENA_FILES_PATH);
            foreach (string f in files)
            {
                var fs = f.Substring(f.LastIndexOf('\\') + 1);
                string[] split = fs.Split('_');
                if (split.Length == 3)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Adding team: " + split[0], Log.LogType.Info);
                    AddPlayerToArena(split[0], split[1], split[2]);
                }
            }
        }

        static void RunHost()
        {
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Host.", Log.LogType.Info);
            FillEligiblePlayers();
            SetUpWatcher();
            SetUpClientListener();
            StartFTPServer(true);
        }

        static void RunClient()
        {
            /*
            bool restartNeeded = false;
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Client.", Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be C++...", Log.LogType.Info);
            restartNeeded=Cpp.InstallCpp() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Python...", Log.LogType.Info);
            restartNeeded=Python.InstallPython() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Java...", Log.LogType.Info);
            restartNeeded=Java.InstallJava() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Javascript...", Log.LogType.Info);
            restartNeeded=Javascript.InstallJavascript() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Lua...", Log.LogType.Info);
            restartNeeded=Lua.InstallLua() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be C#...", Log.LogType.Info);
            restartNeeded=CSharp.InstallCSharp() || restartNeeded;
            if(restartNeeded)
            {
                Log.TraceMessage(Log.Nav.NavIn, "An install was done and we must restart Visual Studio to use it...", Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Waiting 1 minute for all installs to finish...", Log.LogType.Info);
                Thread.Sleep(1000*60);
                var rootDir = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                rootDir = rootDir.Substring(rootDir.IndexOf("//") + 3);
                rootDir = rootDir.Substring(0, rootDir.LastIndexOf(@"/TheArena/"))+"/TheArena.sln";
                Process proc = new Process();
                proc.EnableRaisingEvents = true;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName=rootDir;
                Log.TraceMessage(Log.Nav.NavIn, "Starting new Visual Studio...", Log.LogType.Info);
                proc.Start();
                Process[] process = Process.GetProcessesByName("devenv");
                Log.TraceMessage(Log.Nav.NavIn, "Killing This --- Goodbye!...", Log.LogType.Info);
                process[0].Kill();
                Environment.Exit(0);
            }
            Log.TraceMessage(Log.Nav.NavIn, "Starting Client FTP Server...", Log.LogType.Info);
            StartFTPServer(false);
            Log.TraceMessage(Log.Nav.NavIn, "Creating the Keep-Alive Ping that let's the host know we are here and ready to run games...", Log.LogType.Info);*/
            using (Socket check_for_game = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Log.TraceMessage(Log.Nav.NavIn, "Only allow 5 seconds for sending and receiving...", Log.LogType.Info);
                //check_for_game.Client.SendTimeout = 5000;
                //check_for_game.Client.ReceiveTimeout = 5000;
                //BuildAndRunGame();
                while (true)
                {
                    try
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Sending Ping...", Log.LogType.Info);
                        var remoteEP = new IPEndPoint(IPAddress.Parse(HOST_ADDR), UDP_CONFIRM_PORT);
                        check_for_game.SendTo(new byte[] { 1 }, remoteEP); // Ping -- we are still here
                        Log.TraceMessage(Log.Nav.NavIn, "Waiting for game...", Log.LogType.Info);
                        byte[] data = new byte[1024];
                        //check_for_game.Receive(data);
                        string str_data = System.Text.Encoding.Default.GetString(data);
                        if (str_data != null)
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "We have been told to run game--LET'S GO!", Log.LogType.Info);
                            BuildAndRunGame();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "5 second timeout on receiving game...", Log.LogType.Info);
                    }
                }
            }
        }

        static void BuildAndRunGame()
        {
            if (!Directory.Exists(ARENA_FILES_PATH))
            {
                Directory.CreateDirectory(ARENA_FILES_PATH);
            }
            var files = Directory.GetFiles(ARENA_FILES_PATH);
            foreach (var file in files)
            {
                ZipExtracter.ExtractZip(file, file.Substring(0, file.IndexOf(".")));
                if (file.ToLower().Contains("javascript"))
                {
                    Javascript.BuildAndRun(file + "/Joueur.js/main.js");
                }
                else if (file.ToLower().Contains("cpp"))
                {
                    Cpp.BuildAndRun(file + "/Joueur.cpp/main.cpp");
                }
                else if (file.ToLower().Contains("python"))
                {
                    Python.BuildAndRun(file + "/Joueur.py/main.py");
                }
                else if (file.ToLower().Contains("lua"))
                {
                    Lua.BuildAndRun(file + "/Joueur.lua/main.lua");
                }
                else if (file.ToLower().Contains("java"))
                {
                    Java.BuildAndRun(file + "/Joueur.java/main.java");
                }
                else if (file.ToLower().Contains("csharp"))
                {
                    CSharp.BuildAndRun(file + "/Joueur.cs/main.cs");
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Log.TraceMessage(Log.Nav.NavIn, "START", Log.LogType.Info);
                string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
                var myIP = Dns.GetHostEntry(hostName).AddressList;
                IPAddress arena_host_address = IPAddress.Parse(HOST_ADDR);
               /* if (myIP.ToList().Contains(arena_host_address))
                {*/
                    RunHost();
                /*}
                else
                {
                    RunClient();
                }*/
            }
            catch (Exception ex)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Exception in Main Thread: " + ex.Message, Log.LogType.Info);
            }
        }
    }
}
