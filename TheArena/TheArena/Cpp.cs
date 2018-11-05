using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TheArena
{
    static class Cpp
    {
        static bool IsCommandLineCpp = true;

        public static bool InstallCpp()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if(isWindows)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Windows Machine...", Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Starting commandline in the background...", Log.LogType.Info);
                using (Process cmdProcess = new Process())
                {
                    cmdProcess.StartInfo.FileName = "cmd.exe";
                    cmdProcess.StartInfo.UseShellExecute = false;
                    cmdProcess.StartInfo.CreateNoWindow = true;
                    cmdProcess.StartInfo.RedirectStandardOutput = true;
                    cmdProcess.StartInfo.RedirectStandardInput = true;
                    cmdProcess.StartInfo.RedirectStandardError = true;
                    cmdProcess.Start();
                    Log.TraceMessage(Log.Nav.NavIn, "Prints Microsoft version and cmd line intro info", Log.LogType.Info);
                    for (int i=0; i<3; i++)
                    {
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }

                    Log.TraceMessage(Log.Nav.NavIn, "Check if g++ installed...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("g++ -v");

                    Log.TraceMessage(Log.Nav.NavIn, "Shows command in use...", Log.LogType.Info);
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    Log.TraceMessage(Log.Nav.NavIn, "If g++ is not installed there will be an error...", Log.LogType.Info);
                    string err = cmdProcess.StandardError.ReadLine();
                    err += cmdProcess.StandardError.ReadLine();
                    Console.WriteLine(err);
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    if(err.Contains("not recognized"))
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Not Recognized...", Log.LogType.Info);
                        IsCommandLineCpp = false;
                        //see if we already installed -

                        Log.TraceMessage(Log.Nav.NavIn, "Check if cygwin32 g++ is installed...", Log.LogType.Info);
                        cmdProcess.StandardInput.AutoFlush = true;
                        cmdProcess.StandardInput.WriteLine(@"C:\cygnus\cygwin-b20\H-i586-cygwin32\bin\g++ -v");

                        //Shows command in use
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                        //Shows command in use
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                        //If g++ is not installed there will be an error
                        err = cmdProcess.StandardError.ReadLine();

                        //If g++ is not installed there will be an error
                        err = cmdProcess.StandardError.ReadLine();
                        Console.WriteLine(err);
                        if (err.Contains("not recognized"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Not Recognized Either...", Log.LogType.Info);
                            //No we didn't install yet.
                            //We will install
                            if (IntPtr.Size == 4)
                            {
                                Log.TraceMessage(Log.Nav.NavIn, "32 bit machine...", Log.LogType.Info);
                                // 32-bit
                                ProcessStartInfo psi = new ProcessStartInfo();
                                psi.Verb = "runas";
                                psi.Arguments = "/s /v /qn /min";
                                psi.CreateNoWindow = true;
                                psi.WindowStyle = ProcessWindowStyle.Hidden;
                                psi.FileName = "StandaloneInstallersWindows32/full.exe";
                                psi.UseShellExecute = false;
                                Log.TraceMessage(Log.Nav.NavIn, "Running 32 bit installer...", Log.LogType.Info);
                                Process p = Process.Start(psi);
                                psi = new ProcessStartInfo();
                                psi.Verb = "runas";
                                psi.Arguments = "/s /v /qn /min";
                                psi.CreateNoWindow = true;
                                psi.WindowStyle = ProcessWindowStyle.Hidden;
                                psi.FileName = "StandaloneInstallersWindows32/cmake.exe";
                                psi.UseShellExecute = false;
                                Log.TraceMessage(Log.Nav.NavIn, "Running 32 bit installer...", Log.LogType.Info);
                                p = Process.Start(psi);
                            }
                            else if (IntPtr.Size == 8)
                            {
                                Log.TraceMessage(Log.Nav.NavIn, "64 bit machine...", Log.LogType.Info);
                                // 64-bit
                                ProcessStartInfo psi = new ProcessStartInfo();
                                psi.Verb = "runas";
                                psi.Arguments = "/s /v /qn /min";
                                psi.CreateNoWindow = true;
                                psi.WindowStyle = ProcessWindowStyle.Hidden;
                                psi.FileName = "StandaloneInstallersWindows64/full.exe";
                                psi.UseShellExecute = false;
                                Log.TraceMessage(Log.Nav.NavIn, "Running 64 bit installer...", Log.LogType.Info);
                                Process p = Process.Start(psi);
                                psi = new ProcessStartInfo();
                                psi.Verb = "runas";
                                psi.Arguments = "/s /v /qn /min";
                                psi.CreateNoWindow = true;
                                psi.WindowStyle = ProcessWindowStyle.Hidden;
                                psi.FileName = "StandaloneInstallersWindows64/cmake.exe";
                                psi.UseShellExecute = false;
                                Log.TraceMessage(Log.Nav.NavIn, "Running 64 bit installer...", Log.LogType.Info);
                                p = Process.Start(psi);
                            }
                            Log.TraceMessage(Log.Nav.NavIn, "Adding csc to path...", Log.LogType.Info);
                            const string name = "PATH";
                            string pathvar = System.Environment.GetEnvironmentVariable(name);
                            var value = pathvar + @";C:\cygnus\cygwin-b20\H-i586-cygwin32\bin";
                            var target = EnvironmentVariableTarget.User;
                            System.Environment.SetEnvironmentVariable(name, value, target);
                            return true;
                        }
                    }
                    else
                    {
                        // Installed
                        Log.TraceMessage(Log.Nav.NavOut, "Already Installed.", Log.LogType.Info);
                    }
                }
            }
            else if(isLinux)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Linux Machine.", Log.LogType.Info);
                using (Process process = new Process())
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Log.TraceMessage(Log.Nav.NavIn, "Grabbing Shell Process...", Log.LogType.Info);
                    if (process.Start())
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Installing...", Log.LogType.Info);
                        process.StandardInput.WriteLine("apt-get install g++");
                        process.StandardInput.WriteLine("apt-get install cmake");
                    }
                }
            }
            return false;
        }

        public static string BuildAndRun(string file)
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isWindows)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Windows...", Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Starting Background Process...", Log.LogType.Info);
                //Start commandline in the background
                using (Process cmdProcess = new Process())
                {
                    cmdProcess.StartInfo.FileName = "cmd.exe";
                    cmdProcess.StartInfo.UseShellExecute = false;
                    cmdProcess.StartInfo.CreateNoWindow = true;
                    cmdProcess.StartInfo.RedirectStandardOutput = true;
                    cmdProcess.StartInfo.RedirectStandardInput = true;
                    cmdProcess.StartInfo.RedirectStandardError = true;
                    cmdProcess.Start();

                    Log.TraceMessage(Log.Nav.NavIn, "Printing Microsoft Information...", Log.LogType.Info);
                    for (int i = 0; i < 3; i++)
                    {
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }

                    Log.TraceMessage(Log.Nav.NavIn, "Building file...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("make " + file);

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    while (result.Length > 0 && !result.ToUpper().Contains("I WON") && !result.ToUpper().Contains("I LOST"))
                    {
                        Console.WriteLine(result);
                        result = cmdProcess.StandardOutput.ReadLine();
                    }
                    if (result.ToUpper().Contains("I WON"))
                    {
                        return "won";
                    }
                    string err = cmdProcess.StandardError.ReadLine();
                    err += cmdProcess.StandardError.ReadLine();
                    Console.WriteLine(err);
                }
            }
            else if (isLinux)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Linux.", Log.LogType.Info);
                using (Process process = new Process())
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Log.TraceMessage(Log.Nav.NavIn, "Grabbing Shell Process...", Log.LogType.Info);
                    if (process.Start())
                    {
                        process.StandardInput.WriteLine("cd " + file.Substring(0, file.LastIndexOf('/')));
                            
                        if (File.Exists(file.Substring(0, file.LastIndexOf('/')+1)+"testRun"))
                        {
                            File.Delete(file.Substring(0, file.LastIndexOf('/')+1)+"testRun");
                        }
                        using (StreamWriter sw = new StreamWriter(file.Substring(0, file.LastIndexOf('/')+1)+"testRun"))
                        {
                            sw.AutoFlush = true;
                            sw.WriteLine("#!/bin/bash");
                            sw.WriteLine("if [ -z \"$1\" ]");
                            sw.WriteLine("  then");
                            sw.WriteLine("    echo \"No argument(s) supplied. Please specify game session you want to join or make.\"");
                            sw.WriteLine("  else");
                            sw.WriteLine("    ./run ANARCHY -s dev.siggame.tk -r \"$@\"");
                            sw.WriteLine("fi");
                        }
                        Log.TraceMessage(Log.Nav.NavIn, "Rewrote script-- running", Log.LogType.Info);
                        process.StandardInput.WriteLine("sudo make && ./testRun abxds >>results.txt 2>&1");
                        string result = "";
                        do
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Results file not done waiting 1 min...", Log.LogType.Info);
                            Thread.Sleep(1000 * 60); //Wait 1 min for game to finish
                            string resultsFile = file.Substring(0, file.LastIndexOf('/')+1) + "results.txt";
                            Log.TraceMessage(Log.Nav.NavIn, "Results File="+resultsFile, Log.LogType.Info);
                            if (File.Exists(resultsFile))
                            {
                                Log.TraceMessage(Log.Nav.NavIn, "Results file exists reading...", Log.LogType.Info);
                                using (StreamReader sr = new StreamReader(resultsFile))
                                {
                                    result = sr.ReadToEnd() + Environment.NewLine + file;
                                }
                                Log.TraceMessage(Log.Nav.NavIn, "Results=" + result, Log.LogType.Info);
                            }
                            else
                            {
                                Log.TraceMessage(Log.Nav.NavIn, "Results file does not exist...", Log.LogType.Info);
                            }
                        } while (!result.ToLower().Contains("won") && !result.ToLower().Contains("lost") && !result.ToLower().Contains("communication_error"));
                        Log.TraceMessage(Log.Nav.NavIn, "Results contains win or lose or com error--returning...", Log.LogType.Info);
                        return result;
                    }
                }
            }
            return "";
        }
    }
}
