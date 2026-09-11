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
    bool? ExpectDotnetJsFingerprinting = null,
    // Runtime pack root (the directory that contains runtimes/<rid>/native/...) as resolved
    // by the build, parsed from MSBuild output. When set, ICU asset assertions resolve their
    // source paths from here instead of the workload-installed pack dir, so CoreCLR no-workload
    // runs (where the publish-time pack comes from a per-test NuGet cache that doesn't match
    // the SDK's pre-installed dotnet/packs/ contents) can still be validated correctly.
    string? RuntimePackDir = null
);
