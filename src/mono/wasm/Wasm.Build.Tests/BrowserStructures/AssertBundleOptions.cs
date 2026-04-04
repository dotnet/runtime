// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;

namespace Wasm.Build.Tests;

public record AssertBundleOptions(
    Configuration Configuration,
    MSBuildOptions BuildOptions,
    NativeFilesType ExpectedFileType,
    string BinFrameworkDir,
    bool ExpectSymbolsFile = true,
    bool AssertIcuAssets = true,
    bool AssertSymbolsFile = true,
    bool? ExpectDotnetJsFingerprinting = null
);
