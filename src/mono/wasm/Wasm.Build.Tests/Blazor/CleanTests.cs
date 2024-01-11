// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wasm.Build.NativeRebuild.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class CleanTests : BlazorWasmTestBase
{
    public CleanTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task Blazor_BuildThenClean_NativeRelinkingAsync(string config)
    {
        string id = GetRandomId();

        InitBlazorWasmProjectDir(id);
        string projectFile = await CreateBlazorWasmTemplateProjectAsync(id);

        string extraProperties = @"<_WasmDevel>true</_WasmDevel>
                                    <WasmBuildNative>true</WasmBuildNative>";

        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);
        await BlazorBuildAsync(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        string relinkDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm", "for-build");
        Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-clean.binlog");
        CommandResult res = await new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutputAsync("build", "-t:Clean", $"-p:Configuration={config}", $"-bl:{logPath}");
        res.EnsureSuccessful();

        AssertEmptyOrNonExistentDirectory(relinkDir);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public Task Blazor_BuildNoNative_ThenBuildNative_ThenClean(string config)
        => Blazor_BuildNativeNonNative_ThenCleanTestAsync(config, firstBuildNative: false);

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public Task Blazor_BuildNative_ThenBuildNonNative_ThenClean(string config)
        => Blazor_BuildNativeNonNative_ThenCleanTestAsync(config, firstBuildNative: true);

    private async Task Blazor_BuildNativeNonNative_ThenCleanTestAsync(string config, bool firstBuildNative)
    {
        string id = GetRandomId();

        InitBlazorWasmProjectDir(id);
        string projectFile = await CreateBlazorWasmTemplateProjectAsync(id);

        string extraProperties = @"<_WasmDevel>true</_WasmDevel>";

        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        bool relink = firstBuildNative;
        await BlazorBuildInternalAsync(id, config, publish: false,
                        extraArgs: relink ? "-p:WasmBuildNative=true" : string.Empty);

        string relinkDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm", "for-build");
        if (relink)
            Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        relink = !firstBuildNative;
        await BlazorBuildInternalAsync(id, config, publish: false,
                        extraArgs: relink ? "-p:WasmBuildNative=true" : string.Empty);

        if (relink)
            Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-clean.binlog");
        CommandResult res = await new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("NUGET_PACKAGES", _projectDir!)
                .ExecuteWithCapturedOutputAsync("build", "-t:Clean", $"-p:Configuration={config}", $"-bl:{logPath}");
        res.EnsureSuccessful();

        AssertEmptyOrNonExistentDirectory(relinkDir);
    }
    private void AssertEmptyOrNonExistentDirectory(string dirPath)
    {
        _testOutput.WriteLine($"dirPath: {dirPath}");
        if (!Directory.Exists(dirPath))
            return;

        var files = Directory.GetFileSystemEntries(dirPath);
        if (files.Length == 0)
            return;

        string found = string.Join(',', files.Select(p => Path.GetFileName(p)));
        throw new XunitException($"Expected dir {dirPath} to be empty, but found: {found}");
    }
}
