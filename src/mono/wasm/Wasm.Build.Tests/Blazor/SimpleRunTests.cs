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
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BlazorBuildRunTest(string config)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, BasicTestApp, "blazor");
        BlazorBuild(info);
        await RunForBuildWithDotnetRun(new(config));
    }

    [Theory]
    [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ false)]
    [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ true)]
    [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ true)]
    [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ false)]
    public async Task BlazorBuildAndRunForDifferentOutputPaths(string config, bool appendRID, bool useArtifacts)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, BasicTestApp, "blazor");
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
                    projectDir, "bin", info.ProjectName, config.ToLower(), "wwwroot", "_framework") :
                GetBinFrameworkDir(config, isPublish);
        BuildProject(info,
                new BuildOptions(
                    config,
                    info.ProjectName,
                    BinFrameworkDir: frameworkDir,
                    ExpectedFileType: GetExpectedFileType(info, isPublish),
                    IsPublish: isPublish
            ));
        await RunForBuildWithDotnetRun(new(config));
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    public async Task BlazorPublishRunTest(string config, bool aot)
    {
        ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "blazor_publish");
        BlazorPublish(info);
        await RunForPublishWithWebServer(new(config));
    }
}
