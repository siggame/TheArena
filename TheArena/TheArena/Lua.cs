using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TheArena
{
    public static class Lua
    {
        public const string LUA_PATH = @"C:\Users\sjkyv5\Documents\5.1\";
        public static bool InstallLua()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isWindows)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Is. Windows. Starting Background Process...", Log.LogType.Info);
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

                    Log.TraceMessage(Log.Nav.NavIn, "Checking if Lua installed...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine(LUA_PATH+"luac");

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    if (result.Length > 0)
                        Console.WriteLine(result);

                    result = cmdProcess.StandardError.ReadLine();

                    if (result.ToLower().StartsWith("luac: no"))
                    {
                        Log.TraceMessage(Log.Nav.NavOut, "Lua Installed.", Log.LogType.Info);
                        return false;
                    }
                    else
                    {
                        string err = result;

                        //If Lua is not installed there will be an error
                        if (err.Contains("not recognized"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Not Recongized.", Log.LogType.Info);
                            //see if we already installed -

                            //Check if Lua installed
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
                                    FileName = "StandaloneInstallersWindows32/LuaForWindows_v5.1.5-52.exe",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Installing 32 bit Lua...", Log.LogType.Info);
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
                                    FileName = "StandaloneInstallersWindows64/LuaForWindows_v5.1.5-52.exe",
                                    UseShellExecute = false
                                };
                                Log.TraceMessage(Log.Nav.NavIn, "Installing 64 bit Lua...", Log.LogType.Info);
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
                        process.StandardInput.WriteLine("luac");

                        Log.TraceMessage(Log.Nav.NavIn, "Checking if Lua is Installed...", Log.LogType.Info);
                        // Reads the Lua version if installed
                        string result = process.StandardError.ReadLine();

                        if (result.Length > 0)
                            Console.WriteLine(result);

                        if (result.ToLower().StartsWith("luac: no"))
                        {
                            // Lua has been installed
                            Log.TraceMessage(Log.Nav.NavOut, "Lua Installed.", Log.LogType.Info);
                            return false;
                        }
                        else
                        {

                            //Need to install
                            Log.TraceMessage(Log.Nav.NavIn, "Installing Lua...", Log.LogType.Info);
                            process.StandardInput.WriteLine("sudo apt-get install luajit");
                            process.StandardInput.WriteLine("sudo apt-get install luarocks");
                            process.StandardInput.WriteLine("sudo luarocks install luasocket");
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

                    Log.TraceMessage(Log.Nav.NavIn, "Building file...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine(LUA_PATH+"luac " + file);


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
                        process.StandardInput.WriteLine("cd " + file.Substring(0, file.LastIndexOf('/')));
                        Log.TraceMessage(Log.Nav.NavIn, "Building...", Log.LogType.Info);
                        string result = process.StandardOutput.ReadLine();
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

                        while (result.Length > 0 && !result.ToUpper().Contains("I WON") && !result.ToUpper().Contains("I LOST"))
                        {
                            Console.WriteLine(result);
                            result = process.StandardOutput.ReadLine();
                        }
                        if (result.ToUpper().Contains("I WON"))
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
