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

public class WasmSdkDebugLevelTests : DebugLevelTestsBase
{
    public WasmSdkDebugLevelTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected override void SetupProject(string projectId) => CopyTestAsset("WasmBasicTestApp", projectId);
    
    protected override Task<RunResult> RunForBuild(string configuration) => RunSdkStyleAppForBuild(new(
        Configuration: configuration,
        TestScenario: "DebugLevelTest"
    ));

    protected override Task<RunResult> RunForPublish(string configuration) => RunSdkStyleAppForPublish(new(
        Configuration: configuration,
        TestScenario: "DebugLevelTest"
    ));

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task PublishWithDefaultLevelAndPdbs(string configuration)
    {
        SetupProject($"DebugLevelTests_PublishWithDefaultLevelAndPdbs_{configuration}");
        PublishProject(configuration, extraArgs: $"-p:CopyOutputSymbolsToPublishDirectory=true");

        var result = await RunForPublish(configuration);
        AssertDebugLevel(result, -1);
    }
}
