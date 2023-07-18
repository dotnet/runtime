// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.Serialization.Json;
using Microsoft.NET.Sdk.WebAssembly;

#nullable enable

namespace Wasm.Build.Tests;

public class BlazorWasmProjectProvider : WasmSdkBasedProjectProvider
{
    public BlazorWasmProjectProvider(ITestOutputHelper _testOutput, string? _projectDir = null)
            : base(_testOutput, _projectDir)
    {}

    public void AssertBlazorBootJson(
        string config,
        bool isPublish,
        string targetFramework = BuildTestBase.DefaultTargetFrameworkForBlazor,
        bool expectFingerprintOnDotnetJs = false,
        RuntimeVariant runtimeType = RuntimeVariant.SingleThreaded)
    {
        AssertBootJson(binFrameworkDir: FindBlazorBinFrameworkDir(config, isPublish, targetFramework),
                      isPublish: isPublish,
                      expectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs,
                      runtimeType: runtimeType);
    }

    public void AssertBlazorBundle(
        BlazorBuildOptions options,
        bool isPublish,
        string? binFrameworkDir = null)
    {
        EnsureProjectDirIsSet();
        if (options.TargetFramework is null)
            options = options with { TargetFramework = BuildTestBase.DefaultTargetFrameworkForBlazor };

        AssertDotNetNativeFiles(options.ExpectedFileType,
                                      options.Config,
                                      forPublish: isPublish,
                                      targetFramework: options.TargetFramework,
                                      expectFingerprintOnDotnetJs: options.ExpectFingerprintOnDotnetJs,
                                      runtimeType: options.RuntimeType);

        AssertBlazorBootJson(config: options.Config,
                             isPublish: isPublish,
                             targetFramework: options.TargetFramework,
                             expectFingerprintOnDotnetJs: options.ExpectFingerprintOnDotnetJs,
                             runtimeType: options.RuntimeType);
    }
}
