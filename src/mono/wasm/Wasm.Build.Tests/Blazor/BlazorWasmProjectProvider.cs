// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class BlazorWasmProjectProvider : WasmSdkBasedProjectProvider
{
    public BlazorWasmProjectProvider(ITestOutputHelper _testOutput, string defaultTargetFramework, string? _projectDir = null)
            : base(_testOutput, defaultTargetFramework, _projectDir)
    { }

    public void AssertBundle(BlazorBuildOptions options)
        => AssertBundle(new AssertWasmSdkBundleOptions(
                Config: options.Config,
                BootConfigFileName: options.BootConfigFileName,
                IsPublish: options.IsPublish,
                TargetFramework: options.TargetFramework,
                BinFrameworkDir: options.BinFrameworkDir ?? FindBinFrameworkDir(options.Config, options.IsPublish, options.TargetFramework),
                GlobalizationMode: options.GlobalizationMode,
                CustomIcuFile: options.CustomIcuFile,
                ExpectFingerprintOnDotnetJs: options.ExpectFingerprintOnDotnetJs,
                ExpectedFileType: options.ExpectedFileType,
                RuntimeType: options.RuntimeType,
                AssertIcuAssets: true,
                AssertSymbolsFile: false // FIXME: not supported yet
            ));
}
