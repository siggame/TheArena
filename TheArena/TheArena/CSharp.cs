using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TheArena
{
    public static class CSharp
    {
        public static void InstallCSharp()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isWindows)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Windows Process.", Log.LogType.Info);
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

                    Log.TraceMessage(Log.Nav.NavIn, "Printing Microsoft Information...", Log.LogType.Info);
                    for (int i = 0; i < 3; i++)
                    {
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }

                    Log.TraceMessage(Log.Nav.NavIn, "Check if Csharp compile works csc...", Log.LogType.Info);
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("csc");

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    if (result.Length > 0)
                        Console.WriteLine(result);

                    if (result.ToLower().StartsWith("microsoft"))
                    {
                        Log.TraceMessage(Log.Nav.NavOut, "Csharp Installed.", Log.LogType.Info);
                        return;
                    }
                    else
                    {
                        string err = cmdProcess.StandardError.ReadLine();
                        err += cmdProcess.StandardError.ReadLine();
                        Console.WriteLine(err);

                        //If Java is not installed there will be an error
                        if (err.Contains("not recognized"))
                        {
                            Log.TraceMessage(Log.Nav.NavIn, "Not Recognized.", Log.LogType.Info);
                            //see if we already installed -

    
                            cmdProcess.StandardInput.AutoFlush = true;

                            //No we didn't install yet.
                            //We will install
                            if (IntPtr.Size == 4)
                            {
                                // 32-bit
                                Log.TraceMessage(Log.Nav.NavIn, "Adding csc to path...", Log.LogType.Info);
                                const string name = "PATH";
                                string pathvar = System.Environment.GetEnvironmentVariable(name);
                                var value = pathvar + @";C:\Windows\Microsoft.NET\Framework\v4.0.30319";
                                var target = EnvironmentVariableTarget.Machine;
                                System.Environment.SetEnvironmentVariable(name, value, target);
                            }
                            else if (IntPtr.Size == 8)
                            {
                                // 64 bit
                                Log.TraceMessage(Log.Nav.NavIn, "Adding csc to path...", Log.LogType.Info);
                                const string name = "PATH";
                                string pathvar = System.Environment.GetEnvironmentVariable(name);
                                var value = pathvar + @";C:\Windows\Microsoft.NET\Framework\v4.0.30319";
                                var target = EnvironmentVariableTarget.Machine;
                                System.Environment.SetEnvironmentVariable(name, value, target);
                            }

                        }
                    }
                }
            }
            else if (isLinux)
            {
                Log.TraceMessage(Log.Nav.NavIn, "Linux Process.", Log.LogType.Info);
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
                        Log.TraceMessage(Log.Nav.NavIn, "Checking for csharp compiler mcs...", Log.LogType.Info);
                        process.StandardInput.WriteLine("mcs --version");

                        string result = process.StandardOutput.ReadLine();

                        if (result.Length > 0)
                            Console.WriteLine(result);

                        if (result.ToLower().StartsWith("mono"))
                        {
                            Log.TraceMessage(Log.Nav.NavOut, "CSharp Installed.", Log.LogType.Info);
                            return;
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
                            Log.TraceMessage(Log.Nav.NavIn, "Installing Mono...", Log.LogType.Info);
                            process.StandardInput.WriteLine("sudo apt-get install mono-devel");
                        }
                    }
                }
            }
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
                    cmdProcess.StandardInput.WriteLine("csc " + file);


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

                        Log.TraceMessage(Log.Nav.NavIn, "Building...", Log.LogType.Info);
                        process.StandardInput.WriteLine("mcs " + file);
                        string result = process.StandardOutput.ReadLine();

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
