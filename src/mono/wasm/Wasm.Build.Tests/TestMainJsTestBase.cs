// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public abstract class TestMainJsTestBase : BuildTestBase
{
    protected TestMainJsProjectProvider _provider;
    protected TestMainJsTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(new TestMainJsProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<TestMainJsProjectProvider>();
    }
}
