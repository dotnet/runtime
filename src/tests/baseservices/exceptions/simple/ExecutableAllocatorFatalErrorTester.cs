// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class ExecutableAllocatorFatalErrorTester
{
    private const int ExecutionEngineExceptionHResult = unchecked((int)0x80131506);
    private const int ProcessTimeoutMilliseconds = 30_000;

    [Fact]
    public static void FatalErrorWhileGCWaitsForExecutableAllocatorTerminates()
    {
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(coreRoot, "corerun.exe"),
            Arguments = "ExecutableAllocatorFatalError.dll",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.Environment["DOTNET_EnableWriteXorExecute"] = "1";
        // Checked builds also diagnose the pre-existing JIT-lock-to-lazy-debugger-attach ordering.
        // Continue past that diagnostic so this test can distinguish process termination from the GC deadlock.
        startInfo.Environment["DOTNET_ContinueOnAssert"] = "1";
        startInfo.Environment["DOTNET_gcConcurrent"] = "0";
        startInfo.Environment["DOTNET_gcServer"] = "0";
        startInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
        startInfo.Environment.Remove("DOTNET_EnableCrashReport");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the fatal-error process.");
        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(ProcessTimeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            string timedOutOutput = standardOutputTask.GetAwaiter().GetResult();
            string timedOutError = standardErrorTask.GetAwaiter().GetResult();
            throw new TimeoutException($"The fatal-error process deadlocked.{Environment.NewLine}{timedOutOutput}{timedOutError}");
        }

        string standardOutput = standardOutputTask.GetAwaiter().GetResult();
        string standardError = standardErrorTask.GetAwaiter().GetResult();
        Assert.True(
            process.ExitCode == ExecutionEngineExceptionHResult,
            $"Exit code = {process.ExitCode}, expected {ExecutionEngineExceptionHResult}.{Environment.NewLine}{standardOutput}{standardError}");
    }
}
