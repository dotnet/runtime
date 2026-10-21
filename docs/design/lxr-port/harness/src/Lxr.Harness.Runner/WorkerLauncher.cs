// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>Everything one worker invocation produced, including the ways it can fail.</summary>
public sealed class InvocationOutcome
{
    public required string Status { get; init; }

    public required bool MarkerSeen { get; init; }

    public string? Marker { get; init; }

    public int ExitCode { get; init; }

    public double WallSeconds { get; init; }

    public string? ReportPath { get; init; }

    public JsonElement? Report { get; init; }

    public string? DumpPath { get; init; }

    public string? DumpPolicy { get; init; }

    public string StdOut { get; init; } = string.Empty;

    public string StdErr { get; init; } = string.Empty;

    public double? BackgroundLoadPercent { get; init; }

    public bool Valid => Status == RunStatus.Ok && MarkerSeen && Report is not null &&
        Report.Value.TryGetProperty("valid", out JsonElement valid) && valid.GetBoolean();
}

/// <summary>
/// Launches worker processes and enforces the correctness machinery the brief requires around them:
/// an enforced timeout recorded as a failure rather than an absence, bounded crash dumps that never
/// land in the shared temp directory, and a success marker that exit code zero cannot substitute for.
/// </summary>
public sealed class WorkerLauncher
{
    private readonly string _runDirectory;
    private readonly long _dumpBudgetBytes;
    private long _dumpBytesUsed;

    public WorkerLauncher(string runDirectory, long dumpBudgetMb)
    {
        _runDirectory = runDirectory;
        _dumpBudgetBytes = dumpBudgetMb * 1024L * 1024L;
        Directory.CreateDirectory(Path.Combine(_runDirectory, "dumps"));
    }

    public long DumpBytesUsed => Interlocked.Read(ref _dumpBytesUsed);

