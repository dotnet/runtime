// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lxr.Harness.Core;

/// <summary>
/// Ties a measurement to the exact runtime binary that produced it.
///
/// <para>The brief requires "the commit hash of the built runtime, not just the SDK version", because
/// a number that cannot be attributed to a commit cannot be compared to a later one. Two independent
/// identifiers are captured: the informational version of <c>System.Private.CoreLib</c>, which carries
/// a <c>+&lt;sha&gt;</c> suffix from the build, and the SHA-256 of the loaded <c>coreclr.dll</c>, which
/// is exact even for a local build whose source version metadata is a placeholder.</para>
/// </summary>
public sealed class RuntimeBuildIdentity
{
    public required string FrameworkDescription { get; init; }

    public required string EnvironmentVersion { get; init; }

    public required string RuntimeIdentifier { get; init; }

    public string? CoreLibInformationalVersion { get; init; }

    /// <summary>The <c>+&lt;sha&gt;</c> suffix of the CoreLib informational version, when present.</summary>
    public string? CommitSha { get; init; }

    public string? CoreLibPath { get; init; }

    public string? CoreClrPath { get; init; }

    public string? CoreClrFileVersion { get; init; }

    public string? CoreClrSha256 { get; init; }

    public string? ProcessPath { get; init; }

    public required bool IsCoreRunHost { get; init; }

    public static RuntimeBuildIdentity Capture()
    {
        Assembly coreLib = typeof(object).Assembly;
        string? informational = coreLib.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string? sha = null;
        if (informational is not null)
        {
            int plus = informational.IndexOf('+', StringComparison.Ordinal);
            if (plus >= 0 && plus + 1 < informational.Length)
            {
                sha = informational[(plus + 1)..];
            }
        }

        string? coreLibPath = string.IsNullOrEmpty(coreLib.Location) ? null : coreLib.Location;
        string? coreClrPath = FindLoadedCoreClr();
        string? processPath = Environment.ProcessPath;

        return new RuntimeBuildIdentity
        {
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            EnvironmentVersion = Environment.Version.ToString(),
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            CoreLibInformationalVersion = informational,
            CommitSha = sha,
            CoreLibPath = coreLibPath,
            CoreClrPath = coreClrPath,
            CoreClrFileVersion = coreClrPath is null ? null : SafeFileVersion(coreClrPath),
            CoreClrSha256 = coreClrPath is null ? null : SafeSha256(coreClrPath),
            ProcessPath = processPath,
            IsCoreRunHost = processPath is not null &&
                Path.GetFileNameWithoutExtension(processPath).Equals("corerun", StringComparison.OrdinalIgnoreCase),
        };
    }

