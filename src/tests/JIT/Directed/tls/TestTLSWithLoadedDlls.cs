// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// This test is verifying that the runtime properly handles the cases where the TLS infra in the runtime is forced
// to use a dynamic resolver. This is done by means of a private config variable to validate the behavior on Linux Arm64
// and a set of multithreaded tasks, that has been known to cause the runtime to crash when this is handled incorrectly.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestTLSWithLoadedDlls
{
    static class TLSWithLoadedDlls
    {
        private const int CountOfLibTlsToLoad = 40;

        static async Task DoLotsOfAsyncWork(int loopCount)
        {
            for (int i = 0; i < loopCount; i++)
            {
                Console.WriteLine("Starting a new batch of tasks...");
                var tasks = Enumerable.Range(1, 100).Select(i => Task.Run(async () =>
                {
                    await Task.Delay(1);
                })).ToArray();

                await Task.WhenAll(tasks);

                Console.WriteLine("Batch of tasks completed. Main loop sleeping for 20 ms...");
                await Task.Delay(20);
            }
        }

        static int Main(string[] args)
        {
            if ((args.Length == 1) && (args[0] == "RunLotsOfTasks"))
            {
                DoLotsOfAsyncWork(100).GetAwaiter().GetResult();
                return 100;
            }

            int CountOfLibTlsToLoad = 60;

            if (OperatingSystem.IsWindows()) // Windows does not have a really long command line length limit, and doesn't have a problem with many TLS using images used
                CountOfLibTlsToLoad = 10;

            StringBuilder arguments = new();

            (string prefix, string suffix) = GetSharedLibraryPrefixSuffix();

            string UseTlsFileName = GetSharedLibraryFileNameForCurrentPlatform("usetls");
            string testDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string UseTlsFilePath = Path.Combine(testDirectory, UseTlsFileName);

            for (int i = 0; i < CountOfLibTlsToLoad; i++)
            {
                string tlsNumberSpecificPath = Path.Combine(testDirectory, i.ToString());
                string finalUseTlsPath = Path.Combine(tlsNumberSpecificPath, prefix + "usetls" + suffix);

                Directory.CreateDirectory(tlsNumberSpecificPath);
                if (!File.Exists(finalUseTlsPath))
                {
                    File.Copy(
                        UseTlsFilePath,
                        finalUseTlsPath);
                }

                arguments.Append(" -l ");
                arguments.Append(finalUseTlsPath);
            }

            arguments.Append(' ');
            arguments.Append(System.Reflection.Assembly.GetExecutingAssembly().Location);
            arguments.Append(" RunLotsOfTasks");

            Process process = new Process();
            process.StartInfo.FileName = GetCorerunPath();
            process.StartInfo.Arguments = arguments.ToString();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.EnvironmentVariables["DOTNET_AssertNotStaticTlsResolver"] = "1";

            Console.WriteLine($"Launching {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }

        private static string GetCorerunPath()
        {
            string corerunName;
            if (OperatingSystem.IsWindows())
            {
                corerunName = "CoreRun.exe";
            }
            else
            {
                corerunName = "corerun";
            }

            return Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), corerunName);
        }

        public static (string, string) GetSharedLibraryPrefixSuffix()
        {
            if (OperatingSystem.IsWindows())
                return (string.Empty, ".dll");

            if (OperatingSystem.IsMacOS())
                return ("lib", ".dylib");

            return ("lib", ".so");
        }

        public static string GetSharedLibraryFileNameForCurrentPlatform(string libraryName)
        {
            (string prefix, string suffix) = GetSharedLibraryPrefixSuffix();
            return prefix + libraryName + suffix;
        }
    }
}
