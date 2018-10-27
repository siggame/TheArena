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
    public static class Java
    {
        public static bool InstallJava()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isWindows)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is Windows...", Log.LogType.Info);
                Log.TraceMessage(Log.Nav.NavIn, "Starting Background Process...", Log.LogType.Info);
                using (Process cmdProcess = new Process())
                {
                    cmdProcess.StartInfo.FileName = "cmd.exe";
                    cmdProcess.StartInfo.UseShellExecute = false;
                    cmdProcess.StartInfo.CreateNoWindow = true;
                    cmdProcess.StartInfo.RedirectStandardOutput = true;
                    cmdProcess.StartInfo.RedirectStandardInput = true;
                    cmdProcess.StartInfo.RedirectStandardError = true;
                    cmdProcess.Start();

                    Log.TraceMessage(Log.Nav.NavIn, "Printing Microsoft Info...", Log.LogType.Info);
                    for (int i = 0; i < 3; i++)
                    {
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }

                    Log.TraceMessage(Log.Nav.NavIn, "Checking if Java Installed javac...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("javac");

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    if (result.Length > 0)
                        Console.WriteLine(result);

                    if (result.ToLower().StartsWith("usage"))
                    {
                        Log.TraceMessage(Log.Nav.NavOut, "Java installed.", Log.LogType.Info);
                        // Java has been installed
                        return false;
                    }
                    else
                    {
                        string err = cmdProcess.StandardError.ReadLine();
                        err += cmdProcess.StandardError.ReadLine();
                        Console.WriteLine(err);

                        //If Java is not installed there will be an error
                        if (err.Contains("not recognized"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Not Recognized...", Log.LogType.Info);
                            //see if we already installed -

                            //Check if JDK installed
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
                                    FileName = "StandaloneInstallersWindows32/jdk-8u181-windows-i586.exe",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Installing 32 bit java...", Log.LogType.Info);
                                Process p = Process.Start(psi);
                                Log.TraceMessage(Log.Nav.NavIn, "Adding to PATH...", Log.LogType.Info);
                                const string name = "PATH";
                                string pathvar = System.Environment.GetEnvironmentVariable(name);
                                var value = pathvar + @";C:\Program Files (x86)\Java\jdk1.8.0_181\bin";
                                var target = EnvironmentVariableTarget.Machine;
                                System.Environment.SetEnvironmentVariable(name, value, target);
                            }
                            else if (IntPtr.Size == 8)
                            {
                                // 64-bit
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    Verb = "runas",
                                    CreateNoWindow = true,
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = "StandaloneInstallersWindows64/jdk-10.0.2_windows-x64_bin.exe",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Installing 64 bit java...", Log.LogType.Info);
                                Process p = Process.Start(psi);
                                const string name = "PATH";
                                string pathvar = System.Environment.GetEnvironmentVariable(name);
                                var value = pathvar + @";C:\Program Files\Java\jdk-10.0.2\bin";
                                var target = EnvironmentVariableTarget.Machine;
                                System.Environment.SetEnvironmentVariable(name, value, target);
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
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Log.TraceMessage(Log.Nav.NavIn, "Grabbing Shell Process...", Log.LogType.Info);
                    if (process.Start())
                    {
                        Log.TraceMessage(Log.Nav.NavIn, "Installing...", Log.LogType.Info);
                        process.StandardInput.WriteLine("sudo add-apt-repository ppa:webupd8team/java");
                        process.StandardInput.WriteLine("sudo apt-get update");
                        process.StandardInput.WriteLine("sudo apt-get install oracle-java8-installer");
                        process.StandardInput.WriteLine("sudo apt-get install oracle-java8-set-default");
                        process.StandardInput.WriteLine("sudo apt-get install maven");                       
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
                    cmdProcess.StandardInput.WriteLine("javac " + file);


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
                        process.StandardInput.WriteLine("make && ./testRun abxds >>results.txt 2>&1");
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
