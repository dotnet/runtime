// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;

namespace Wasm.Build.Tests;

public record AssertBundleOptions(
    BuildProjectOptions BuildOptions,
    bool ExpectSymbolsFile = true,
    bool AssertIcuAssets = true,
    bool AssertSymbolsFile = true,
    bool ExpectFingerprintOnDotnetJs = false
    )
{
    public bool DotnetWasmFromRuntimePack => BuildOptions.ExpectedFileType == NativeFilesType.FromRuntimePack;
    public bool AOT => BuildOptions.ExpectedFileType == NativeFilesType.AOT;
    public string BundleDir => Path.Combine(BuildOptions.BinFrameworkDir, "..");
}
