// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

public record BlazorBuildOptions
(
    string Id,
    string Config,
    NativeFilesType ExpectedFileType = NativeFilesType.FromRuntimePack,
    string TargetFramework = BuildTestBase.DefaultTargetFrameworkForBlazor,
    bool IsPublish = false,
    bool WarnAsError = true,
    bool ExpectSuccess = true,
    bool ExpectRelinkDirWhenPublishing = false,
    bool ExpectFingerprintOnDotnetJs = false,
    RuntimeVariant RuntimeType = RuntimeVariant.SingleThreaded,
    GlobalizationMode GlobalizationMode = GlobalizationMode.Sharded,
    string PredefinedIcudt = "",
    bool AssertAppBundle = true,
    string? BinFrameworkDir = null,
    string? Label = null
);
