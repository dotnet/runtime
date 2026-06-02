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
            // Subprocess mode: allocate until OOM is triggered.
            // Phase 1: fill quickly with large blocks to use most of the heap.
            // Phase 2: exhaust remaining scraps with small allocations so that
            // virtually no memory is left when OOM is finally thrown.
            var list = new List<object>();
            try { while (true) list.Add(new byte[16 * 1024]); } catch (OutOfMemoryException) { }
            // If we keep adding elements to the list, it's possible that the list's
            // internal array fails when trying a big allocation to grow.
            // Instead, we create a long chain of objects that will fail with OOM when
            // trying to allocate the next one.
            object a = null;
            for (;;) a = new object[] { a };
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

        Console.WriteLine($"Subprocess exit code: {output.ExitStatus.ExitCode}");
        Console.WriteLine($"Subprocess stderr: {output.StandardError}");

        if (output.ExitStatus.ExitCode == 0 || output.ExitStatus.ExitCode == Pass)
        {
            Console.WriteLine("Expected a non-success exit code from the OOM subprocess.");
            return Fail;
        }

        string stderr = output.StandardError;

        if (allocateArg == AllocateSmallArg && !stderr.Contains(ExpectedMinimalOomToken))
        {
            // This test should exercise the minimal OOM fail-fast path.
            Console.WriteLine($"Expected minimal OOM diagnostic token not found in subprocess stderr.");
            return Fail;
        }

        // In the general case, we expect either a message containing "OutOfMemoryException" or the minimal OOM message.
        if (!(stderr.Contains(ExpectedOomToken) || stderr.Contains(ExpectedMinimalOomToken)))
        {
            Console.WriteLine($"Expected OOM diagnostic token not found in subprocess stderr.");
            return Fail;
        }

        return Pass;
    }
}
