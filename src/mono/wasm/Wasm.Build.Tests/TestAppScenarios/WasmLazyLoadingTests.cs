// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class WasmLazyLoadingTests : AppTestBase
{
    public WasmLazyLoadingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task LazyLoadAssembly()
    {
        CopyTestAsset("WasmBasicTestApp", "WasmLazyLoading");
        PublishProject("Debug");

        var testOutput = await RunSdkStyleApp(new(Configuration: "Debug", ForPublish: true, TestScenario: "LazyLoadingTest"));
        Assert.True(testOutput.Any(m => m.Contains("FirstName")), "The lazy loading test didn't emit expected message with JSON");
    }
}
