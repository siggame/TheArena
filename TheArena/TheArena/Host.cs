using Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TheArena
{
    //Partial means -> Some of this class is defined in other files for readability
    public partial class Runner
    {
        // Tells Client to Run the game with the AIs we sent them through FTP.
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
                                if (currentlyRunningGames[i].ClientRunningGame.Equals(anyIP.Address))
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

        static void SendRunGame(IPAddress toRun)
        {
            Log.TraceMessage(Log.Nav.NavIn, "Sending Run Game! ", Log.LogType.Info);
            using (Socket start_game = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                start_game.SendTo(new byte[1] { 0 }, new IPEndPoint(toRun, UDP_ASK_PORT));
            }
        }

        // Tells Client that game has been recorded and they can destroy all relevant info.
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

            if (!FillEligiblePlayers())
            {
                return;
            }

            Tournament t = new Tournament(eligible_players, people_per_game);
            currentlyRunningTourney = t;
            while (!t.IsDone)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Tourney not done yet ", Log.LogType.Info);
                for (int i = 0; i < currentlyRunningGames.Count; i++)
                {
                    if (currentlyRunningGames[i].GameRan.IsComplete)
                    {
                        currentlyRunningGames.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Game " + i + " has been running for " + (new TimeSpan(DateTime.Now.Ticks - currentlyRunningGames[i].StartTimeTicks)).TotalMinutes + " minutes.", Log.LogType.Info);
                        Console.WriteLine("Game " + i + " has been running for " + (new TimeSpan(DateTime.Now.Ticks - currentlyRunningGames[i].StartTimeTicks)).TotalMinutes + " minutes.");
                        if ((new TimeSpan(DateTime.Now.Ticks - currentlyRunningGames[i].StartTimeTicks)).TotalMinutes > 8)
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "It's been 20 minutes and game as not returned -- giving it to another client ", Log.LogType.Info);
                            currentlyRunningGames[i].GameRan.IsRunning = false;
                            currentlyRunningGames[i].GameRan.IsComplete = false;
                            currentlyRunningGames.RemoveAt(i);
                            i--;
                        }
                    }
                }
                t.GetNextNonRunningGame(out Game toAssign);
                if (toAssign != null)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Game to assign is not null ", Log.LogType.Info);
                    bool dequeuedSuccess = clients.TryDequeue(out IPAddress clientToRun);
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
                                Log.TraceMessage(Log.Nav.NavIn, "Currently looking at " + files[j], Log.LogType.Info);
                                if (files[j].Contains(toAssign.Competitors[i].Info.TeamName))
                                {
                                    Log.TraceMessage(Log.Nav.NavIn, "It contained " + toAssign.Competitors[i].Info.TeamName, Log.LogType.Info);
                                    var split = files[j].Split('_');
                                    var reversed = split.Reverse().ToArray();
                                    Log.TraceMessage(Log.Nav.NavIn, "The submission number checked is " + reversed[1], Log.LogType.Info);
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
                        currentlyRunningGames.Add(new RunGameInfo() { ClientRunningGame = clientToRun, GameRan = toAssign, StartTimeTicks = DateTime.Now.Ticks });
                    }
                }
                else
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Sleeping 5 seconds ", Log.LogType.Info);
                    Thread.Sleep(5000);
                }
            }
        }

        static bool FillEligiblePlayers()
        {
            Log.TraceMessage(Log.Nav.NavIn, "Checking in Arena Directory for files to create eligible players...", Log.LogType.Info);
            if (!Directory.Exists(ARENA_FILES_PATH))
            {
                Log.TraceMessage(Log.Nav.NavOut, "Arena File Directory Didn't Exist..Creating it now.", Log.LogType.Info);
                Directory.CreateDirectory(ARENA_FILES_PATH);
            }
            var files = Directory.GetFiles(ARENA_FILES_PATH);
            Log.TraceMessage(Log.Nav.NavOut, "FilesPath " + ARENA_FILES_PATH, Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavOut, "Files " + files, Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavOut, "FilesLen " + files.Length, Log.LogType.Info);
            if (files != null && files.Length > 0)
            {
                Log.TraceMessage(Log.Nav.NavOut, files.Length + " Files existed in Arena Files Directory.", Log.LogType.Info);
                Dictionary<string, Tuple<string, int>> filesWithSubmission = new Dictionary<string, Tuple<string, int>>();
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
                    if(filesWithSubmission.TryGetValue(teamName, out Tuple<string,int> subNum))
                    {
                        if(int.Parse(submission)>subNum.Item2)
                        {
                            filesWithSubmission[teamName] = new Tuple<string,int>(lang,int.Parse(submission));
                        }
                    }
                    else
                    {
                        filesWithSubmission[teamName]= new Tuple<string, int>(lang, int.Parse(submission));
                    }
                }
                foreach (var x in filesWithSubmission)
                {
                    Log.TraceMessage(Log.Nav.NavIn, "Adding team: " + x.Key + " with lang " + x.Value.Item1 + " and submissionNum=" + x.Value.Item2, Log.LogType.Info);

                    AddPlayerToArena(x.Key, x.Value.Item2+"", x.Value.Item1);
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        static void RunHost()
        {
            Log.TraceMessage(Log.Nav.NavIn, "This Arena is Host.", Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavOut, "Filling Eligible Players.", Log.LogType.Info);
            Log.TraceMessage(Log.Nav.NavOut, "Setting Up Client Listener.", Log.LogType.Info);
            SetUpClientListener();
            Log.TraceMessage(Log.Nav.NavOut, "Setting Up FTP Server.", Log.LogType.Info);
            StartFTPServer(true);
        }
    }
}
