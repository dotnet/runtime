// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

/// <summary>
/// This class is used for creating a test that has an entry point
/// that is not the test itself. For example a test that starts from
/// a native exe instead of a managed entry point.
/// </summary>
public class Program
{
    static int Main(string[] noArgs)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Console.WriteLine($"Exe launcher only supported on Windows environments...");
            return 100;
        }

#if BLOCK_WINDOWS_NANO
        // Not supported on Windows Nano
        if (TestLibrary.Utilities.IsWindowsNanoServer)
        {
            return 100;
        }
#endif

        string workingDir = Environment.CurrentDirectory;
        Console.WriteLine($"Searching for exe to launch in {workingDir}...");

        Assembly thisAssem = Assembly.GetEntryAssembly();
        string startExe = string.Empty;
        foreach (string exeMaybe in Directory.EnumerateFiles(workingDir, "*.exe"))
        {
            // This entry point is _not_ an option
            if (exeMaybe.Equals(thisAssem.Location, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            startExe = exeMaybe;
            break;
        }

        if (string.IsNullOrEmpty(startExe))
        {
            throw new Exception("Unable to find start EXE");
        }

        var startInfo = new ProcessStartInfo()
        {
            FileName = startExe,

            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine($"Launching '{startExe}'...");
        using (Process p = Process.Start(startInfo))
        {
            p.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
            p.BeginOutputReadLine();

            p.ErrorDataReceived += (_, args) => Console.Error.WriteLine(args.Data);
            p.BeginErrorReadLine();

            p.WaitForExit();
            return p.ExitCode;
        }
    }
}