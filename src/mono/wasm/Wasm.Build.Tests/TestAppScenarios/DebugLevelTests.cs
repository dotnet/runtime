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

public class DebugLevelTests : AppTestBase
{
    public DebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildWithDefaultLevel(string configuration)
    {
        CopyTestAsset("WasmBasicTestApp", "DebugLevelTests_BuildWithDefaultLevel");
        BuildProject(configuration);

        var result = await RunSdkStyleApp(new(
            Configuration: configuration,
            TestScenario: "DebugLevelTest"
        ));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("WasmDebugLevel: -1", m)
        );
    }
}