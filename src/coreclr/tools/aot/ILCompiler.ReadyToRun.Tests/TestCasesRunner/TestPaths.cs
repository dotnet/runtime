// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

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

    private static string Crossgen2ExeName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "crossgen2.exe" : "crossgen2";

    /// <summary>
    /// The OS and architecture this root build targets, e.g. <c>linux</c>/<c>x64</c> or
    /// <c>browser</c>/<c>wasm</c>. Every test case compiles for this target.
    /// </summary>
    /// <remarks>
    /// The test assembly itself always runs on the build machine; this is what crossgen2 compiles
    /// <em>for</em>, which is independent of what the tests run <em>on</em>.
    /// </remarks>
    public static string TargetOS => GetRequiredConfig("R2RTest.TargetOS");
    public static string TargetArchitecture => GetRequiredConfig("R2RTest.TargetArchitecture");

    private static string TargetDescription => $"{TargetOS}-{TargetArchitecture}";

    public static bool IsWasmTarget => TargetArchitecture == "wasm";

    /// <summary>
    /// Gates the test cases that are not wasm-specific. crossgen2's wasm backend cannot yet compile
    /// most of what these tests exercise, and the R2R images it does produce are wasm modules rather
    /// than PE files, so <see cref="ILCompiler.Reflection.ReadyToRun.ReadyToRunReader"/> cannot read
    /// them back to validate. Test cases are therefore opt-in for wasm rather than opt-out.
    /// </summary>
    public static bool IsNotWasmTarget => !IsWasmTarget;

    public static bool IsArmTarget => TargetArchitecture is "arm" or "armel";

    public static bool IsWindowsTarget => TargetOS is "windows" or "win";

    /// <summary>
    /// Path to the crossgen2 that compiles for this build's target.
    /// </summary>
    /// <remarks>
    /// A build stages the crossgen2 it built next to the test assembly. A crossgen2 built for one
    /// target does not carry the cross-targeting JIT for any other, so substituting a different one
    /// either fails with a DllNotFoundException or silently compiles for the wrong architecture.
    /// </remarks>
    public static string Crossgen2Exe
    {
        get
        {
            string exe = Path.Combine(AppContext.BaseDirectory, "crossgen2", Crossgen2ExeName);
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException(
                    $"No crossgen2 staged for {TargetDescription} at '{exe}'. Build the clr and libs subsets for " +
                    $"{TargetDescription}, then rebuild this test project.");
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
                    $"No shared framework assemblies staged for {TargetDescription} at '{dir}'. Build the clr and " +
                    $"libs subsets for {TargetDescription}, then rebuild this test project.");
            }

            return dir;
        }
    }

    public static string CoreCLRConfiguration => GetRequiredConfig("R2RTest.CoreCLRConfiguration");
    public static bool IsReleaseCoreCLR => string.Equals(CoreCLRConfiguration, "Release", StringComparison.OrdinalIgnoreCase);
    public static bool IsNotReleaseCoreCLR => !IsReleaseCoreCLR;
}