    public InvocationOutcome Launch(
        HostDescriptor host,
        string workerAssembly,
        CollectorArm arm,
        IReadOnlyList<string> workerArguments,
        IReadOnlyDictionary<string, string> extraProperties,
        IReadOnlyDictionary<string, string> extraEnvironment,
        string cellId,
        int invocation,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(arm);

        string reportPath = Path.Combine(_runDirectory, "reports", $"{cellId}.{invocation}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in arm.RuntimeProperties)
        {
            properties[entry.Key] = entry.Value;
        }

        foreach (KeyValuePair<string, string> entry in extraProperties)
        {
            properties[entry.Key] = entry.Value;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = host.Executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(workerAssembly)!,
        };

        if (host.UsesCoreRunProperties)
        {
            // CoreRun builds the TPA itself from exactly two directories - core_root and
            // CORE_LIBRARIES (corerun.cpp:132-175, which iterates `{ core_libraries, core_root }`),
            // and core_root defaults to the directory holding corerun.exe. That directory contains
            // coreclr.dll and System.Private.CoreLib.dll but none of the framework facades, so a
            // managed assembly compiled against System.Runtime fails to load with
            // FileNotFoundException for 'System.Runtime, Version=11.0.0.0' before Main is reached.
            // Note that CORE_LIBRARIES is a single directory, not a path list, so the harness
            // assemblies and the framework cannot both be delivered through it. The framework is
            // therefore supplied as core_root via --clr-path (corerun.cpp:711, :778) pointing at the
            // built shared-framework directory, whose coreclr.dll is byte-identical to the one beside
            // corerun.exe, and CORE_LIBRARIES carries the harness output directory.
            if (host.FrameworkDirectory is { Length: > 0 } frameworkDirectory)
            {
                startInfo.ArgumentList.Add("--clr-path");
                startInfo.ArgumentList.Add(frameworkDirectory);
            }

            startInfo.Environment["CORE_LIBRARIES"] = Path.GetDirectoryName(workerAssembly)!;

            // corerun.cpp:726 - properties are command line arguments, not runtimeconfig entries.
            foreach (KeyValuePair<string, string> property in properties)
            {
                startInfo.ArgumentList.Add("-p");
                startInfo.ArgumentList.Add($"{property.Key}={property.Value}");
            }

            startInfo.ArgumentList.Add(workerAssembly);
        }
        else
        {
            // The dotnet host reads configProperties from <app>.runtimeconfig.json, which is the
            // public host-property channel the whole design depends on: it parses integers with base
            // zero, i.e. decimal (configuration.cpp:86), whereas the DOTNET_* environment channel
            // parses GC integers as hexadecimal (gcenv.ee.cpp:1338), so delivering a heap count of 16
            // through the environment would silently request twenty-two heaps. Environment variables
            // are therefore used only where a control deliberately wants to override a property, which
            // works because the environment channel is consulted first (gcenv.ee.cpp:1324 and 1351).
            RuntimeConfigWriter.ApplyProperties(workerAssembly, properties);
            startInfo.ArgumentList.Add(workerAssembly);
        }

        foreach (string argument in workerArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(reportPath);
        startInfo.ArgumentList.Add("--arm");
        startInfo.ArgumentList.Add(arm.Id);

        foreach (KeyValuePair<string, string> property in properties)
        {
            startInfo.ArgumentList.Add("--requested");
            startInfo.ArgumentList.Add($"{property.Key}={property.Value}");
        }

        foreach (KeyValuePair<string, string> entry in arm.EnvironmentOverrides)
        {
            startInfo.Environment[entry.Key] = entry.Value;
        }

        foreach (KeyValuePair<string, string> entry in extraEnvironment)
        {
            startInfo.Environment[entry.Key] = entry.Value;
        }

        string dumpPolicy = ConfigureDumps(startInfo, cellId, invocation, out string dumpPattern);

        var loadSampler = new SystemLoadSampler();
        loadSampler.Prime();

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (stdout) { stdout.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (stderr) { stderr.AppendLine(e.Data); } } };

        // A dump left over from an earlier launch on the same path must never be attributed to this
        // one. This is not hypothetical: the control suite reuses one scenario across several
        // demonstrations, and a stale dump from the crash control silently marked a later clean run as
        // crashed, which in turn dropped a perfectly good measurement out of a confidence interval.
        DateTime launchedAtUtc = DateTime.UtcNow.AddSeconds(-1);

        long start = Stopwatch.GetTimestamp();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool exited = process.WaitForExit(timeoutSeconds * 1000);
        string status;
        int exitCode;

        if (!exited)
        {
            // A timeout is a recorded failure, never a missing run. The whole tree is killed because a
            // hung worker can have live child processes and orphaning them would poison later cells.
            //
            // Every plausible failure is swallowed, because this is the one path whose entire purpose is
            // to turn a hang into a recorded result. Kill(bool) is documented to throw AggregateException
            // when part of the tree survives and Win32Exception when TerminateProcess fails - the latter
            // being the ordinary outcome of the very race the catch is here to absorb, since a tree
            // member can exit between enumeration and termination. Letting either escape would convert
            // one hung worker into the loss of an entire matrix run.
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(30_000);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or AggregateException or NotSupportedException)
            {
                // Raced with exit, or part of the tree was already gone. Either way there is nothing
                // left to do but record the timeout.
            }

            // WaitForExit(int) does not wait for the async output readers to drain, so without this the
            // stdout and stderr captured for a timed-out invocation can be truncated - and that output
            // is the primary diagnostic for why it hung.
            try
            {
                process.WaitForExit();
            }
            catch (Exception ex) when (ex is InvalidOperationException or SystemException)
            {
                // The process object may already be gone; the partial output stands.
            }

            status = RunStatus.Timeout;
            exitCode = -1;
        }
        else
        {
            // Flushes the async readers.
            process.WaitForExit();
            exitCode = process.ExitCode;
            status = exitCode == 0 ? RunStatus.Ok : RunStatus.Failed;
        }

        double wallSeconds = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
        string outText = stdout.ToString();
        string errText = stderr.ToString();

        string? dumpPath = CollectDump(dumpPattern, launchedAtUtc);
        if (dumpPath is not null)
        {
            status = RunStatus.Crashed;
        }
        else if (exited && exitCode != 0 && IsFatalExitCode(exitCode))
        {
            status = RunStatus.Crashed;
        }

        string? marker = FindMarker(outText);
        JsonElement? report = ReadReport(reportPath);

        return new InvocationOutcome
        {
            Status = status,
            MarkerSeen = marker is not null,
            Marker = marker,
            ExitCode = exitCode,
            WallSeconds = wallSeconds,
            ReportPath = File.Exists(reportPath) ? reportPath : null,
            Report = report,
            DumpPath = dumpPath,
            DumpPolicy = dumpPolicy,
            StdOut = outText,
            StdErr = errText,
            BackgroundLoadPercent = loadSampler.Sample(),
        };
    }

