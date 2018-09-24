using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TheArena
{
    static class Python
    {
        static bool IsCommandLinePython = true;

        public static void InstallPython()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isWindows)
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
                    for (int i = 0; i < 3; i++)
                    {
                        Console.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }

                    //Check if Python 3 is installed
                    cmdProcess.StandardInput.AutoFlush = true;
                    cmdProcess.StandardInput.WriteLine("python -V");

                    //Shows command in use
                    Console.WriteLine(cmdProcess.StandardOutput.ReadLine());

                    string result = cmdProcess.StandardOutput.ReadLine();

                    if (result.Length > 0)
                        Console.WriteLine(result);

                    if (result.ToLower().StartsWith("Python "))
                    {
                        // Python has been installed
                        return;
                    }
                    else
                    {
                        string err = cmdProcess.StandardError.ReadLine();
                        err += cmdProcess.StandardError.ReadLine();
                        Console.WriteLine(err);

                        //If Python is not installed there will be an error
                        if (err.Contains("not recognized"))
                        {
                            IsCommandLinePython = false;
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
                                Process p = Process.Start(psi);
                            }

                        }
                    }
                }
            }
            else if (isLinux)
            {
                using (Process process = new Process())
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                    if (process.Start())
                    {
                        process.StandardInput.WriteLine("python3.7 -V");

                        // Reads the python version if installed
                        string result = process.StandardOutput.ReadLine();

                        if (result.Length > 0)
                            Console.WriteLine(result);

                        if (result.ToLower().StartsWith("Python "))
                        {
                            // Python has been installed
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
                            process.StandardInput.WriteLine("sudo add-apt-repository ppa:deadsnakes/ppa");
                            process.StandardInput.WriteLine("sudo apt-get update");
                            process.StandardInput.WriteLine("sudo apt-get install python3.7");
                        }
                    }
                }
            }
        }
    }
}
