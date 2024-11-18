// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;

namespace Wasm.Build.Tests;

public record AssertBundleOptions(
    MSBuildOptions BuildOptions,
    bool ExpectSymbolsFile = true,
    bool AssertIcuAssets = true,
    bool AssertSymbolsFile = true,
    NativeFilesType ExpectedFileType
);
