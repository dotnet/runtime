// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public abstract class BlazorWasmTestBase : BuildTestBase
{
    protected WasmSdkBasedProjectProvider _provider;
    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(new WasmSdkBasedProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
    }
}
