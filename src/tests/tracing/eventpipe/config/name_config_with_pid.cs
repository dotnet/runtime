// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class NameConfigWithPid
{
    private const string WaitForInput = "waitforinput";
    static int Main(string[] args)
    {
        if (args.Length == 0)
            Console.WriteLine("No Args");
        else
            Console.WriteLine($"args[0] = `{args[0]}`");

        if (args.Length > 0 && args[0] == WaitForInput)
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

            // Use app directory as temp directory
            string tempDir = AppContext.BaseDirectory;
            string outputPathBaseName = $"eventPipeStream{Thread.CurrentThread.ManagedThreadId}_{(ulong)Stopwatch.GetTimestamp()}";
            string outputPathPattern = Path.Combine(tempDir, outputPathBaseName + "_{pid}_{pid}.nettrace");

            ProcessStartInfo startInfo = new(Environment.ProcessPath,
                TestLibrary.Utilities.IsNativeAot ? [WaitForInput] : [typeof(NameConfigWithPid).Assembly.Location, WaitForInput]);
            process.StartInfo = startInfo;
            using Process processToDispose = process;
            startInfo.InheritedHandles = [];
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;

            startInfo.Environment.Add("DOTNET_EnableEventPipe", "1");
            startInfo.Environment.Add("DOTNET_EventPipeConfig", "Microsoft-Windows-DotNETRuntime:4c14fccbd:4");
            startInfo.Environment.Add("DOTNET_EventPipeOutputPath", outputPathPattern);
            startInfo.Environment.Add("CORE_ROOT", coreRoot);

            Console.WriteLine($"Starting process '{startInfo.FileName}' '{string.Join(" ", startInfo.ArgumentList)}'");
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
            Console.WriteLine($"StdErr ReadToEnd from child process '{process.StandardError.ReadToEnd()}'");
            process.WaitForExitStatus();
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
