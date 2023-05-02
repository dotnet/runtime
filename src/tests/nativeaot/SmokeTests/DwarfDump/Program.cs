// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

public class Program
{
    public static int Main(string[] args)
    {
        var llvmDwarfDumpPath = Path.Combine(
            Environment.GetEnvironmentVariable("CORE_ROOT"),
            "SuperFileCheck",
            "runtimes",
            RuntimeInformation.RuntimeIdentifier,
            "native",
            "llvm-dwarfdump");

        if (!File.Exists(llvmDwarfDumpPath))
        {
            // Linux-only test
            Console.WriteLine("Could not find llvm-dwarfdump at path: " + llvmDwarfDumpPath);
            return 1;
        }

        Console.WriteLine("Running llvm-dwarfdump");
        var proc = Process.Start(llvmDwarfDumpPath, "--version");

        if (proc is null)
        {
            Console.WriteLine("llvm-dwarfdump could not run");
            return 2;
        }

        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            return 3;
        }

        proc = Process.Start(new ProcessStartInfo
        {
            FileName = llvmDwarfDumpPath,
            Arguments = $"--verify {Environment.ProcessPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        // Just count the number of warnings and errors. There are so many right now that it's not worth enumerating the list
#if DEBUG
        const int MinWarnings = 16500;
        const int MaxWarnings = 20000;
#else
        const int MinWarnings = 12000;
        const int MaxWarnings = 13000;
#endif
        int count = 0;
        string line;
        while ((line = proc.StandardOutput.ReadLine()) != null)
        {
            if (line.Contains("warning:") || line.Contains("error:"))
            {
                count++;
            }
        }
        proc.WaitForExit();
        Console.WriteLine($"Found {count} warnings and errors");
        if (count is not (>= MinWarnings and <= MaxWarnings))
        {
            Console.WriteLine($"Found {count} warnings and errors, expected between {MinWarnings} and {MaxWarnings}");
            Console.WriteLine("This is likely a result of debug info changes. To see the new output, run the following command:");
            Console.WriteLine("\tllvm-dwarfdump --verify " + Environment.ProcessPath);
            return 10;
        }
        return 100;
    }
}