    private static string? FindLoadedCoreClr()
    {
        foreach (System.Diagnostics.ProcessModule module in System.Diagnostics.Process.GetCurrentProcess().Modules)
        {
            string name = Path.GetFileName(module.FileName);
            if (name.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("libcoreclr.so", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("libcoreclr.dylib", StringComparison.OrdinalIgnoreCase))
            {
                return module.FileName;
            }
        }

        return null;
    }

    private static string? SafeFileVersion(string path)
    {
        try
        {
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? SafeSha256(string path)
    {
        try
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>
/// Records what the measurement was taken on. This machine is a shared Azure VM rather than a
/// quiesced benchmarking host, so the environment is part of the result, and a run taken under
/// visible background load is marked suspect rather than published as a clean number.
/// </summary>
public sealed class MachineInfo
{
    public string? ProcessorName { get; init; }

    public required int LogicalCores { get; init; }

    public int? PhysicalCores { get; init; }

    public required long TotalMemoryBytes { get; init; }

    public string? PowerPlan { get; init; }

    public string? SystemModel { get; init; }

    /// <summary>
    /// Whether the host is virtualized, or null when it was not determined.
    /// </summary>
    /// <remarks>
    /// Nullable deliberately. Virtualization is inferred from the system model, and the model is an
    /// operator-supplied fact the worker cannot read for itself. When it is absent, 'false' is a
    /// definite claim that the host is bare metal - which is the boolean form of publishing 0 for a
    /// metric nobody measured, and on this project's own hardware it is also wrong: every run made
    /// without --machine-model would assert 'not virtualized' from a VM.
    /// </remarks>
    public bool? Virtualized { get; init; }

    public required string OsDescription { get; init; }

    public required int ProcessCount { get; init; }

    /// <summary>Resolution of <see cref="System.Diagnostics.Stopwatch"/>, measured rather than assumed.</summary>
    public required double TimerResolutionNs { get; init; }

    public static MachineInfo Capture(string? processorName, int? physicalCores, string? powerPlan, string? systemModel)
    {
        bool? virtualized = systemModel is null
            ? null
            : systemModel.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
              systemModel.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
              systemModel.Contains("KVM", StringComparison.OrdinalIgnoreCase);

        return new MachineInfo
        {
            ProcessorName = processorName ?? ReadProcessorNameFromRegistry(),
            LogicalCores = Environment.ProcessorCount,
            PhysicalCores = physicalCores,
            TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            PowerPlan = powerPlan,
            SystemModel = systemModel,
            Virtualized = virtualized,
            OsDescription = RuntimeInformation.OSDescription,
            ProcessCount = SafeProcessCount(),
            TimerResolutionNs = Clock.MeasureResolutionNanoseconds(),
        };
    }

    private static int SafeProcessCount()
    {
        try
        {
            return System.Diagnostics.Process.GetProcesses().Length;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    private static string? ReadProcessorNameFromRegistry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return key?.GetValue("ProcessorNameString") as string;
        }
        catch (System.Security.SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>
/// Samples whole-system CPU utilisation so that a run competing with unrelated work on this shared
/// host can be identified. Uses <c>GetSystemTimes</c> directly: performance counters are not in the
/// shared framework and an external dependency would not run on the <c>corerun</c> host.
/// </summary>
public sealed class SystemLoadSampler
{
    private ulong _idle;
    private ulong _kernel;
    private ulong _user;
    private bool _primed;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

    public void Prime()
    {
        if (OperatingSystem.IsWindows() && GetSystemTimes(out _idle, out _kernel, out _user))
        {
            _primed = true;
        }
    }

    /// <summary>Whole-system busy percentage since <see cref="Prime"/>, or null when unavailable.</summary>
    public double? Sample()
    {
        if (!_primed || !OperatingSystem.IsWindows() || !GetSystemTimes(out ulong idle, out ulong kernel, out ulong user))
        {
            return null;
        }

        // kernelTime includes idleTime, so total elapsed is kernel + user and busy is total - idle.
        double idleDelta = idle - _idle;
        double totalDelta = (kernel - _kernel) + (double)(user - _user);
        _idle = idle;
        _kernel = kernel;
        _user = user;

        return totalDelta <= 0 ? null : Math.Clamp(100.0 * (totalDelta - idleDelta) / totalDelta, 0.0, 100.0);
    }
}

internal static class Clock
{
    /// <summary>
    /// Measures the smallest non-zero interval the stopwatch can report. P0.1 hit a timer-jitter floor
    /// of roughly 0.12 ms on the reference side; the .NET floor is different and is measured here
    /// rather than assumed, because a latency percentile is only meaningful above it.
    /// </summary>
    public static double MeasureResolutionNanoseconds()
    {
        double smallest = double.MaxValue;
        for (int i = 0; i < 64; i++)
        {
            long a = System.Diagnostics.Stopwatch.GetTimestamp();
            long b;
            do
            {
                b = System.Diagnostics.Stopwatch.GetTimestamp();
            }
            while (b == a);

            smallest = Math.Min(smallest, b - a);
        }

        return smallest * 1_000_000_000.0 / System.Diagnostics.Stopwatch.Frequency;
    }

    public static string ToInvariant(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
