// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Base class for cDAC GC stress tests. Runs a debuggee app under corerun
/// with DOTNET_GCStress=0x24 and parses the verification results.
/// </summary>
public abstract class GCStressTestBase
{
    private readonly ITestOutputHelper _output;

    protected GCStressTestBase(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Runs the named debuggee under GC stress and returns the parsed results.
    /// </summary>
    internal GCStressResults RunGCStress(string debuggeeName, int timeoutSeconds = 300)
    {
        string coreRoot = GetCoreRoot();
        string corerun = GetCoreRunPath(coreRoot);
        string debuggeeDll = GetDebuggeePath(debuggeeName);
        string logFile = Path.Combine(Path.GetTempPath(), $"cdac-gcstress-{debuggeeName}-{Guid.NewGuid():N}.txt");

        _output.WriteLine($"Running GC stress: {debuggeeName}");
        _output.WriteLine($"  corerun:  {corerun}");
        _output.WriteLine($"  debuggee: {debuggeeDll}");
        _output.WriteLine($"  log:      {logFile}");

        var psi = new ProcessStartInfo
        {
            FileName = corerun,
            Arguments = debuggeeDll,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["CORE_ROOT"] = coreRoot;
        psi.Environment["DOTNET_GCStress"] = "0x24";
        psi.Environment["DOTNET_GCStressCdacFailFast"] = "0";
        psi.Environment["DOTNET_GCStressCdacLogFile"] = logFile;
        psi.Environment["DOTNET_GCStressCdacStep"] = "1";
        psi.Environment["DOTNET_ContinueOnAssert"] = "1";

        using var process = Process.Start(psi)!;

        // Read stderr asynchronously to avoid deadlock when both pipe buffers fill.
        string stderr = "";
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr += e.Data + Environment.NewLine;
        };
        process.BeginErrorReadLine();

        string stdout = process.StandardOutput.ReadToEnd();

        bool exited = process.WaitForExit(timeoutSeconds * 1000);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"GC stress test '{debuggeeName}' timed out after {timeoutSeconds}s");
        }

        _output.WriteLine($"  exit code: {process.ExitCode}");
        if (!string.IsNullOrWhiteSpace(stdout))
            _output.WriteLine($"  stdout: {stdout.TrimEnd()}");
        if (!string.IsNullOrWhiteSpace(stderr))
            _output.WriteLine($"  stderr: {stderr.TrimEnd()}");

        Assert.True(process.ExitCode == 100,
            $"GC stress test '{debuggeeName}' exited with {process.ExitCode} (expected 100).\nstdout: {stdout}\nstderr: {stderr}");

        Assert.True(File.Exists(logFile),
            $"GC stress results log not created: {logFile}");

        GCStressResults results = GCStressResults.Parse(logFile);

        _output.WriteLine($"  results:  {results}");

        return results;
    }

    /// <summary>
    /// Asserts that GC stress verification produced 100% pass rate with no failures or skips.
    /// </summary>
    internal static void AssertAllPassed(GCStressResults results, string debuggeeName)
    {
        Assert.True(results.TotalVerifications > 0,
            $"GC stress test '{debuggeeName}' produced zero verifications — " +
            "GCStress may not have triggered or cDAC may not be loaded.");

        if (results.Failed > 0)
        {
            string details = string.Join("\n", results.FailureDetails);
            Assert.Fail(
                $"GC stress test '{debuggeeName}' had {results.Failed} failure(s) " +
                $"out of {results.TotalVerifications} verifications.\n{details}");
        }

        if (results.Skipped > 0)
        {
            string details = string.Join("\n", results.SkipDetails);
            Assert.Fail(
                $"GC stress test '{debuggeeName}' had {results.Skipped} skip(s) " +
                $"out of {results.TotalVerifications} verifications.\n{details}");
        }
    }

    /// <summary>
    /// Asserts that GC stress verification produced a pass rate at or above the given threshold.
    /// A small number of failures is expected due to unimplemented frame scanning for
    /// dynamic method stubs (InvokeStub / PromoteCallerStack).
    /// </summary>
    internal static void AssertHighPassRate(GCStressResults results, string debuggeeName, double minPassRate)
    {
        Assert.True(results.TotalVerifications > 0,
            $"GC stress test '{debuggeeName}' produced zero verifications — " +
            "GCStress may not have triggered or cDAC may not be loaded.");

        double passRate = (double)results.Passed / results.TotalVerifications;
        if (passRate < minPassRate)
        {
            string details = string.Join("\n", results.FailureDetails);
            Assert.Fail(
                $"GC stress test '{debuggeeName}' pass rate {passRate:P2} is below " +
                $"{minPassRate:P1} threshold. {results.Failed} failure(s) out of " +
                $"{results.TotalVerifications} verifications.\n{details}");
        }
    }

    private static string GetCoreRoot()
    {
        // Check environment variable first
        string? coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (!string.IsNullOrEmpty(coreRoot) && Directory.Exists(coreRoot))
            return coreRoot;

        // Default path based on repo layout
        string repoRoot = FindRepoRoot();
        string rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "linux";
        string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        coreRoot = Path.Combine(repoRoot, "artifacts", "tests", "coreclr", $"{rid}.{arch}.Checked", "Tests", "Core_Root");

        if (!Directory.Exists(coreRoot))
            throw new DirectoryNotFoundException(
                $"Core_Root not found at '{coreRoot}'. " +
                "Set the CORE_ROOT environment variable or run 'src/tests/build.cmd Checked generatelayoutonly'.");

        return coreRoot;
    }

    private static string GetCoreRunPath(string coreRoot)
    {
        string exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "corerun.exe" : "corerun";
        string path = Path.Combine(coreRoot, exe);
        Assert.True(File.Exists(path), $"corerun not found at '{path}'");

        return path;
    }

    private static string GetDebuggeePath(string debuggeeName)
    {
        string repoRoot = FindRepoRoot();

        // Debuggees are built to artifacts/bin/GCStressTests/<name>/Release/<tfm>/
        string binDir = Path.Combine(repoRoot, "artifacts", "bin", "GCStressTests", debuggeeName);

        if (!Directory.Exists(binDir))
            throw new DirectoryNotFoundException(
                $"Debuggee '{debuggeeName}' not found at '{binDir}'. Build the GCStressTests project first.");

        // Find the dll in any Release/<tfm> subdirectory
        foreach (string dir in Directory.GetDirectories(binDir, "*", SearchOption.AllDirectories))
        {
            string dll = Path.Combine(dir, $"{debuggeeName}.dll");
            if (File.Exists(dll))
                return dll;
        }

        throw new FileNotFoundException($"Could not find {debuggeeName}.dll under '{binDir}'");
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not find repo root (global.json)");
    }
}
