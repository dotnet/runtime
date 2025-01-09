// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class SimpleRunTests : BlazorWasmTestBase
{
    public SimpleRunTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task BlazorBuildRunTest(Configuration config)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "blazor");
        BlazorBuild(info, config);
        await RunForBuildWithDotnetRun(new BlazorRunOptions(config));
    }

    [Theory]
    [InlineData(Configuration.Debug, /*appendRID*/ true, /*useArtifacts*/ false)]
    [InlineData(Configuration.Debug, /*appendRID*/ true, /*useArtifacts*/ true)]
    [InlineData(Configuration.Debug, /*appendRID*/ false, /*useArtifacts*/ true)]
    [InlineData(Configuration.Debug, /*appendRID*/ false, /*useArtifacts*/ false)]
    public async Task BlazorBuildAndRunForDifferentOutputPaths(Configuration config, bool appendRID, bool useArtifacts)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "blazor");
        string extraPropertiesForDBP = "";
        if (appendRID)
            extraPropertiesForDBP += "<AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>";
        if (useArtifacts)
            extraPropertiesForDBP += "<UseArtifactsOutput>true</UseArtifactsOutput><ArtifactsPath>.</ArtifactsPath>";
        string projectDir = Path.GetDirectoryName(info.ProjectFilePath) ?? "";
        string rootDir = Path.GetDirectoryName(projectDir) ?? "";
        if (!string.IsNullOrEmpty(extraPropertiesForDBP))
            AddItemsPropertiesToProject(Path.Combine(rootDir, "Directory.Build.props"),
                                        extraPropertiesForDBP);

        bool isPublish = false;
        string frameworkDir = useArtifacts ?
                Path.Combine(
                    projectDir, "bin", info.ProjectName, config.ToString().ToLower(), "wwwroot", "_framework") :
                GetBinFrameworkDir(config, isPublish);
        BuildProject(info, config, new BuildOptions(NonDefaultFrameworkDir: frameworkDir));
        await RunForBuildWithDotnetRun(new BlazorRunOptions(config));
    }

    [Theory]
    [InlineData(Configuration.Debug, false)]
    [InlineData(Configuration.Release, false)]
    [InlineData(Configuration.Release, true)]
    public async Task BlazorPublishRunTest(Configuration config, bool aot)
    {
        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.BlazorBasicTestApp, "blazor_publish");
        BlazorPublish(info, config);
        await RunForPublishWithWebServer(new BlazorRunOptions(config));
    }
}
