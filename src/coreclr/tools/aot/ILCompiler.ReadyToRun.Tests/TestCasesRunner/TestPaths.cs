// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Provides paths to build artifacts needed by the test infrastructure, and the target and host
/// predicates that gate test cases.
/// </summary>
internal static class TestPaths
{
    private static string GetRequiredConfig(string key)
    {
        return AppContext.GetData(key) as string
            ?? throw new InvalidOperationException($"Missing RuntimeHostConfigurationOption '{key}'. Was the project built with the correct properties?");
    }

    private static string Crossgen2ExeName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "crossgen2.exe" : "crossgen2";

    public static string SystemPrivateCoreLibPath => Path.Combine(LibrariesDir, "System.Private.CoreLib.dll");

    public static string TargetOS => GetRequiredConfig("R2RTest.TargetOS");
    public static string TargetArchitecture => GetRequiredConfig("R2RTest.TargetArchitecture");

    private static string TargetRid => $"{TargetOS}-{TargetArchitecture}";

    public static bool IsWasmTarget => TargetArchitecture == "wasm";

    public static bool IsNotWasmTarget => !IsWasmTarget;

    public static bool IsArmTarget => TargetArchitecture is "arm" or "armel";

    public static bool IsWindowsTarget => TargetOS is "windows" or "win";

    public static bool IsIosArm64Target => TargetOS is "ios" && TargetArchitecture is "arm64";

    public static bool IsWindowsHost => OperatingSystem.IsWindows();

    /// <summary>
    /// Path to the crossgen2 that compiles for this build's target.
    /// </summary>
    public static string Crossgen2Exe
    {
        get
        {
            string exe = Path.Combine(AppContext.BaseDirectory, "crossgen2", Crossgen2ExeName);
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException(
                    $"No crossgen2 staged for {TargetRid} at '{exe}'. Build the clr and libs subsets for " +
                    $"{TargetRid}, then rebuild this test project.");
            }

            return exe;
        }
    }

    /// <summary>
    /// Directory holding the shared framework IL assemblies crossgen2 compiles against.
    /// </summary>
    public static string LibrariesDir
    {
        get
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "libraries");
            if (!Directory.Exists(dir))
            {
                throw new DirectoryNotFoundException(
                    $"No shared framework assemblies staged for {TargetRid} at '{dir}'. Build the clr and " +
                    $"libs subsets for {TargetRid}, then rebuild this test project.");
            }

            return dir;
        }
    }

    public static string CoreCLRConfiguration => GetRequiredConfig("R2RTest.CoreCLRConfiguration");
    public static bool IsReleaseCoreCLR => string.Equals(CoreCLRConfiguration, "Release", StringComparison.OrdinalIgnoreCase);
    public static bool IsNotReleaseCoreCLR => !IsReleaseCoreCLR;
}
