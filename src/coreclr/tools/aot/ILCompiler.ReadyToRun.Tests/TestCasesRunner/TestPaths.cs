// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Provides paths to build artifacts needed by the test infrastructure.
/// All paths come from RuntimeHostConfigurationOption items in the csproj.
/// </summary>
internal static class TestPaths
{
    private static string GetRequiredConfig(string key)
    {
        return AppContext.GetData(key) as string
            ?? throw new InvalidOperationException($"Missing RuntimeHostConfigurationOption '{key}'. Was the project built with the correct properties?");
    }

    /// <summary>
    /// Path to the crossgen2 output directory (contains crossgen2.dll and clrjit).
    /// e.g. artifacts/bin/coreclr/linux.x64.Checked/crossgen2/
    /// Falls back to Checked or Release if Debug path doesn't exist.
    /// </summary>
    public static string Crossgen2Dir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.Crossgen2Dir");
            if (!File.Exists(Path.Combine(dir, "crossgen2.dll")))
            {
                // Try Checked and Release fallbacks since crossgen2 may be built in a different config
                foreach (string fallbackConfig in new[] { "Checked", "Release", "Debug" })
                {
                    string fallback = Regex.Replace(
                        dir, @"\.(Debug|Release|Checked)[/\\]", $".{fallbackConfig}/");
                    if (File.Exists(Path.Combine(fallback, "crossgen2.dll")))
                        return fallback;
                }
            }

            return dir;
        }
    }

    /// <summary>
    /// Path to the native crossgen2 executable (apphost).
    /// </summary>
    public static string Crossgen2Exe
    {
        get
        {
            string exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "crossgen2.exe" : "crossgen2";
            return Path.Combine(Crossgen2Dir, exe);
        }
    }

    /// <summary>
    /// Path to the runtime pack managed assemblies directory.
    /// e.g. artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/lib/net11.0/
    /// Falls back to Release if Debug path doesn't exist (libs are typically built Release).
    /// </summary>
    public static string RuntimePackDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.RuntimePackDir");
            if (!Directory.Exists(dir) && dir.Contains("Debug"))
            {
                string releaseFallback = dir.Replace("Debug", "Release");
                if (Directory.Exists(releaseFallback))
                    return releaseFallback;
            }

            return dir;
        }
    }

    /// <summary>
    /// Path to the CoreCLR artifacts directory (contains native bits like corerun).
    /// e.g. artifacts/bin/coreclr/linux.x64.Checked/
    /// Falls back to Checked or Release if Debug path doesn't exist.
    /// </summary>
    public static string CoreCLRArtifactsDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.CoreCLRArtifactsDir");
            if (!Directory.Exists(dir))
            {
                foreach (string fallbackConfig in new[] { "Checked", "Release", "Debug" })
                {
                    string fallback = Regex.Replace(
                        dir, @"\.(Debug|Release|Checked)(/|\\|$)", $".{fallbackConfig}$2");
                    if (Directory.Exists(fallback))
                        return fallback;
                }
            }

            return dir;
        }
    }

    public static string TargetArchitecture => GetRequiredConfig("R2RTest.TargetArchitecture");
    public static string TargetOS => GetRequiredConfig("R2RTest.TargetOS");
    public static string Configuration => GetRequiredConfig("R2RTest.Configuration");

    /// <summary>
    /// Path to the reference assembly pack (for Roslyn compilation).
    /// e.g. artifacts/bin/microsoft.netcore.app.ref/ref/net11.0/
    /// </summary>
    public static string RefPackDir
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
                            return subDir;
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
    /// Path to the testhost shared framework directory containing corerun,
    /// libcoreclr, all managed framework DLLs, and native shims.
    /// e.g. artifacts/bin/testhost/net11.0-linux-Release-x64/shared/Microsoft.NETCore.App/11.0.0/
    /// </summary>
    public static string TestHostSharedFrameworkDir
    {
        get
        {
            string dir = GetRequiredConfig("R2RTest.TestHostSharedFrameworkDir");
            if (!Directory.Exists(dir))
            {
                foreach (string fallbackConfig in new[] { "Release", "Debug", "Checked" })
                {
                    string fallback = Regex.Replace(
                        dir, @"-(Debug|Release|Checked)-", $"-{fallbackConfig}-");
                    if (Directory.Exists(fallback))
                        return fallback;
                }
            }

            return dir;
        }
    }

    /// <summary>
    /// Path to the corerun executable inside the testhost shared framework.
    /// </summary>
    public static string CorerunPath
    {
        get
        {
            string exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "corerun.exe" : "corerun";
            return Path.Combine(TestHostSharedFrameworkDir, exe);
        }
    }

    /// <summary>
    /// Returns all framework reference assembly paths (*.dll in the runtime pack).
    /// </summary>
    public static IEnumerable<string> GetFrameworkReferencePaths()
    {
        if (!Directory.Exists(RuntimePackDir))
            throw new DirectoryNotFoundException($"Runtime pack directory not found: {RuntimePackDir}");

        return Directory.EnumerateFiles(RuntimePackDir, "*.dll");
    }
}
