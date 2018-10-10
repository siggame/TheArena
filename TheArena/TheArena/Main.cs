using Logger;
using System;
using System.Collections.Generic;
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
        const string HOST_ADDR = "131.151.113.141";
        const string ARENA_FILES_PATH = @"ArenaFiles";
        const int HOST_PORT = 21;
        const int UDP_ASK_PORT = 234;
        const int UDP_CONFIRM_PORT = 432;
        static FtpServer server;
        static List<PlayerInfo> eligible_players = new List<PlayerInfo>();

        static void StartFTPServer(bool is_host)
        {
            try
            {
                Log.TraceMessage(Log.Nav.NavIn, "Starting FTP Server", Log.LogType.Info);
                if (!Directory.Exists(ARENA_FILES_PATH))
                {
                    Directory.CreateDirectory(ARENA_FILES_PATH);
                }
                server = new FtpServer(IPAddress.Parse(HOST_ADDR), HOST_PORT, ARENA_FILES_PATH);
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
        }

        public struct ClientInfo
        {
            public long countsSinceLastPing;
            public IPAddress clientIP;
        }

        private static void ForeverQueueClients()
        {
            List<ClientInfo> clients = new List<ClientInfo>();
            long counter = 0;
            while(true)
            {
                for(int i=0; i<clients.Count; i++)
                {
                    ClientInfo ci = clients[i];
                    ci.countsSinceLastPing++;
                    clients[i] = ci;
                    if(ci.countsSinceLastPing>1000000)
                    {
                        clients.Remove(ci);
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
            StartFTPServer(false);
            UdpClient check_for_game = new UdpClient(UDP_ASK_PORT);
            BuildAndRunGame();
            while (true)
            {
                var remoteEP = new IPEndPoint(IPAddress.Parse(HOST_ADDR), UDP_CONFIRM_PORT);
                check_for_game.Send(new byte[] { 1 }, 1, remoteEP); // Ping -- we are still here
                var data = check_for_game.Receive(ref remoteEP);
                string str_data = System.Text.Encoding.Default.GetString(data);
                if (str_data != null)
                {
                    BuildAndRunGame();
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
                if (myIP.ToList().Contains(arena_host_address))
                {
                    RunHost();
                }
                else
                {
                    RunClient();
                }
            }
            catch (Exception ex)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Exception in Main Thread: " + ex.Message, Log.LogType.Info);
            }
        }
    }
}
