// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;

namespace Wasm.Build.Tests;

public abstract record AssertBundleOptionsBase(
    string Config,
    bool IsPublish,
    string TargetFramework,
    string BinFrameworkDir,
    string? PredefinedIcudt,
    string BundleDirName = "wwwroot",
    GlobalizationMode GlobalizationMode = GlobalizationMode.Sharded,
    string BootJsonFileName = "blazor.boot.json",
    NativeFilesType ExpectedFileType = NativeFilesType.FromRuntimePack,
    RuntimeVariant RuntimeType = RuntimeVariant.SingleThreaded,
    bool ExpectFingerprintOnDotnetJs = false,
    bool ExpectSymbolsFile = true,
    bool AssertIcuAssets = true,
    bool AssertSymbolsFile = true)
{
    public bool DotnetWasmFromRuntimePack => ExpectedFileType == NativeFilesType.FromRuntimePack;
    public bool AOT => ExpectedFileType == NativeFilesType.AOT;
    public string BundleDir => Path.Combine(BinFrameworkDir, "..");
}
