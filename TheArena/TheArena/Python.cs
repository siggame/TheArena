﻿using Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace TheArena
{
    static class Python
    {

        public static bool InstallPython()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isWindows)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Windows. Starting Background Process...", Log.LogType.Info);
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

                    //Check if Python 3 is installed
                    Log.TraceMessage(Log.Nav.NavIn, "Checking if Python is installed...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("python3 -V");

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    if (result.Length > 0)
                        Console.WriteLine(result);

                    if (result.ToLower().StartsWith("python"))
                    {
                        Log.TraceMessage(Log.Nav.NavOut, "Python Installed.", Log.LogType.Info);
                        return false;
                    }
                    else
                    {
                        string err = cmdProcess.StandardError.ReadLine();
                        err += cmdProcess.StandardError.ReadLine();
                        Console.WriteLine(err);

                        //If Python is not installed there will be an error
                        if (err.Contains("not recognized"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Not Recognized.", Log.LogType.Info);
                            //see if we already installed -

                            //Check if Python installed
                            cmdProcess.StandardInput.AutoFlush = true;

                            //No we didn't install yet.
                            //We will install
                            if (IntPtr.Size == 4)
                            {
                                // 32-bit
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    Verb = "runas",
                                    CreateNoWindow = true,
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = "StandaloneInstallersWindows32/python-3.7.0.exe",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Running 32 bit Python installer.", Log.LogType.Info);
                                Process p = Process.Start(psi);
                            }
                            else if (IntPtr.Size == 8)
                            {
                                // 64-bit
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    Verb = "runas",
                                    CreateNoWindow = true,
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = "StandaloneInstallersWindows64/python-3.7.0-amd64.exe",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Running 64 bit Python installer...", Log.LogType.Info);
                                Process p = Process.Start(psi);
                            }
                            return true;
                        }
                    }
                }
            }
            else if (isLinux)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Linux.", Log.LogType.Info);
                using (Process process = new Process())
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Log.TraceMessage(Log.Nav.NavIn, "Grabbing Shell Process...", Log.LogType.Info);
                    if (process.Start())
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Installing Python...", Log.LogType.Info);
                        process.StandardInput.WriteLine("sudo add-apt-repository ppa:deadsnakes/ppa");
                        process.StandardInput.WriteLine("sudo apt-get update");
                        process.StandardInput.WriteLine("sudo apt-get install python3.7");
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
                    cmdProcess.StandardInput.WriteLine("python3 " + file);


                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    while (result.Length > 0 && !result.ToUpper().Contains("WIN") && !result.ToUpper().Contains("LOSE"))
                    {
                        Console.WriteLine(result);
                        result = cmdProcess.StandardOutput.ReadLine();
                    }
                    if (result.ToUpper().Contains("WIN"))
                    {
                        return "win";
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

                        if (File.Exists(file.Substring(0, file.LastIndexOf('/')+1) + "testRun"))
                        {
                            File.Delete(file.Substring(0, file.LastIndexOf('/')+1) + "testRun");
                        }
                        using (StreamWriter sw = new StreamWriter(file.Substring(0, file.LastIndexOf('/')+1) + "testRun"))
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
                            Log.TraceMessage(Log.Nav.NavIn, "Results File=" + resultsFile, Log.LogType.Info);
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
