using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TheArena
{
    static class Cpp
    {
        static bool IsCommandLineCpp = true;

        public static void InstallCpp()
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
                                psi.FileName = "StandaloneInstallersWindows64/full.exe";
                                psi.UseShellExecute = false;
                                Log.TraceMessage(Log.Nav.NavIn, "Running 32 bit installer...", Log.LogType.Info);
                                Process p = Process.Start(psi);
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
                                psi.FileName = "StandaloneInstallersWindows32/full.exe";
                                psi.UseShellExecute = false;
                                Log.TraceMessage(Log.Nav.NavIn, "Running 64 bit installer...", Log.LogType.Info);
                                Process p = Process.Start(psi);
                            }
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
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Log.TraceMessage(Log.Nav.NavIn, "Grabbing Shell Process...", Log.LogType.Info);
                    if (process.Start())
                    {
                         Log.TraceMessage(Log.Nav.NavIn, "Testing G++...", Log.LogType.Info);
                        process.StandardInput.WriteLine("g++ -v");
                        //Shows command in use
                        string err = "";
                        for(int i=0; i<9; i++)
                        {
                            err += process.StandardError.ReadLine();
                        }
                        Console.WriteLine(err);
                        if(err.Contains("gcc version"))
                        {
                            //Installed
                            Log.TraceMessage(Log.Nav.NavOut, "C++ Installed.", Log.LogType.Info);
                            IsCommandLineCpp = true;
                        }
                        else
                        {
                            //Need to install
                            Log.TraceMessage(Log.Nav.NavIn, "Installing...", Log.LogType.Info);
                            process.StandardInput.WriteLine("apt-get install g++");
                        }
                    }
                }
            }
        }
    }
}
