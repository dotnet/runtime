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

class OutOfMemoryExceptionTest
{
    const int Pass = 100;
    const int Fail = -1;
    const int TimeoutMilliseconds = 60 * 1000;

    const string AllocateSmallArg = "--allocate-small";
    const string AllocateLargeArg = "--allocate-large";
    // The standard unhandled-exception path ("Unhandled exception. System.OutOfMemoryException...")
    // contains this token. The minimal OOM fail-fast path may only print a short "Out of memory." message.
    // The test validates that some OOM diagnostic is printed rather than just "Aborted" with no context.
    const string ExpectedOomToken = "OutOfMemoryException";
    const string ExpectedMinimalOomToken = "Out of memory.";

    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == AllocateSmallArg)
        {
            // Pre-allocate a flat array for storage.
            object[] storage = new object[8192];
            int idx = 0;
            // We expect ~2048 iterations in the first loop and ~64 iterations in the second.
            try { while (idx < storage.Length) storage[idx++] = GC.AllocateArray<byte>(16 * 1024, pinned: true); } catch (OutOfMemoryException) { }
            try { while (idx < storage.Length) storage[idx++] = GC.AllocateArray<byte>(256, pinned: true); } catch (OutOfMemoryException) { }
            // < 280 bytes free.
            // Use the smallest possible allocation to exhaust the last scraps.
            while (idx < storage.Length) storage[idx++] = GC.AllocateArray<byte>(1, pinned: true);
            return Fail;
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

        string fileName = Environment.ProcessPath;
        string[] arguments = TestLibrary.Utilities.IsNativeAot
            ? [allocateArg]
            : [typeof(OutOfMemoryExceptionTest).Assembly.Location, allocateArg];

        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        // 32 MB GC heap limit (0x2000000): small enough to exhaust quickly but large enough for startup.
        psi.Environment["DOTNET_GCHeapHardLimit"] = "0x2000000";
        psi.Environment["DOTNET_DbgEnableMiniDump"] = "0";

        ProcessTextOutput output;
        try
        {
            output = Process.RunAndCaptureText(psi, TimeSpan.FromMilliseconds(TimeoutMilliseconds));
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Subprocess timed out after {TimeoutMilliseconds / 1000} seconds.");
            return Fail;
        }

        if (output.ExitStatus.ExitCode == 0 || output.ExitStatus.ExitCode == Pass)
        {
            Console.WriteLine($"Subprocess exit code: {output.ExitStatus.ExitCode}");
            Console.WriteLine($"Subprocess stderr: {output.StandardError}");
            Console.WriteLine("Expected a non-success exit code from the OOM subprocess.");
            return Fail;
        }

        string stderr = output.StandardError;

        // Even in the small allocations case, the runtime might still have enough memory to construct
        // an OutOfMemoryException and print the full diagnostic.
        // Either token is acceptable, but at least one should be present to confirm that OOM was the reason for termination.
        if (!(stderr.Contains(ExpectedOomToken) || stderr.Contains(ExpectedMinimalOomToken)))
        {
            Console.WriteLine($"Subprocess exit code: {output.ExitStatus.ExitCode}");
            Console.WriteLine($"Subprocess stderr: {stderr}");
            Console.WriteLine("Expected OOM diagnostic token not found in subprocess stderr.");
            return Fail;
        }

        return Pass;
    }
}
