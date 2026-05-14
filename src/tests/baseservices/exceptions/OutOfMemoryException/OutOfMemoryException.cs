// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test verifies that an out-of-memory condition produces a diagnostic
// message on stderr before the process terminates.
//
// The test spawns itself as a subprocess with a small GC heap limit set via
// DOTNET_GCHeapHardLimit so that the subprocess reliably runs out of memory.
// The outer process then validates that the subprocess wrote the expected
// OOM message to its standard error stream.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

class OutOfMemoryExceptionTest
{
    const int Pass = 100;
    const int Fail = -1;
    const int TimeoutMilliseconds = 60 * 1000;

    const string AllocateSmallArg = "--allocate-small";
    const string AllocateLargeArg = "--allocate-large";
    // Both the minimal OOM fail-fast path ("Process terminated. System.OutOfMemoryException")
    // and the standard unhandled-exception path ("Unhandled exception. System.OutOfMemoryException...")
    // contain this token. The test validates that some OOM diagnostic is printed rather than
    // just "Aborted" with no context.
    const string ExpectedToken = "OutOfMemoryException";

    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == AllocateSmallArg)
        {
            // Subprocess mode: allocate until OOM is triggered.
            // Phase 1: fill quickly with large blocks to use most of the heap.
            // Phase 2: exhaust remaining scraps with small allocations so that
            // virtually no memory is left when OOM is finally thrown.
            var list = new List<object>();
            try { while (true) list.Add(new byte[16 * 1024]); } catch (OutOfMemoryException) { }
            while (true) list.Add(new object());
        }

        if (args.Length > 0 && args[0] == AllocateLargeArg)
        {
            // Subprocess mode: allocate 128 KB chunks until OOM is triggered.
            // This leaves some free memory when OOM fires, exercising the code
            // path where GetRuntimeException may allocate a new OutOfMemoryException.
            var list = new List<byte[]>();
            while (true) list.Add(new byte[128 * 1024]);
        }

        // Controller mode: launch subprocesses with a GC heap limit and verify their output.
        int result = RunSubprocess(AllocateSmallArg, "small allocations");
        if (result != Pass)
            return result;

        return RunSubprocess(AllocateLargeArg, "large allocations");
    }

    static int RunSubprocess(string allocateArg, string description)
    {
        Console.WriteLine($"Testing OOM with {description}...");

        string fileName = Process.GetCurrentProcess().MainModule.FileName;
        string arguments = TestLibrary.Utilities.IsNativeAot
            ? allocateArg
            : $"{typeof(OutOfMemoryExceptionTest).Assembly.Location} {allocateArg}";

        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // 32 MB GC heap limit (hex): small enough to exhaust quickly but large enough for startup.
        psi.Environment["DOTNET_GCHeapHardLimit"] = "0x2000000";
        psi.Environment["DOTNET_DbgEnableMiniDump"] = "0";

        using Process? p = Process.Start(psi);
        if (p is null)
        {
            Console.WriteLine("Failed to start subprocess.");
            return Fail;
        }

        // Read stderr asynchronously so that WaitForExit can enforce the timeout.
        // A synchronous ReadToEnd() would block until the child exits, defeating the timeout.
        Task<string> stderrTask = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(TimeoutMilliseconds))
        {
            p.Kill(true);
            p.WaitForExit();
            _ = stderrTask.GetAwaiter().GetResult();
            Console.WriteLine($"Subprocess timed out after {TimeoutMilliseconds / 1000} seconds.");
            return Fail;
        }
        string stderr = stderrTask.GetAwaiter().GetResult();

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
