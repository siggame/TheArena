﻿using Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    public class RunGameInfo
    {
        public IPAddress clientRunningGame { get; set; }
        public Game GameRan { get; set; }
        public long startTimeTicks { get; set; }
    }

    class Runner
    {
        const string HOST_ADDR = "35.239.194.206";
        const string ARENA_FILES_PATH = @"/home/sjkyv5/ArenaFiles/";
        const int HOST_PORT = 21;
        const int UDP_ASK_PORT = 234;
        const int UDP_CONFIRM_PORT = 1100;
        static bool runningGame = false;
        static FtpServer server;
        static List<PlayerInfo> eligible_players = new List<PlayerInfo>();
        static ConcurrentQueue<IPAddress> clients = new ConcurrentQueue<IPAddress>();
        static List<RunGameInfo> currentlyRunningGames = new List<RunGameInfo>();
        static Tournament currentlyRunningTourney;

        static void StartFTPServer(bool is_host)
        {
            try
            {
                Log.TraceMessage(Log.Nav.NavIn, "Starting FTP Server", Log.LogType.Info);
                if (!Directory.Exists(ARENA_FILES_PATH))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Arena File Directory didn't exist.. creating now", Log.LogType.Info);
                    Directory.CreateDirectory(ARENA_FILES_PATH);
                }
                if (is_host)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Starting FTP Server on Host Address", Log.LogType.Info);
                    server = new FtpServer(IPAddress.Parse(HOST_ADDR), HOST_PORT, ARENA_FILES_PATH);
                }
                else
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Starting FTP Server on Any Address", Log.LogType.Info);
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
                Log.TraceMessage(Log.Nav.NavOut, er.StackTrace + "", Log.LogType.Error);
            }
        }

        /* private static void SetUpWatcher()
         {
             Log.TraceMessage(Log.Nav.NavIn, "Setting up file system watcher...", Log.LogType.Info);
             FileSystemWatcher watcher = new FileSystemWatcher();
             watcher.Path = ARENA_FILES_PATH;
             watcher.NotifyFilter = NotifyFilters.LastWrite;
             watcher.Filter = "*.*";
             watcher.Changed += new FileSystemEventHandler(ConvertNewFileToPlayerInfo);
             watcher.EnableRaisingEvents = true;
         }*/

        private static void SetUpClientListener()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Setting up client listener...", Log.LogType.Info);
            Thread clientListener = new Thread(ForeverQueueClients);
            clientListener.Start();
        }

        private static void ForeverQueueClients()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Starting UDP Client on port " + UDP_CONFIRM_PORT, Log.LogType.Info);
            using (UdpClient listener = new UdpClient(UDP_CONFIRM_PORT))
            {
                Log.TraceMessage(Log.Nav.NavIn, "30 milliseconds before receive will timeout... ", Log.LogType.Info);
                listener.Client.ReceiveTimeout = 30;
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                long counter = 0;
                while (true)
                {
                    try
                    {
                        byte[] bytes = listener.Receive(ref anyIP);
                        Log.TraceMessage(Log.Nav.NavIn, "Received " + (bytes != null ? bytes.Length : 0) + " bytes.", Log.LogType.Info);
                        if (bytes != null && bytes.Length == 1 && bytes[0] == 1) //Ping
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Hey someone pinged us! " + anyIP.Address, Log.LogType.Info);
                            IPAddress newClient = anyIP.Address;
                            if (!clients.Contains(newClient))
                            {
                                Console.WriteLine("Adding " + anyIP.Address);
                                Log.TraceMessage(Log.Nav.NavIn, "They were new adding them to the client queue.", Log.LogType.Info);
                                clients.Enqueue(newClient);
                            }
                        }
                        else //Game Finished
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Hey a client is saying they finished their assigned game...", Log.LogType.Info);
                            Log.TraceMessage(Log.Nav.NavIn, "CurrentlyRunningGames count=" + currentlyRunningGames.Count, Log.LogType.Info);
                            for (int i = 0; i < currentlyRunningGames.Count; i++)
                            {
                                if (currentlyRunningGames[i].clientRunningGame.Equals(anyIP.Address))
                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "Found a currently running game they should have been running...", Log.LogType.Info);
                                    Log.TraceMessage(Log.Nav.NavIn, "They sent us " + Encoding.ASCII.GetString(bytes), Log.LogType.Info);
                                    string[] str_data = Encoding.ASCII.GetString(bytes).Split(';');
                                    Game toCheck = currentlyRunningGames[i].GameRan;
                                    for (int j = 0; j < toCheck.Competitors.Count; j++)
                                    {
                                        if (toCheck.Competitors[j].Info.TeamName.Contains(str_data[0]))
                                        {
                                            Log.TraceMessage(Log.Nav.NavIn, "We found a complete match--setting winner." + Encoding.ASCII.GetString(bytes), Log.LogType.Info);
                                            toCheck.SetWinner(toCheck.Competitors[j], currentlyRunningTourney, toCheck.Competitors[j].Info.TeamName, str_data[1]);
                                            toCheck.IsComplete = true;
                                            toCheck.IsRunning = false;
                                        }
                                    }
                                }
                            }
                            Log.TraceMessage(Log.Nav.NavIn, "Sending Complete right back at em! " + Encoding.ASCII.GetString(bytes), Log.LogType.Info);
                            SendComplete(anyIP.Address);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("timed out"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Error=" + ex.Message, Log.LogType.Error);
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
                Log.TraceMessage(Log.Nav.NavIn, "Adding Player To Arena " + e.FullPath, Log.LogType.Info);
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
                Log.TraceMessage(Log.Nav.NavIn, "Cpp ", Log.LogType.Info);
                to_add.lang = Languages.Cpp;
            }
            else if (lang.ToLower().Contains("javascript"))
            {
                Log.TraceMessage(Log.Nav.NavIn, "Js ", Log.LogType.Info);
                to_add.lang = Languages.Javascript;
            }
            else if (lang.ToLower().Contains("csharp"))
            {
                Log.TraceMessage(Log.Nav.NavIn, "Csharp ", Log.LogType.Info);
                to_add.lang = Languages.CSharp;
            }
            else if (lang.Contains("java"))
            {
                Log.TraceMessage(Log.Nav.NavIn, "Java ", Log.LogType.Info);
                to_add.lang = Languages.Java;
            }
            else if (lang.Contains("lua"))
            {
                Log.TraceMessage(Log.Nav.NavIn, "Lua ", Log.LogType.Info);
                to_add.lang = Languages.Lua;
            }
            else if (lang.Contains("python"))
            {
                Log.TraceMessage(Log.Nav.NavIn, "Python ", Log.LogType.Info);
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

        static void SendRunGame(IPAddress toRun)
        {
            Log.TraceMessage(Log.Nav.NavIn, "Sending Run Game! ", Log.LogType.Info);
            using (Socket start_game = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                start_game.SendTo(new byte[1] { 0 }, new IPEndPoint(toRun, UDP_ASK_PORT));
            }
        }

        static void SendComplete(IPAddress toComplete)
        {
            Log.TraceMessage(Log.Nav.NavIn, "Sending Complete! ", Log.LogType.Info);
            using (Socket start_game = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                start_game.SendTo(new byte[1] { 1 }, new IPEndPoint(toComplete, UDP_ASK_PORT));
            }
        }

        static void StartTourney(int people_per_game)
        {
            Log.TraceMessage(Log.Nav.NavOut, "Starting Tourney with " + people_per_game + " per game.", Log.LogType.Info);
            eligible_players = new List<PlayerInfo>();
            FillEligiblePlayers();
            Tournament t = new Tournament(eligible_players, people_per_game);
            currentlyRunningTourney = t;
            while (!t.IsDone)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Tourney not done yet ", Log.LogType.Info);
                for (int i = 0; i < currentlyRunningGames.Count; i++)
                {
		    if(currentlyRunningGames[i].GameRan.IsComplete)
		    {
			currentlyRunningGames.RemoveAt(i);
			i--;
		    }
		    else
		    {
		        Log.TraceMessage(Log.Nav.NavIn, "Game "+i+" has been running for "+(new TimeSpan(DateTime.Now.Ticks - currentlyRunningGames[i].startTimeTicks)).TotalMinutes+" minutes.", Log.LogType.Info);
                        Console.WriteLine("Game "+i+" has been running for "+(new TimeSpan(DateTime.Now.Ticks - currentlyRunningGames[i].startTimeTicks)).TotalMinutes+" minutes.");
		        if ((new TimeSpan(DateTime.Now.Ticks - currentlyRunningGames[i].startTimeTicks)).TotalMinutes > 20)
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "It's been 20 minutes and game as not returned -- giving it to another client ", Log.LogType.Info);
                            currentlyRunningGames[i].GameRan.IsRunning = false;
                            currentlyRunningGames[i].GameRan.IsComplete = false;
                            currentlyRunningGames.RemoveAt(i);
                            i--;
                        }
                    }
                }
                Game toAssign;
                t.GetNextNonRunningGame(out toAssign);
                if (toAssign != null)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Game to assign is not null ", Log.LogType.Info);
                    IPAddress clientToRun;
                    bool dequeuedSuccess = clients.TryDequeue(out clientToRun);
                    if (dequeuedSuccess)
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Dequeued Client ", Log.LogType.Info);
                        var files = Directory.GetFiles(ARENA_FILES_PATH);
                        for (int i = 0; i < toAssign.Competitors.Count; i++)
                        {
                            int maxSubmissionNumber = int.MinValue;
                            string maxSubmission = "";
                            for (int j = 0; j < files.Length; j++)
                            {
				Log.TraceMessage(Log.Nav.NavIn, "Currently looking at "+ files[j], Log.LogType.Info);
                                if (files[j].Contains(toAssign.Competitors[i].Info.TeamName))
                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "It contained "+toAssign.Competitors[i].Info.TeamName, Log.LogType.Info);
                                    var split=files[j].Split('_');
                                    var reversed=split.Reverse().ToArray();
                                    Log.TraceMessage(Log.Nav.NavIn,"The submission number checked is "+ reversed[1], Log.LogType.Info);
                                    if (int.Parse(reversed[1]) > maxSubmissionNumber)
                                    {
                                        maxSubmissionNumber = int.Parse(reversed[1]);
                                        maxSubmission = files[j];
                                        Log.TraceMessage(Log.Nav.NavIn, "Max submission number is now " + maxSubmissionNumber + " and " + maxSubmission, Log.LogType.Info);
                                    }
                                }
                            }
                            Log.TraceMessage(Log.Nav.NavIn, "Sending it over FTP", Log.LogType.Info);
                            FTPSender.Send_FTP(maxSubmission, clientToRun.ToString());
                        }
                        Log.TraceMessage(Log.Nav.NavIn, "Sending run game", Log.LogType.Info);
                        SendRunGame(clientToRun);
                        toAssign.IsRunning = true;
                        RunGameInfo rgi = new RunGameInfo();
                        rgi.clientRunningGame = clientToRun;
                        rgi.GameRan = toAssign;
                        rgi.startTimeTicks = DateTime.Now.Ticks;
                        currentlyRunningGames.Add(rgi);
                    }
                }
                else
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Sleeping 5 seconds ", Log.LogType.Info);
                    Thread.Sleep(5000);
                }
            }
        }

        static void FillEligiblePlayers()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Checking in Arena Directory for files to create eligible players...", Log.LogType.Info);
            if (!Directory.Exists(ARENA_FILES_PATH))
            {
                Log.TraceMessage(Log.Nav.NavOut, "Arena File Directory Didn't Exist..Creating it now.", Log.LogType.Info);
                Directory.CreateDirectory(ARENA_FILES_PATH);
            }
            var files = Directory.GetFiles(ARENA_FILES_PATH);
            if (files != null && files.Length > 0)
            {
                Log.TraceMessage(Log.Nav.NavOut, files.Length + " Files existed in Arena Files Directory.", Log.LogType.Info);
                foreach (string f in files)
                {
                    var fs = f.Substring(f.LastIndexOf('/') + 1); // From C:\Users\Me\Documents\team1_1_csharp.zip to team_one_1_cs.zip
                    var withoutZip = fs.Substring(0, fs.LastIndexOf(".zip")); //To team_one_1_cs
                    string[] split = withoutZip.Split('_'); // ["team","one","1","cs"]
                    var reversed = split.Reverse().ToArray(); // ["cs","1","one","team"]
                    string lang = reversed[0]; //"cs"
                    string submission = reversed[1]; //"1"
                    string teamName = "";
                    for (int i = reversed.Length - 1; i > 1; i--)
                    {
                        teamName += reversed[i] + "_"; // "team_one_"
                    }
                    teamName = teamName.Substring(0, teamName.Length - 1); //"team_one"
                    Log.TraceMessage(Log.Nav.NavIn, "Adding team: " + teamName + " with lang" + lang + " and submissionNum=" + submission, Log.LogType.Info);
                    AddPlayerToArena(teamName, submission, lang);
                }
            }
        }

        static void RunHost()
        {
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Host.", Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavOut, "Filling Eligible Players.", Log.LogType.Info);
            //FillEligiblePlayers();
            /*Log.TraceMessage(Log.Nav.NavOut, "Setting Up Directory Watcher.", Log.LogType.Info);
            SetUpWatcher();*/
            Log.TraceMessage(Log.Nav.NavOut, "Setting Up Client Listener.", Log.LogType.Info);
            SetUpClientListener();
            Log.TraceMessage(Log.Nav.NavOut, "Setting Up FTP Server.", Log.LogType.Info);
            StartFTPServer(true);
        }

        static void RunClient()
        {

            bool restartNeeded = false;
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Client.", Log.LogType.Info);
            /*Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be C++...", Log.LogType.Info);
            restartNeeded = Cpp.InstallCpp() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Python...", Log.LogType.Info);
            restartNeeded = Python.InstallPython() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Java...", Log.LogType.Info);
            restartNeeded = Java.InstallJava() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Javascript...", Log.LogType.Info);
            restartNeeded = Javascript.InstallJavascript() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be Lua...", Log.LogType.Info);
            restartNeeded = Lua.InstallLua() || restartNeeded;
            Log.TraceMessage(Log.Nav.NavIn, "Checking for and installing if need be C#...", Log.LogType.Info);
            restartNeeded = CSharp.InstallCSharp() || restartNeeded;*/
            if (restartNeeded)
            {
                Log.TraceMessage(Log.Nav.NavIn, "An install was done and we must restart Visual Studio to use it...", Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Waiting 1 minute for all installs to finish...", Log.LogType.Info);
                Thread.Sleep(1000 * 60);
                var rootDir = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                rootDir = rootDir.Substring(rootDir.IndexOf("//") + 3);
                rootDir = rootDir.Substring(0, rootDir.LastIndexOf(@"/TheArena/")) + "/TheArena.sln";
                Process proc = new Process();
                proc.EnableRaisingEvents = true;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = rootDir;
                Log.TraceMessage(Log.Nav.NavIn, "Starting new Visual Studio...", Log.LogType.Info);
                proc.Start();
                Process[] process = Process.GetProcessesByName("devenv");
                Log.TraceMessage(Log.Nav.NavIn, "Killing This --- Goodbye!...", Log.LogType.Info);
                process[0].Kill();
                Environment.Exit(0);
            }
            Log.TraceMessage(Log.Nav.NavIn, "Starting Client FTP Server...", Log.LogType.Info);
            StartFTPServer(false);
            Log.TraceMessage(Log.Nav.NavIn, "Creating the Keep-Alive Ping that let's the host know we are here and ready to run games...", Log.LogType.Info);
            string resultStr = "";
            using (Socket resultsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                using (Socket ping = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    using (UdpClient ask_for_game = new UdpClient(UDP_ASK_PORT))
                    {
                        ask_for_game.Client.ReceiveTimeout = 5000;
                        Log.TraceMessage(Log.Nav.NavIn, "Only allow 5 seconds for sending and receiving...", Log.LogType.Info);
                        while (true)
                        {
                            try
                            {
                                IPEndPoint remoteEP;
                                if (resultStr == "")
                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "Sending Ping...", Log.LogType.Info);
                                    remoteEP = new IPEndPoint(IPAddress.Parse(HOST_ADDR), UDP_CONFIRM_PORT);
                                    ping.SendTo(new byte[] { 1 }, remoteEP); // Ping -- we are still here
                                }
                                else
                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "Sending Results...", Log.LogType.Info);
                                    remoteEP = new IPEndPoint(IPAddress.Parse(HOST_ADDR), UDP_CONFIRM_PORT);
                                    byte[] toSendResults = Encoding.ASCII.GetBytes(resultStr);
                                    Log.TraceMessage(Log.Nav.NavIn, "Sending toSendResults: " + resultStr + " as bytes=" + toSendResults.Length, Log.LogType.Info);
                                    resultsSocket.SendTo(toSendResults, remoteEP); // Ping -- we are still here
                                }
                                Log.TraceMessage(Log.Nav.NavIn, "Waiting for game...", Log.LogType.Info);
                                byte[] data = ask_for_game.Receive(ref remoteEP);
                                string str_data = System.Text.Encoding.Default.GetString(data);
                                if (data != null && data.Length == 1 && data[0] == 1)
                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "Host received results-clearing results.", Log.LogType.Info);
                                    resultStr = "";
                                    Directory.Delete(ARENA_FILES_PATH, true);
				    Directory.CreateDirectory(ARENA_FILES_PATH);
                                }
                                if (data != null && data.Length == 1 && data[0] == 0)                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "We have been told to run game--LET'S GO!", Log.LogType.Info);
                                    List<string> results = BuildAndRunGame();
                                    Log.TraceMessage(Log.Nav.NavIn, "Results returned with size" + results.Count(), Log.LogType.Info);
                                    string status = "finished";
                                    string winReason = "";
                                    string loseReason = "";
                                    string winnerName = "";
                                    string winnerSubmissionNumber = "";
                                    string loserName = "";
                                    string loserSubmissionNumber = "";
                                    string logURL = "";
                                    foreach (string s in results)
                                    {
                                        string[] split = s.Split(Environment.NewLine);
                                        string[] splitLine = split[split.Length - 1].Split('_');
					splitLine[0]=splitLine[0].Substring(splitLine[0].LastIndexOf('/')+1);
					string[] reversedSplitLine=splitLine.Reverse().ToArray();
					string teamName="";
					for(int i=reversedSplitLine.Length-1; i>1; i--)
					{
					    teamName+=reversedSplitLine[i]+"_";
					}
					teamName=teamName.Substring(0,teamName.Length-1);
                                        Log.TraceMessage(Log.Nav.NavIn, "team name" + teamName, Log.LogType.Info);
                                        string teamSubmissionNumber = reversedSplitLine[1];
                                        Log.TraceMessage(Log.Nav.NavIn, "sub num" + teamSubmissionNumber, Log.LogType.Info);
                                        bool won = false;
                                        foreach (string i in split)
                                        {
                                            if (i.ToUpper().Contains("WON"))
                                            {
                                                Log.TraceMessage(Log.Nav.NavIn, teamName + "won", Log.LogType.Info);
                                                won = true;
						winReason=i;
                                            }
                                            else if (i.ToUpper().Contains("ERROR"))
                                            {
                                                Log.TraceMessage(Log.Nav.NavIn, teamName + "error", Log.LogType.Info);
                                                loseReason = "error";
                                            }
					    else if (i.ToUpper().Contains("LOST"))
					    {
						Log.TraceMessage(Log.Nav.NavIn, teamName + "lost", Log.LogType.Info);
						loseReason=i;
					    }
                                            else if (i.ToUpper().Contains("HTTP"))
                                            {
                                                logURL = i;
                                                Log.TraceMessage(Log.Nav.NavIn, teamName + "logurl" + logURL, Log.LogType.Info);
                                            }
                                        }
					if (won)
                                        {
                                            winnerName = teamName;
                                            winnerSubmissionNumber = teamSubmissionNumber;
                                        }
                                        else
                                        {
                                            loserName = teamName;
                                            loserSubmissionNumber = teamSubmissionNumber;
                                        }
                                    }
				    Log.TraceMessage(Log.Nav.NavOut, status+" "+winReason+" "+loseReason+" "+logURL+" "+winnerName+" "+winnerSubmissionNumber+" "+loserName+" "+loserSubmissionNumber, Log.LogType.Info);
                                    HTTP.HTTPPost(status, winReason, loseReason, logURL, winnerName, winnerSubmissionNumber, loserName, loserSubmissionNumber);
                                    resultStr = winnerName + ";" + logURL;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.TraceMessage(Log.Nav.NavIn, "5 second timeout on receiving game...", Log.LogType.Info);
                            }
                        }
                    }
                }
            }
        }

        public static void RunGame(object fileo, ref List<string> toReturn)
        {
            try
            {
                string file = fileo as string;
                Log.TraceMessage(Log.Nav.NavIn, "Thead for " + file + " started", Log.LogType.Info);
                if (Directory.Exists(file.Substring(0, file.LastIndexOf("."))))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Extraction Directory already existed for " + file + " ...deleting", Log.LogType.Info);
                    Directory.Delete(file.Substring(0, file.LastIndexOf(".")), true);
                }
                Log.TraceMessage(Log.Nav.NavIn, "Extracting ", Log.LogType.Info);
                ZipExtracter.ExtractZip(file, file.Substring(0, file.LastIndexOf(".")));
                if (file.ToLower().Contains("js"))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Building javascript ", Log.LogType.Info);
                    toReturn.Add(Javascript.BuildAndRun(file.Substring(0, file.LastIndexOf(".")) + "/Joueur.js/main.js"));
                }
                else if (file.ToLower().Contains("cpp"))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Building Cpp ", Log.LogType.Info);
                    toReturn.Add(Cpp.BuildAndRun(file.Substring(0, file.LastIndexOf(".")) + "/Joueur.cpp/main.cpp"));
                }
                else if (file.ToLower().Contains("py"))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Building Python ", Log.LogType.Info);
                    toReturn.Add(Python.BuildAndRun(file.Substring(0, file.LastIndexOf(".")) + "/Joueur.py/main.py"));
                }
                else if (file.ToLower().Contains("lua"))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Building Lua ", Log.LogType.Info);
                    toReturn.Add(Lua.BuildAndRun(file.Substring(0, file.LastIndexOf(".")) + "/Joueur.lua/main.lua"));
                }
                else if (file.ToLower().Contains("java"))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Building Java ", Log.LogType.Info);
                    toReturn.Add(Java.BuildAndRun(file.Substring(0, file.LastIndexOf(".")) + "/Joueur.java/main.java"));
                }
                else if (file.ToLower().Contains("cs"))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Building Csharp ", Log.LogType.Info);
                    toReturn.Add(CSharp.BuildAndRun(file.Substring(0, file.LastIndexOf(".")) + "/Joueur.cs/main.cs"));
                }
            }
            catch (Exception ex)
            {
                Log.TraceMessage(Log.Nav.NavIn, ex);
            }
        }

        static List<string> BuildAndRunGame()
        {
            try
            {
                List<string> answers = new List<string>();
                Log.TraceMessage(Log.Nav.NavIn, "Building and Running Game ", Log.LogType.Info);
                if (!Directory.Exists(ARENA_FILES_PATH))
                {
                    Directory.CreateDirectory(ARENA_FILES_PATH);
                }
                var files = Directory.GetFiles(ARENA_FILES_PATH);
                Log.TraceMessage(Log.Nav.NavIn, "ARENA FILES Directory Contains " + files.Count() + " files.", Log.LogType.Info);
                List<Task> allGames = new List<Task>();
                foreach (var file in files)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Creating Thread for file " + file, Log.LogType.Info);
                    allGames.Add(Task.Run(() => RunGame(file, ref answers)));
                }
                Log.TraceMessage(Log.Nav.NavIn, "Starting WaitAll ", Log.LogType.Info);
                Task.WaitAll(allGames.ToArray());
                Log.TraceMessage(Log.Nav.NavIn, "Finished WaitAll", Log.LogType.Info);
                return answers;
            }
            catch (Exception ex)
            {
                Log.TraceMessage(Log.Nav.NavIn, ex);
                return new List<string>();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                //HTTP.HTTPPost("finished", "winReason", "loseReason", "https://www.google.com", "First_Team_1", "1", "First_Team", "1");
                Log.TraceMessage(Log.Nav.NavIn, "START", Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "HOST ADDRESS is " + HOST_ADDR, Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Arena File Directory is " + ARENA_FILES_PATH, Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "HOST PORT " + HOST_PORT, Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "ASK PORT " + UDP_ASK_PORT, Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "CONFIRM PORT " + UDP_CONFIRM_PORT, Log.LogType.Info);
                string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
                Log.TraceMessage(Log.Nav.NavIn, "HOST NAME: " + hostName, Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Deleting all files in arena file directory", Log.LogType.Info);
                if(Directory.Exists(ARENA_FILES_PATH))
                {
                    Directory.Delete(ARENA_FILES_PATH, true);
                }
                var myIP = Dns.GetHostEntry(hostName).AddressList;
                IPAddress arena_host_address = IPAddress.Parse(HOST_ADDR);
                if (myIP.ToList().Contains(arena_host_address))
                {
                    Log.TraceMessage(Log.Nav.NavIn, "My IP matches Host IP", Log.LogType.Info);
                    RunHost();
                }
                else
                {
                    Log.TraceMessage(Log.Nav.NavIn, "My IP does NOT match Host IP", Log.LogType.Info);
                    RunClient();
                }
            }
            catch (Exception ex)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Exception in Main Thread: " + ex.Message, Log.LogType.Error);
            }
        }
    }
}
