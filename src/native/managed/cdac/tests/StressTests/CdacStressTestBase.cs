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
/// with a configurable <c>DOTNET_CdacStress</c> value and parses the
/// verification results.
/// </summary>
public abstract class CdacStressTestBase
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Stress sub-checks enabled together with the ALLOC (where) trigger.
    /// Maps directly onto the <c>WHAT</c> byte of <c>DOTNET_CdacStress</c>.
    /// </summary>
    protected enum StressMode
    {
        /// <summary>
        /// 0x101 = ALLOC + GCREFS -- compare cDAC <c>GetStackReferences</c>
        /// vs the runtime's own GC root oracle at every allocation.
        /// </summary>
        GcRefs,

        /// <summary>
        /// 0x201 = ALLOC + ARGITER -- compare cDAC <c>EnumerateArguments</c>-
        /// derived GCRefMap blobs vs runtime <c>ComputeCallRefMap</c> at
        /// every allocation. Independent of GCREFS so the two can be run
        /// from separate test methods on the same build.
        /// </summary>
        ArgIter,
    }

    protected CdacStressTestBase(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Runs the named debuggee under the GCREFS sub-check
    /// (DOTNET_CdacStress=0x101) and returns the parsed results. Convenience
    /// shim around <see cref="RunStressAsync"/>.
    /// </summary>
    internal Task<CdacStressResults> RunGCRefStressAsync(string debuggeeName, int timeoutSeconds = 300)
        => RunStressAsync(debuggeeName, StressMode.GcRefs, timeoutSeconds);

    /// <summary>
    /// Runs the named debuggee under the ARGITER sub-check
    /// (DOTNET_CdacStress=0x201) and returns the parsed results.
    /// </summary>
    internal Task<CdacStressResults> RunArgIterStressAsync(string debuggeeName, int timeoutSeconds = 300)
        => RunStressAsync(debuggeeName, StressMode.ArgIter, timeoutSeconds);

    private async Task<CdacStressResults> RunStressAsync(string debuggeeName, StressMode mode, int timeoutSeconds)
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
        string modeTag = mode == StressMode.GcRefs ? "gcrefs" : "argiter";
        string logFile = Path.Combine(logDir, $"cdac-{modeTag}-{debuggeeName}-{Guid.NewGuid():N}.txt");

        // Mirrors the cdacstress.cpp flag layout: byte 0 = WHERE (0x01 = ALLOC),
        // byte 1 = WHAT (0x100 = GCREFS, 0x200 = ARGITER). Verifies every stress
        // hit; the debuggee's own iteration count keeps test time bounded.
        string flags = mode switch
        {
            StressMode.GcRefs => "0x101",
            StressMode.ArgIter => "0x201",
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        _output.WriteLine($"Running {modeTag} stress: {debuggeeName} (DOTNET_CdacStress={flags})");
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
        psi.Environment["DOTNET_CdacStress"] = flags;
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
            Assert.Fail($"cDAC {modeTag} stress test '{debuggeeName}' timed out after {timeoutSeconds}s");
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
            $"cDAC {modeTag} stress test '{debuggeeName}' exited with {process.ExitCode} (expected 100).\nstdout: {stdout}\nstderr: {stderr}");

        Assert.True(File.Exists(logFile),
            $"cDAC {modeTag} stress results log not created: {logFile}\n" +
            $"  This usually means the cDAC stress framework failed to initialize\n" +
            $"  (e.g. could not load mscordaccore_universal, log directory missing,\n" +
            $"  or DOTNET_CdacStress not honored).\n" +
            $"stdout: {stdout}\nstderr: {stderr}");

        CdacStressResults results = CdacStressResults.Parse(logFile);

        _output.WriteLine($"  results:  {results}");

        return results;
    }

    /// <summary>
    /// Asserts the GCREFS stress run produced a <c>[GC_STATS]</c> summary
    /// with at least one verification, no hard failures, and no deferred
    /// (known-issue) frames.
    /// </summary>
    internal static void AssertAllPassed(CdacStressResults results, string debuggeeName)
    {
        Assert.True(results.AnyGcRefsRecorded,
            $"GCREFS stress test '{debuggeeName}' produced no [GC_STATS] line — " +
            "GCREFS sub-check did not run (DOTNET_CdacStress missing the 0x100 bit, " +
            "or the native harness was not built with cdacstress support).\n" +
            $"Log: {results.LogFilePath}");

        Assert.True(results.TotalVerifications > 0,
            $"GCREFS stress test '{debuggeeName}' verified zero allocation sites — " +
            "the debuggee may not have allocated, or the cdacstress framework " +
            "did not initialize correctly.\n" +
            $"Log: {results.LogFilePath}");

        if (results.Failed > 0 || results.KnownIssues > 0)
        {
            string analysis = results.AnalyzeFailures(maxFailures: 3);
            Assert.Fail(
                $"GCREFS stress test '{debuggeeName}' had {results.Failed} failure(s) " +
                $"and {results.KnownIssues} known issue(s) out of " +
                $"{results.TotalVerifications} verifications.\n" +
                $"Log: {results.LogFilePath}\n\n{analysis}");
        }
    }

    /// <summary>
    /// Asserts the ArgIter stress run produced an <c>[ARG_STATS]</c> summary
    /// with non-zero verifications and zero hard failures.
    /// <see cref="CdacStressResults.ArgIterSkipped"/> is tolerated (and logged
    /// so triage can see it), mirroring how <see cref="AssertAllPassed"/>
    /// tolerates <see cref="CdacStressResults.KnownIssues"/> for GCREFS.
    /// <c>[ARG_SKIP]</c> is emitted by the native harness when either side
    /// returns <c>E_NOTIMPL</c> / <c>S_FALSE</c> -- an acknowledged gap, not a
    /// divergence. <c>[ARG_FAIL]</c> (byte-for-byte mismatch) and
    /// <c>[ARG_ERROR]</c> (unexpected failure HR from cDAC or runtime) still
    /// fail the test.
    /// </summary>
    internal static void AssertAllArgIterPassed(CdacStressResults results, string debuggeeName)
    {
        Assert.True(results.AnyArgIterRecorded,
            $"ArgIter stress test '{debuggeeName}' produced no [ARG_STATS] line — " +
            "ARGITER sub-check did not run (DOTNET_CdacStress missing the 0x200 bit, " +
            "or the native harness was not built with cdacstress support).\n" +
            $"Log: {results.LogFilePath}");

        int total = results.ArgIterPassed + results.ArgIterFailed + results.ArgIterSkipped + results.ArgIterErrors;
        Assert.True(total > 0,
            $"ArgIter stress test '{debuggeeName}' verified zero methods — " +
            "the debuggee may have completed before any alloc trigger fired " +
            "(typical fix: call AllocBurst() at the entry of each test method).\n" +
            $"Log: {results.LogFilePath}");

        if (results.ArgIterFailed > 0 || results.ArgIterErrors > 0)
        {
            // Surface up to a handful of [ARG_FAIL] / [ARG_ERROR] lines so the
            // test failure message is actionable without opening the log.
            const int MaxFailLines = 5;
            string sample = results.ArgIterFailureLines.Count > 0
                ? string.Join('\n', results.ArgIterFailureLines.Take(MaxFailLines))
                : "(no [ARG_FAIL] / [ARG_ERROR] lines captured in log)";
            Assert.Fail(
                $"ArgIter stress test '{debuggeeName}' had " +
                $"{results.ArgIterFailed} fail / {results.ArgIterErrors} error out of " +
                $"{total} verifications ({results.ArgIterSkipped} skip(s) tolerated).\n" +
                $"Log: {results.LogFilePath}\n\n" +
                $"First {Math.Min(MaxFailLines, results.ArgIterFailureLines.Count)} divergence line(s):\n{sample}");
        }
    }

    /// <summary>
    /// Resolve the OS + architecture of the corerun the harness will exec.
    /// Both differ from the testhost process when CORE_ROOT points at a
    /// different layout (typical local case: x64 dotnet driving an x86 or
    /// cross-OS Core_Root). Parses both from the CORE_ROOT path's
    /// <c>&lt;os&gt;.&lt;arch&gt;.&lt;config&gt;</c> segment when present;
    /// falls back to the current process when not (Helix's path layout
    /// doesn't encode arch/os, but matches the testhost there anyway).
    /// </summary>
    protected static void GetTargetPlatform(out OSPlatform os, out Architecture arch)
    {
        string coreRoot = GetCoreRoot();

        // Standard layout: artifacts/tests/coreclr/<os>.<arch>.<config>/Tests/Core_Root
        foreach (string segment in coreRoot.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]))
        {
            string[] parts = segment.Split('.');
            if (parts.Length != 3)
                continue;
            OSPlatform? osMatch = parts[0].ToLowerInvariant() switch
            {
                "windows" => OSPlatform.Windows,
                "linux" => OSPlatform.Linux,
                "osx" => OSPlatform.OSX,
                _ => null,
            };
            Architecture? archMatch = parts[1].ToLowerInvariant() switch
            {
                "x86" => Architecture.X86,
                "x64" => Architecture.X64,
                "arm" => Architecture.Arm,
                "arm64" => Architecture.Arm64,
                _ => null,
            };
            if (osMatch is not null && archMatch is not null)
            {
                os = osMatch.Value;
                arch = archMatch.Value;
                return;
            }
        }

        os = OperatingSystem.IsWindows() ? OSPlatform.Windows
           : OperatingSystem.IsMacOS() ? OSPlatform.OSX
           : OSPlatform.Linux;
        arch = RuntimeInformation.ProcessArchitecture;
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
