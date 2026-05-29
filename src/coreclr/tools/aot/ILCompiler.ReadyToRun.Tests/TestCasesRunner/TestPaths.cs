// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Provides paths to build artifacts needed by the test infrastructure.
/// All paths come from RuntimeHostConfigurationOption items in the csproj.
/// </summary>
internal sealed class TestPaths
{
    private readonly ITestOutputHelper _output;

    public TestPaths(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetRequiredConfig(string key)
    {
        return AppContext.GetData(key) as string
            ?? throw new InvalidOperationException($"Missing RuntimeHostConfigurationOption '{key}'. Was the project built with the correct properties?");
    }

    /// <summary>
    /// Tries to find an existing directory by swapping the build configuration path segment
    /// (Debug, Release, Checked) in the path. Returns the first match, or the original
    /// path if no fallback exists.
    /// </summary>
    private string ProbeConfigFallback(string dir)
    {
        if (Directory.Exists(dir))
            return dir;

        foreach (string fallbackConfig in new[] { "Release", "Checked", "Debug" })
        {
            string fallback = Regex.Replace(
                dir, @"(?<=[/\\])(Debug|Release|Checked)(?=[/\\])", fallbackConfig);
            if (fallback != dir && Directory.Exists(fallback))
            {
                _output.WriteLine($"[TestPaths] '{dir}' not found; falling back to '{fallback}'");
                return fallback;
            }
        }

        return dir;
    }

    /// <summary>
    /// Like <see cref="ProbeConfigFallback"/> but for paths where the configuration name
    /// is embedded in a dot-delimited segment (e.g. <c>linux.x64.Checked</c>).
    /// </summary>
    private string ProbeDottedConfigFallback(string dir)
    {
        if (Directory.Exists(dir))
            return dir;

        foreach (string fallbackConfig in new[] { "Checked", "Release", "Debug" })
        {
            string fallback = Regex.Replace(
                dir, @"\.(Debug|Release|Checked)([/\\])", $".{fallbackConfig}$2");
            if (fallback != dir && Directory.Exists(fallback))
            {
                _output.WriteLine($"[TestPaths] '{dir}' not found; falling back to '{fallback}'");
                return fallback;
            }
        }

        return dir;
    }

    /// <summary>
    /// Path to the crossgen2 output directory (contains the self-contained crossgen2 executable and clrjit).
    /// e.g. artifacts/bin/coreclr/linux.x64.Checked/x64/crossgen2/
    /// Falls back to Checked or Release if the configured path doesn't exist.
    /// </summary>
    public string Crossgen2Dir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.Crossgen2Dir");
            if (!File.Exists(Path.Combine(dir, Crossgen2ExeName)))
            {
                string fallback = ProbeDottedConfigFallback(dir);
                if (File.Exists(Path.Combine(fallback, Crossgen2ExeName)))
                    return fallback;
            }

            return dir;
        }
    }

    private static string Crossgen2ExeName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "crossgen2.exe" : "crossgen2";

    /// <summary>
    /// Path to the self-contained crossgen2 executable (from crossgen2_inbuild).
    /// </summary>
    public string Crossgen2Exe => Path.Combine(Crossgen2Dir, Crossgen2ExeName);

    /// <summary>
    /// Path to the runtime pack managed assemblies directory.
    /// e.g. artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/lib/net11.0/
    /// Falls back to a different build configuration if the path doesn't exist.
    /// </summary>
    public string RuntimePackDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.RuntimePackDir");
            return ProbeConfigFallback(dir);
        }
    }

    /// <summary>
    /// Path to the runtime pack native directory (contains System.Private.CoreLib.dll and native runtime).
    /// e.g. artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/native/
    /// Falls back to a different build configuration if the path doesn't exist.
    /// </summary>
    public string RuntimePackNativeDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.RuntimePackNativeDir");
            return ProbeConfigFallback(dir);
        }
    }

    /// <summary>
    /// Path to the CoreCLR artifacts directory (contains native bits like corerun).
    /// e.g. artifacts/bin/coreclr/linux.x64.Checked/
    /// Falls back to Checked or Release if the configured path doesn't exist.
    /// </summary>
    public string CoreCLRArtifactsDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.CoreCLRArtifactsDir");
            return ProbeDottedConfigFallback(dir);
        }
    }

    public static string TargetArchitecture => GetRequiredConfig("R2RTest.TargetArchitecture");
    public static string TargetOS => GetRequiredConfig("R2RTest.TargetOS");
    public static string Configuration => GetRequiredConfig("R2RTest.Configuration");

    /// <summary>
    /// Path to the reference assembly pack (for Roslyn compilation).
    /// e.g. artifacts/bin/microsoft.netcore.app.ref/ref/net11.0/
    /// </summary>
    public string RefPackDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.RefPackDir");
            if (!Directory.Exists(dir))
            {
                // Try the artifacts/bin/ref/net* fallback
                string artifactsBin = Path.GetFullPath(Path.Combine(CoreCLRArtifactsDir, "..", ".."));
                string refDir = Path.Combine(artifactsBin, "ref");
                if (Directory.Exists(refDir))
                {
                    foreach (string subDir in Directory.GetDirectories(refDir, "net*"))
                    {
                        if (File.Exists(Path.Combine(subDir, "System.Runtime.dll")))
                        {
                            _output.WriteLine($"[TestPaths] '{dir}' not found; falling back to '{subDir}'");
                            return subDir;
                        }
                    }
                }
            }

            return dir;
        }
    }

    /// <summary>
    /// Returns the target triple string for crossgen2 (e.g. "linux-x64").
    /// </summary>
    public static string TargetTriple => $"{TargetOS.ToLowerInvariant()}-{TargetArchitecture.ToLowerInvariant()}";

    /// <summary>
    /// Returns all framework reference assembly paths (*.dll in the runtime pack).
    /// </summary>
    public IEnumerable<string> GetFrameworkReferencePaths()
    {
        if (!Directory.Exists(RuntimePackDir))
            throw new DirectoryNotFoundException($"Runtime pack directory not found: {RuntimePackDir}");

        return Directory.EnumerateFiles(RuntimePackDir, "*.dll");
    }
}
