using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TheArena
{
    public static class Javascript
    {

        public static bool InstallJavascript()
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

                    Log.TraceMessage(Log.Nav.NavIn, "Checking if Node installed...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("node -v");

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    if (result.Length > 0)
                        Console.WriteLine(result);

                    if (result.ToLower().StartsWith("v"))
                    {
                        Log.TraceMessage(Log.Nav.NavOut, "Node Installed.", Log.LogType.Info);
                        return false;
                    }
                    else
                    {
                        string err = cmdProcess.StandardError.ReadLine();
                        err += cmdProcess.StandardError.ReadLine();
                        Console.WriteLine(err);

                        //If Javascript is not installed there will be an error
                        if (err.Contains("not recognized"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Not Recognized...", Log.LogType.Info);
                            //see if we already installed -

            
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
                                    FileName = "StandaloneInstallersWindows32/node-v8.12.0-x86.msi",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Installing 32 bit node...", Log.LogType.Info);
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
                                    FileName = "StandaloneInstallersWindows64/node-v8.12.0-x64.msi",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Installing 64 bit node...", Log.LogType.Info);
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
                        process.StandardInput.WriteLine("node -v");

                        Log.TraceMessage(Log.Nav.NavIn, "Checking for node...", Log.LogType.Info);
                        string result = process.StandardOutput.ReadLine();

                        if (result.Length > 0)
                            Console.WriteLine(result);

                        if (result.ToLower().StartsWith("v"))
                        {
                            Log.TraceMessage(Log.Nav.NavOut, "Node Installed.", Log.LogType.Info);
                            return false;
                        }
                        else
                        {
                            //Shows command in use
                            string err = "";
                            for (int i = 0; i < 9; i++)
                            {
                                err += process.StandardError.ReadLine();
                            }

                            Console.WriteLine(err);

                            //Need to install
                            Log.TraceMessage(Log.Nav.NavIn, "Installing node...", Log.LogType.Info);
                            process.StandardInput.WriteLine("curl -sL https://deb.nodesource.com/setup_8.x | sudo -E bash -");
                            process.StandardInput.WriteLine("sudo apt-get install -y nodejs");
                            process.StandardInput.WriteLine("sudo npm install -g node-gyp");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool BuildAndRun(string file)
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

                    Log.TraceMessage(Log.Nav.NavOut, "Installing ArgParse if not Installed", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("npm install argparse");

                    Log.TraceMessage(Log.Nav.NavIn, "Building file...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("node "+file);

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    while(result.Length>0 && !result.ToUpper().Contains("WIN") && !result.ToUpper().Contains("LOSE"))
                    {
                        Console.WriteLine(result);
                        result = cmdProcess.StandardOutput.ReadLine();
                    }
                    if(result.ToUpper().Contains("WIN"))
                    {
                        return true;
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
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Log.TraceMessage(Log.Nav.NavIn, "Grabbing Shell Process...", Log.LogType.Info);
                    if (process.Start())
                    {
                        process.StandardInput.WriteLine("npm install argparse");

                        Log.TraceMessage(Log.Nav.NavIn, "Installing ArgParse if haven't already...", Log.LogType.Info);
                        string result = process.StandardOutput.ReadLine();

                        if (result.Length > 0)
                            Console.WriteLine(result);

                        process.StandardInput.WriteLine("cd " + file.Substring(0, file.LastIndexOf('/')));
                        Log.TraceMessage(Log.Nav.NavIn, "Building...", Log.LogType.Info);
                        process.StandardInput.WriteLine("make");
                        result = process.StandardOutput.ReadLine();
                        if (File.Exists("testRun"))
                        {
                            File.Delete("testRun");
                        }
                        using (StreamWriter sw = new StreamWriter("testRun"))
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
                        process.StandardInput.WriteLine("./testRun abxds");
                        while (result.Length > 0 && !result.ToUpper().Contains("WIN") && !result.ToUpper().Contains("LOSE"))
                        {
                            Console.WriteLine(result);
                            result = process.StandardOutput.ReadLine();
                        }
                        if (result.ToUpper().Contains("WIN"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
