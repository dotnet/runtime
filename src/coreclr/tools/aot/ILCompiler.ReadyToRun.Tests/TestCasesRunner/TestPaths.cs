// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    private static string? GetOptionalConfig(string key)
    {
        string? value = AppContext.GetData(key) as string;
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Path to the crossgen2 output directory (contains the self-contained crossgen2 executable and clrjit).
    /// e.g. artifacts/bin/coreclr/linux.x64.Checked/x64/crossgen2/
    /// </summary>
    public string Crossgen2Dir => GetRequiredConfig("R2RTest.Crossgen2Dir");

    private static string Crossgen2ExeName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "crossgen2.exe" : "crossgen2";

    /// <summary>
    /// Path to the self-contained crossgen2 executable (from crossgen2_inbuild).
    /// </summary>
    public string Crossgen2Exe => Path.Combine(Crossgen2Dir, Crossgen2ExeName);

    /// <summary>
    /// Path to the host runtime pack managed assemblies directory.
    /// </summary>
    public string RuntimePackDir => GetRequiredConfig("R2RTest.RuntimePackDir");

    /// <summary>
    /// Path to the host runtime pack native directory.
    /// </summary>
    public string RuntimePackNativeDir => GetRequiredConfig("R2RTest.RuntimePackNativeDir");

    /// <summary>
    /// Path to the browser-wasm runtime pack managed assemblies directory, or <c>null</c> when the
    /// browser-wasm runtime pack has not been built/downloaded (e.g. in the default linux-x64 tools
    /// test job). When present, the wasm crossgen2 compilation references these real browser-wasm
    /// framework assemblies instead of the host runtime pack.
    /// e.g. artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Release/runtimes/browser-wasm/lib/net11.0/
    /// </summary>
    public string? WasmRuntimePackDir => GetOptionalConfig("R2RTest.WasmRuntimePackDir");

    /// <summary>
    /// Path to the browser-wasm runtime pack native directory (contains the wasm
    /// System.Private.CoreLib.dll), or <c>null</c> when not available.
    /// e.g. artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Release/runtimes/browser-wasm/native/
    /// </summary>
    public string? WasmRuntimePackNativeDir => GetOptionalConfig("R2RTest.WasmRuntimePackNativeDir");

    public bool RequireWasmReferences => string.Equals(
        GetOptionalConfig("R2RTest.RequireWasmReferences"),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);

    public static string CoreCLRConfiguration => GetRequiredConfig("R2RTest.CoreCLRConfiguration");
    public static bool IsReleaseCoreCLR => string.Equals(CoreCLRConfiguration, "Release", StringComparison.OrdinalIgnoreCase);
    public static bool IsNotReleaseCoreCLR => !IsReleaseCoreCLR;
    public static bool ArmOnHostOSSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
