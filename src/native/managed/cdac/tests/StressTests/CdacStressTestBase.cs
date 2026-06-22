// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Base class for cDAC stress tests. Runs a debuggee app under corerun
/// with DOTNET_CdacStress=0x101 (ALLOC + GCREFS) and parses the verification results.
/// </summary>
public abstract class CdacStressTestBase
{
    private readonly ITestOutputHelper _output;

    protected CdacStressTestBase(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Runs the named debuggee under GC stress and returns the parsed results.
    /// </summary>
    internal async Task<CdacStressResults> RunGCStressAsync(string debuggeeName, int timeoutSeconds = 300)
    {
        string coreRoot = GetCoreRoot();
        string corerun = Path.Combine(coreRoot, OperatingSystem.IsWindows() ? "corerun.exe" : "corerun");
        Assert.True(File.Exists(corerun), $"corerun not found at '{corerun}'");

        string debuggeeDll = GetDebuggeePath(debuggeeName);
        // When running on Helix, write logs into HELIX_WORKITEM_UPLOAD_ROOT so
        // they're uploaded as work-item artifacts and visible via the Helix API.
        // Locally, fall back to the system temp directory.
        string logDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT")
                        ?? Path.GetTempPath();
        string logFile = Path.Combine(logDir, $"cdac-gcstress-{debuggeeName}-{Guid.NewGuid():N}.txt");

        _output.WriteLine($"Running GC stress: {debuggeeName}");
        _output.WriteLine($"  corerun:  {corerun}");
        _output.WriteLine($"  debuggee: {debuggeeDll}");
        _output.WriteLine($"  log:      {logFile}");

        var psi = new ProcessStartInfo
        {
            FileName = corerun,
            Arguments = $"\"{debuggeeDll}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["CORE_ROOT"] = coreRoot;
        // Verifies every stress hit. We rely on the debuggee's own iteration
        // count to keep test time bounded. ALLOC (where=0x01) + GCREFS (what=0x100).
        psi.Environment["DOTNET_CdacStress"] = "0x101";
        psi.Environment["DOTNET_CdacStressFailFast"] = "0";
        psi.Environment["DOTNET_CdacStressLogFile"] = logFile;
        psi.Environment["DOTNET_ContinueOnAssert"] = "1";

        using var process = Process.Start(psi)!;

        // Drain stdout/stderr concurrently with WaitForExit so pipe buffers can't
        // deadlock, and so a timeout cancels all three waits via one CTS.
        // ReadToEndAsync returns only after the pipe is closed at process exit,
        // so we can't lose trailing output the way BeginOutputReadLine + a manual
        // drain can.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"GC stress test '{debuggeeName}' timed out after {timeoutSeconds}s");
            throw;
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        _output.WriteLine($"  exit code: {process.ExitCode}");
        if (!string.IsNullOrWhiteSpace(stdout))
            _output.WriteLine($"  stdout: {stdout.TrimEnd()}");
        if (!string.IsNullOrWhiteSpace(stderr))
            _output.WriteLine($"  stderr: {stderr.TrimEnd()}");

        Assert.True(process.ExitCode == 100,
            $"GC stress test '{debuggeeName}' exited with {process.ExitCode} (expected 100).\nstdout: {stdout}\nstderr: {stderr}");

        Assert.True(File.Exists(logFile),
            $"GC stress results log not created: {logFile}\n" +
            $"  This usually means the cDAC stress framework failed to initialize\n" +
            $"  (e.g. could not load mscordaccore_universal, log directory missing,\n" +
            $"  or DOTNET_CdacStress not honored).\n" +
            $"stdout: {stdout}\nstderr: {stderr}");

        CdacStressResults results = CdacStressResults.Parse(logFile);

        _output.WriteLine($"  results:  {results}");

        return results;
    }

    /// <summary>
    /// Asserts the GC stress run produced at least one verification and had no
    /// hard failures. <see cref="CdacStressResults.KnownIssues"/> is intentionally
    /// tolerated (the native harness emits <c>[KNOWN_ISSUE]</c> for acknowledged
    /// divergences via <c>s_knownIssueCount</c>, separate from
    /// <c>s_failCount</c>) but is logged so regressions in the known-issue
    /// count are visible during triage.
    /// </summary>
    internal static void AssertAllPassed(CdacStressResults results, string debuggeeName)
    {
        Assert.True(results.TotalVerifications > 0,
            $"GC stress test '{debuggeeName}' produced zero verifications — " +
            "the cDAC stress framework may not be enabled (DOTNET_CdacStress unset, " +
            "or coreclr built without CDAC_STRESS).");

        if (results.Failed > 0)
        {
            string analysis = results.AnalyzeFailures(maxFailures: 3);
            Assert.Fail(
                $"GC stress test '{debuggeeName}' had {results.Failed} failure(s) " +
                $"out of {results.TotalVerifications} verifications " +
                $"({results.KnownIssues} known issue(s) tolerated).\n" +
                $"Log: {results.LogFilePath}\n\n{analysis}");
        }
    }

    private static string GetCoreRoot()
    {
        // Explicit override wins (typical when running locally with a custom layout).
        string? coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (!string.IsNullOrEmpty(coreRoot) && Directory.Exists(coreRoot))
            return coreRoot;

        // Helix layout: testhost is unpacked under HELIX_CORRELATION_PAYLOAD and
        // corerun lives in shared/Microsoft.NETCore.App/<version>/. Pick the
        // first version directory; the payload should contain exactly one.
        string? helixPayload = Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD");
        if (!string.IsNullOrEmpty(helixPayload))
        {
            string frameworkRoot = Path.Combine(helixPayload, "shared", "Microsoft.NETCore.App");
            if (Directory.Exists(frameworkRoot))
            {
                string? versionDir = Directory.EnumerateDirectories(frameworkRoot).FirstOrDefault();
                if (versionDir is not null)
                    return versionDir;
            }
        }

        // Local fallback: derive from the repo's standard artifact layout.
        string os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        coreRoot = Path.Combine(FindRepoRoot(), "artifacts", "tests", "coreclr", $"{os}.{arch}.Checked", "Tests", "Core_Root");

        if (!Directory.Exists(coreRoot))
            throw new DirectoryNotFoundException(
                $"Core_Root not found at '{coreRoot}'. " +
                "Set the CORE_ROOT environment variable or run 'src/tests/build.cmd Checked generatelayoutonly'.");

        return coreRoot;
    }

    private static string GetDebuggeePath(string debuggeeName)
    {
        // On Helix, the work-item payload places debuggees as siblings of the
        // test assembly at <payload>/debuggees/<name>/. Locally they're under
        // artifacts/bin/StressTests/<name>/<config>/<tfm>/.
        string? helixPayload = Environment.GetEnvironmentVariable("HELIX_WORKITEM_PAYLOAD");
        string root = !string.IsNullOrEmpty(helixPayload)
            ? Path.Combine(helixPayload, "debuggees", debuggeeName)
            : Path.Combine(FindRepoRoot(), "artifacts", "bin", "StressTests", debuggeeName);

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(
                $"Debuggee '{debuggeeName}' not found at '{root}'. Build the StressTests project first.");

        string dllName = $"{debuggeeName}.dll";
        string? dll = Directory.EnumerateFiles(root, dllName, SearchOption.AllDirectories).FirstOrDefault();
        if (dll is null)
            throw new FileNotFoundException($"Could not find {dllName} under '{root}'");

        return dll;
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
