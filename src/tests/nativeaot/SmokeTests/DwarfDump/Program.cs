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
        ProcessExitStatus status = Process.Run(new ProcessStartInfo(llvmDwarfDumpPath, ["--version"])
        {
            InheritedHandles = [],
        });
        if (status.ExitCode != 0)
        {
            return 3;
        }

        ProcessTextOutput listsResult = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, ["--debug-info", "--recurse-depth=0", "--debug-loc", "--debug-ranges", "--debug-loclists", "--debug-rnglists", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (listsResult.ExitStatus.ExitCode != 0)
        {
            Console.Error.WriteLine(listsResult.StandardError);
            return 4;
        }

        ProcessTextOutput result = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, ["--verify", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        // A verifier that was killed or crashed reports nothing, which would look like a clean il.cpp.
        // It always ends with a summary line, and fails normally because of the native units.
        string output = result.StandardOutput.TrimEnd();
        if (result.ExitStatus.Signal is not null || result.ExitStatus.Canceled ||
            !(output.EndsWith("Errors detected.") || output.EndsWith("No errors.")))
        {
            Console.Error.WriteLine($"llvm-dwarfdump --verify did not complete (exit code {result.ExitStatus.ExitCode}, signal {result.ExitStatus.Signal}).");
            Console.Error.WriteLine(result.StandardError);
            return 5;
        }

        int count = 0;
        bool foundIlCpp = false;
        bool insideIlCpp = false;
        foreach (string rawLine in result.StandardOutput.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.StartsWith("Verifying unit:"))
            {
                if (line.EndsWith("\"il.cpp\""))
                {
                    foundIlCpp = true;
                    insideIlCpp = true;
                }
                else
                {
                    insideIlCpp = false;
                }
            }
            if ((line.Contains("warning:") || line.Contains("error:")) && insideIlCpp)
            {
                count++;
            }
        }

        if (!foundIlCpp)
        {
            Console.Error.WriteLine($"llvm-dwarfdump failed. Cound not find unit named \"il.cpp\".");
            return 10;
        }

        Console.WriteLine($"Found {count} warnings and errors");
        if (count != 0)
        {
            Console.WriteLine("This is likely a result of debug info changes. To see the new output, run the following command:");
            Console.WriteLine("\tllvm-dwarfdump --verify " + Environment.ProcessPath);
            return 10;
        }

        // References to classes are resolved to their complete type, there's no separate declaration. Tools
        // like dsymutil only keep the referenced types, and would drop a complete type that is only found by name.
        ProcessTextOutput typeResult = Process.RunAndCaptureText(new ProcessStartInfo(llvmDwarfDumpPath, ["--debug-info", "--regex", $"--name=.*_{nameof(Program)}$", Environment.ProcessPath])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (typeResult.ExitStatus.ExitCode != 0 ||
            !typeResult.StandardOutput.Contains("DW_TAG_class_type") ||
            typeResult.StandardOutput.Contains("DW_AT_declaration"))
        {
            Console.Error.WriteLine($"Unexpected debug info for {nameof(Program)}:");
            Console.Error.WriteLine(typeResult.StandardOutput);
            Console.Error.WriteLine(typeResult.StandardError);
            return 11;
        }

        return 100;
    }
}
