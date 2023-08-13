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
        string id = $"blazor_{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ false)]
    [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ true)]
    [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ true)]
    [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ false)]
    public async Task BlazorBuildAndRunForDifferentOutputPaths(string config, bool appendRID, bool useArtifacts)
    {
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        string extraPropertiesForDBP = "";
        if (appendRID)
            extraPropertiesForDBP += "<AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>";
        if (useArtifacts)
            extraPropertiesForDBP += "<UseArtifactsOutput>true</UseArtifactsOutput><ArtifactsPath>.</ArtifactsPath>";

        string projectDirectory = Path.GetDirectoryName(projectFile)!;
        if (!string.IsNullOrEmpty(extraPropertiesForDBP))
            AddItemsPropertiesToProject(Path.Combine(projectDirectory, "Directory.Build.props"),
                                        extraPropertiesForDBP);

        var buildArgs = new BuildArgs(projectName, config, false, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        BlazorBuildOptions buildOptions = new(id, config, NativeFilesType.FromRuntimePack);
        if (useArtifacts)
        {
            buildOptions = buildOptions with
            {
                BinFrameworkDir = Path.Combine(projectDirectory,
                                               "bin",
                                               id,
                                               config.ToLower(),
                                               "wwwroot",
                                               "_framework")
            };
        }
        BlazorBuild(buildOptions);
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    public async Task BlazorPublishRunTest(string config, bool aot)
    {
        string id = $"blazor_{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
        if (aot)
            AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");

        BlazorPublish(new BlazorBuildOptions(
            id,
            config,
            aot ? NativeFilesType.AOT
                : (config == "Release" ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack)));
        await BlazorRunForPublishWithWebServer(new BlazorRunOptions() { Config = config });
    }
}