    /// <summary>
    /// Enables minidumps, bounded and redirected. The bound matters concretely: this machine already
    /// carries about 12 GB of stale dumps in the shared temp directory from earlier work, so the
    /// harness writes only inside its own run directory and stops once the budget is spent.
    /// </summary>
    private string ConfigureDumps(ProcessStartInfo startInfo, string cellId, int invocation, out string dumpPattern)
    {
        string dumpDirectory = Path.Combine(_runDirectory, "dumps");
        dumpPattern = Path.Combine(dumpDirectory, $"{cellId}.{invocation}.");

        if (Interlocked.Read(ref _dumpBytesUsed) >= _dumpBudgetBytes)
        {
            // Deliberately does not set the dump variables at all, so the runtime writes nothing.
            return "suppressed-budget";
        }

        // clrconfigvalues.h:575-578. Type 1 is a normal (small) minidump; the default of 2 "withheap"
        // is what produced the multi-gigabyte files already on this machine.
        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
        startInfo.Environment["DOTNET_DbgMiniDumpType"] = "1";
        startInfo.Environment["DOTNET_DbgMiniDumpName"] = dumpPattern + "%p.dmp";
        startInfo.Environment["DOTNET_CreateDumpDiagnostics"] = "0";
        return "enabled-type1";
    }

    private string? CollectDump(string dumpPattern, DateTime launchedAtUtc)
    {
        string directory = Path.GetDirectoryName(dumpPattern)!;
        string prefix = Path.GetFileName(dumpPattern);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        foreach (string file in Directory.GetFiles(directory, prefix + "*.dmp"))
        {
            var info = new FileInfo(file);
            if (info.LastWriteTimeUtc < launchedAtUtc)
            {
                continue;
            }

            Interlocked.Add(ref _dumpBytesUsed, info.Length);
            return file;
        }

        return null;
    }

    private static bool IsFatalExitCode(int exitCode) =>
        exitCode is unchecked((int)0xC0000005) or unchecked((int)0x80131506) or unchecked((int)0xC0000409) ||
        (exitCode < 0 && ((uint)exitCode & 0xF0000000) == 0xC0000000);

    private static string? FindMarker(string stdout)
    {
        foreach (string line in stdout.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith(WorkerEntryPoint.MarkerPrefix, StringComparison.Ordinal))
            {
                return trimmed[WorkerEntryPoint.MarkerPrefix.Length..];
            }
        }

        return null;
    }

    private static JsonElement? ReadReport(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The <c>DOTNET_*</c> name for a GC host property. Used only by controls that deliberately force
    /// a mismatch, never on the measurement path: the environment channel parses GC integers as
    /// hexadecimal (gcenv.ee.cpp:1338), so <c>DOTNET_GCHeapCount=16</c> requests twenty-two heaps.
    /// </summary>
    public static string? GcEnvironmentName(string property) =>
        property switch
        {
            "System.GC.Server" => "DOTNET_gcServer",
            "System.GC.Concurrent" => "DOTNET_gcConcurrent",
            "System.GC.Name" => "DOTNET_GCName",
            "System.GC.Path" => "DOTNET_GCPath",
            _ => null,
        };

    public static string FormatSeconds(double seconds) => seconds.ToString("F3", CultureInfo.InvariantCulture);
}
