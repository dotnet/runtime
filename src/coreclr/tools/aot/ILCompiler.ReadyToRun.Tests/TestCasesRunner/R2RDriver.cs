// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Known crossgen2 option kinds.
/// </summary>
internal enum Crossgen2AssemblyOption
{
    /// <summary>Enables cross-module inlining for a named assembly (--opt-cross-module AssemblyName).</summary>
    CrossModuleOptimization,
}

internal enum Crossgen2InputKind
{
    InputAssembly,
    Reference,
    InputBubbleReference,
    UnrootedInputFile,
}

internal enum Crossgen2Option
{
    Composite,
    InputBubble,
    ObjectFormat,
    HotColdSplitting,
    Optimize,
    TargetArch,
    TargetOS,
}

internal static class Crossgen2OptionsExtensions
{
    public static string ToArg(this Crossgen2AssemblyOption kind) => kind switch
    {
        Crossgen2AssemblyOption.CrossModuleOptimization => $"--opt-cross-module",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string ToArg(this Crossgen2InputKind kind) => kind switch
    {
        Crossgen2InputKind.InputAssembly => "", // positional argument
        Crossgen2InputKind.Reference => $"--reference",
        Crossgen2InputKind.InputBubbleReference => $"--inputbubbleref",
        Crossgen2InputKind.UnrootedInputFile => $"--unrooted-input-file-paths",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string ToArg(this Crossgen2Option kind) => kind switch
    {
        Crossgen2Option.Composite => $"--composite",
        Crossgen2Option.InputBubble => $"--input-bubble",
        Crossgen2Option.ObjectFormat => $"--object-format",
        Crossgen2Option.HotColdSplitting => $"--hot-cold-splitting",
        Crossgen2Option.Optimize => $"--optimize",
        Crossgen2Option.TargetArch => $"--target-arch",
        Crossgen2Option.TargetOS => $"--target-os",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

/// <summary>
/// Result of a crossgen2 compilation step.
/// </summary>
internal sealed record R2RCompilationResult(
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Invokes crossgen2 out-of-process to produce R2R images.
/// </summary>
internal sealed class R2RDriver
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(2);
    private readonly ITestOutputHelper _output;
    private readonly TestPaths _paths;

    public R2RDriver(ITestOutputHelper output, TestPaths paths)
    {
        _output = output;
        _paths = paths;

        if (!File.Exists(_paths.Crossgen2Exe))
            throw new FileNotFoundException($"crossgen2 executable not found at {_paths.Crossgen2Exe}");
    }

    /// <summary>
    /// Runs crossgen2 with the given arguments.
    /// </summary>
    public R2RCompilationResult Compile(List<string> args)
    {
        var fullArgs = new List<string>(args);
        return RunCrossgen2(fullArgs);
    }

    private R2RCompilationResult RunCrossgen2(List<string> crossgen2Args)
    {
        var psi = new ProcessStartInfo(_paths.Crossgen2Exe, crossgen2Args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        string[] envVarsToStrip = { "DOTNET_GCName", "DOTNET_GCStress", "DOTNET_HeapVerify", "DOTNET_ReadyToRun" };
        foreach (string envVar in envVarsToStrip)
        {
            psi.Environment[envVar] = null;
        }

        string commandLine = $"{_paths.Crossgen2Exe} {string.Join(" ", crossgen2Args)}";
        _output.WriteLine($"  crossgen2 command: {commandLine}");

        using var process = Process.Start(psi)!;

        // Read stdout and stderr concurrently to avoid pipe buffer deadlock.
        // If crossgen2 fills one pipe while we block reading the other, both processes hang.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(ProcessTimeout))
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* best effort */ }
            throw new TimeoutException($"crossgen2 timed out after {ProcessTimeout.TotalMinutes} minutes");
        }

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            _output.WriteLine($"  crossgen2 FAILED (exit code {process.ExitCode})");
            _output.WriteLine(stderr);
        }

        return new R2RCompilationResult(
            process.ExitCode,
            stdout,
            stderr);
    }
}
