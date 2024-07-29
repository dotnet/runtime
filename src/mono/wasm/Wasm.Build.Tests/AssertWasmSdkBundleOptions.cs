// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

// Identical to AssertBundleOptionsBase currently
public record AssertWasmSdkBundleOptions(
    string Config,
    bool IsPublish,
    string TargetFramework,
    string BinFrameworkDir,
    string? PredefinedIcudt,
    GlobalizationMode GlobalizationMode = GlobalizationMode.Sharded,
    string BootJsonFileName = "blazor.boot.json",
    NativeFilesType ExpectedFileType = NativeFilesType.FromRuntimePack,
    RuntimeVariant RuntimeType = RuntimeVariant.SingleThreaded,
    bool ExpectFingerprintOnDotnetJs = false,
    bool ExpectSymbolsFile = true,
    bool AssertIcuAssets = true,
    bool AssertSymbolsFile = true)
        : AssertBundleOptionsBase(
               Config: Config,
               IsPublish: IsPublish,
               TargetFramework: TargetFramework,
               BinFrameworkDir: BinFrameworkDir,
               PredefinedIcudt: PredefinedIcudt,
               GlobalizationMode: GlobalizationMode,
               ExpectedFileType: ExpectedFileType,
               RuntimeType: RuntimeType,
               BootJsonFileName: BootJsonFileName,
               ExpectFingerprintOnDotnetJs: ExpectFingerprintOnDotnetJs,
               ExpectSymbolsFile: ExpectSymbolsFile,
               AssertIcuAssets: AssertIcuAssets,
               AssertSymbolsFile: AssertSymbolsFile)
{}
