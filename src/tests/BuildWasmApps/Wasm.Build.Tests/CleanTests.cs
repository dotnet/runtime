// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Wasm.Build.NativeRebuild.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class CleanTests : NativeRebuildTestsBase
{
    public CleanTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void Blazor_BuildThenClean_NativeRelinking(string config)
    {
        string id = Path.GetRandomFileName();

        InitBlazorWasmProjectDir(id);
        string projectFile = CreateBlazorWasmTemplateProject(id);

        string extraProperties = @"<_WasmDevel>true</_WasmDevel>
                                    <WasmBuildNative>true</WasmBuildNative>";

        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        string relinkDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFramework, "wasm", "for-build");
        Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-clean.binlog");
        new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput("build", "-t:Clean", $"-p:Configuration={config}", $"-bl:{logPath}")
                .EnsureSuccessful();

        AssertEmptyOrNonExistantDirectory(relinkDir);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void Blazor_BuildNoNative_ThenBuildNative_ThenClean(string config)
        => Blazor_BuildNativeNonNative_ThenCleanTest(config, firstBuildNative: false);

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void Blazor_BuildNative_ThenBuildNonNative_ThenClean(string config)
        => Blazor_BuildNativeNonNative_ThenCleanTest(config, firstBuildNative: true);

    private void Blazor_BuildNativeNonNative_ThenCleanTest(string config, bool firstBuildNative)
    {
        string id = Path.GetRandomFileName();

        InitBlazorWasmProjectDir(id);
        string projectFile = CreateBlazorWasmTemplateProject(id);

        string extraProperties = @"<_WasmDevel>true</_WasmDevel>";

        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        bool relink = firstBuildNative;
        BuildInternal(id, config, publish: false,
                        extraArgs: relink ? "-p:WasmBuildNative=true" : string.Empty);

        string relinkDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFramework, "wasm", "for-build");
        if (relink)
            Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        relink = !firstBuildNative;
        BuildInternal(id, config, publish: false,
                        extraArgs: relink ? "-p:WasmBuildNative=true" : string.Empty);

        if (relink)
            Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-clean.binlog");
        new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput("build", "-t:Clean", $"-p:Configuration={config}", $"-bl:{logPath}")
                .EnsureSuccessful();

        AssertEmptyOrNonExistantDirectory(relinkDir);
    }
    private void AssertEmptyOrNonExistantDirectory(string dirPath)
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
