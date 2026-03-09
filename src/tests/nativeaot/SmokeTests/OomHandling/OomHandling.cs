// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test verifies that an out-of-memory condition in a NativeAOT process
// produces a diagnostic message on stderr before the process terminates.
//
// The test spawns itself as a subprocess with a small GC heap limit set via
// DOTNET_GCHeapHardLimit so that the subprocess reliably runs out of memory.
// The outer process then validates that the subprocess wrote the expected
// OOM message to its standard error stream.

using System;
using System.Collections.Generic;
using System.Diagnostics;

class OomHandlingTest
{
    const int Pass = 100;
    const int Fail = -1;

    const string AllocateArg = "--allocate";
    // Both the minimal OOM fail-fast path ("Process is terminating due to OutOfMemoryException.")
    // and the standard unhandled-exception path ("Unhandled exception. System.OutOfMemoryException...")
    // contain this token. The test validates that some OOM diagnostic is printed rather than
    // just "Aborted" with no context.
    const string ExpectedToken = "OutOfMemoryException";

    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == AllocateArg)
        {
            // Subprocess mode: allocate until OOM is triggered.
            List<byte[]> list = new();
            while (true)
                list.Add(new byte[128 * 1024]);
        }

        // Controller mode: launch a subprocess with a GC heap limit and verify its output.
        string? processPath = Environment.ProcessPath;
        if (processPath == null)
        {
            Console.WriteLine("ProcessPath is null, skipping test.");
            return Pass;
        }

        var psi = new ProcessStartInfo(processPath, AllocateArg)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // A 20 MB GC heap limit is small enough to exhaust quickly but large enough for startup.
        psi.Environment["DOTNET_GCHeapHardLimit"] = "20000000";

        using Process? p = Process.Start(psi);
        if (p == null)
        {
            Console.WriteLine("Failed to start subprocess.");
            return Fail;
        }

        // Read stderr before waiting to avoid deadlock.
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        Console.WriteLine($"Subprocess exit code: {p.ExitCode}");
        Console.WriteLine($"Subprocess stderr: {stderr}");

        if (p.ExitCode == 0 || p.ExitCode == Pass)
        {
            Console.WriteLine("Expected a non-success exit code from the OOM subprocess.");
            return Fail;
        }

        if (!stderr.Contains(ExpectedToken))
        {
            Console.WriteLine($"Expected stderr to contain: {ExpectedToken}");
            return Fail;
        }

        return Pass;
    }
}
