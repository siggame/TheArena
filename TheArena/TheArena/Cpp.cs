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
                    //Prints Microsoft version and cmd line intro info
                    for(int i=0; i<3; i++)
                    {
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }

                    //Check if g++ installed
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("g++ -v");
                    
                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    
                    //If g++ is not installed there will be an error
                    string err = cmdProcess.StandardError.ReadLine();
                    err += cmdProcess.StandardError.ReadLine();
                    Console.WriteLine(err);
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    if(err.Contains("not recognized"))
                    {
                        IsCommandLineCpp = false;
                        //see if we already installed -

                        //Check if g++ installed
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
                            //No we didn't install yet.
                            //We will install
                            if (IntPtr.Size == 4)
                            {
                                // 32-bit
                                ProcessStartInfo psi = new ProcessStartInfo();
                                psi.Verb = "runas";
                                psi.Arguments = "/s /v /qn /min";
                                psi.CreateNoWindow = true;
                                psi.WindowStyle = ProcessWindowStyle.Hidden;
                                psi.FileName = "StandaloneInstallersWindows64/full.exe";
                                psi.UseShellExecute = false;
                                Process p = Process.Start(psi);
                            }
                            else if (IntPtr.Size == 8)
                            {
                                // 64-bit
                                ProcessStartInfo psi = new ProcessStartInfo();
                                psi.Verb = "runas";
                                psi.Arguments = "/s /v /qn /min";
                                psi.CreateNoWindow = true;
                                psi.WindowStyle = ProcessWindowStyle.Hidden;
                                psi.FileName = "StandaloneInstallersWindows32/full.exe";
                                psi.UseShellExecute = false;
                                Process p = Process.Start(psi);
                            }
                        }
                    }
                    else
                    {
                        // Installed
                    }
                }
            }
            else if(isLinux)
            {
                Console.WriteLine("Here1");
                using (Process process = new Process())
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    Console.WriteLine("Here2");
                    if (process.Start())
                    {
                        Console.WriteLine("Here3");
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
                            IsCommandLineCpp = true;
                        }
                        else
                        {
                            //Need to install
                            process.StandardInput.WriteLine("apt-get install g++");
                        }
                    }
                }
            }
        }
    }
}
