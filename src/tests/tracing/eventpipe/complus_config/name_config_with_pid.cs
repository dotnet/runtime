// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class NameConfigWithPid
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
            Console.WriteLine("No Args");
        else
            Console.WriteLine($"args[0] = `{args[0]}`");

        if (args.Length > 0 && args[0] == "waitforinput")
        {
            Console.Error.WriteLine("WaitingForInput in ErrorStream");
            Console.WriteLine("WaitingForInput");
            Console.Error.Flush();
            Console.Out.Flush();
            Console.ReadLine();
            return 100;
        }
        else
        {
            Process process = null;
            try
            {
                process = new Process();
            }
            catch (PlatformNotSupportedException)
            {
                // For platforms that do not support launching child processes, simply succeed the test
                return 100;
            }

            string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
            string corerun = Path.Combine(coreRoot, "corerun");
            if (OperatingSystem.IsWindows())
                corerun = corerun + ".exe";

            // Use dll directory as temp directory
            string tempDir = Path.GetDirectoryName(typeof(NameConfigWithPid).Assembly.Location);
            string outputPathBaseName = $"eventPipeStream{Thread.CurrentThread.ManagedThreadId}_{(ulong)Stopwatch.GetTimestamp()}";
            string outputPathPattern = Path.Combine(tempDir, outputPathBaseName + "_{pid}_{pid}.nettrace");

            process.StartInfo.FileName = corerun;
            process.StartInfo.Arguments = typeof(NameConfigWithPid).Assembly.Location + " waitforinput";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.Environment.Add("COMPlus_EnableEventPipe", "1");
            process.StartInfo.Environment.Add("COMPlus_EventPipeConfig", "Microsoft-Windows-DotNETRuntime:4c14fccbd:4");
            process.StartInfo.Environment.Add("COMPlus_EventPipeOutputPath", outputPathPattern);
            process.StartInfo.Environment.Add("CORE_ROOT", coreRoot);

            Console.WriteLine($"Starting process '{process.StartInfo.FileName}' '{process.StartInfo.Arguments}'");
            Console.Out.Flush();
            process.Start();

            string readFromTargetProcess = process.StandardError.ReadLine(); 
            Console.WriteLine($"Readline '{readFromTargetProcess}'");
            if (readFromTargetProcess != "WaitingForInput in ErrorStream")
            {
                Console.WriteLine($"Child process terminating");
                Thread.Sleep(10000);
                process.Kill();
                Console.WriteLine($"Child process terminated");
            }
            Console.Out.Flush();
            uint pid = (uint)process.Id;
            string expectedPath = outputPathPattern.Replace("{pid}", pid.ToString());

            process.StandardInput.WriteLine("input");
            process.StandardInput.Flush();
            process.WaitForExit();

            Console.WriteLine($"StdErr ReadToEnd from child process '{process.StandardError.ReadToEnd()}'");
            if (!File.Exists(expectedPath))
            {
                Console.WriteLine($"{expectedPath} not found");
                return 1;
            }
            else
            {
                Console.WriteLine($"{expectedPath} found");
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        if (File.Exists(expectedPath))
                            File.Delete(expectedPath);
                        return 100;
                    }
                    catch { }
                    Thread.Sleep(1000);
                }
                Console.WriteLine($"Unable to delete {expectedPath}");
                return 2; 
            }
        }
    }
}
