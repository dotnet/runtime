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

namespace Wasm.Build.Tests.Blazor;

public class CleanTests : BlazorWasmTestBase
{
    public CleanTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void Blazor_BuildThenClean_NativeRelinking(Configuration config)
    {
        string extraProperties = @"<_WasmDevel>true</_WasmDevel><WasmBuildNative>true</WasmBuildNative>";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "clean", extraProperties: extraProperties);
        BlazorBuild(info, config, isNativeBuild: true);

        string relinkDir = Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFrameworkForBlazor, "wasm", "for-build");
        Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, info.ProjectName, $"{info.ProjectName}-clean.binlog");
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir);
        cmd.WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("build", "-t:Clean", $"-p:Configuration={config}", $"-bl:{logPath}")
            .EnsureSuccessful();

        AssertEmptyOrNonExistentDirectory(relinkDir);
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void Blazor_BuildNoNative_ThenBuildNative_ThenClean(Configuration config)
        => Blazor_BuildNativeNonNative_ThenCleanTest(config, firstBuildNative: false);

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void Blazor_BuildNative_ThenBuildNonNative_ThenClean(Configuration config)
        => Blazor_BuildNativeNonNative_ThenCleanTest(config, firstBuildNative: true);

    private void Blazor_BuildNativeNonNative_ThenCleanTest(Configuration config, bool firstBuildNative)
    {
        string extraProperties = @"<_WasmDevel>true</_WasmDevel>";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "clean_native", extraProperties: extraProperties);

        bool relink = firstBuildNative;
        BlazorBuild(info,
            config,
            new BuildOptions(ExtraMSBuildArgs: relink ? "-p:WasmBuildNative=true" : string.Empty),
            isNativeBuild: relink);

        string relinkDir = Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFrameworkForBlazor, "wasm", "for-build");
        if (relink)
            Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        relink = !firstBuildNative;
        BlazorBuild(info,
            config,
            new BuildOptions(UseCache: false, ExtraMSBuildArgs: relink ? "-p:WasmBuildNative=true" : string.Empty),
            isNativeBuild: relink ? true : null);

        if (relink)
            Assert.True(Directory.Exists(relinkDir), $"Could not find expected relink dir: {relinkDir}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, info.ProjectName, $"{info.ProjectName}-clean.binlog");
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir);
        cmd.WithEnvironmentVariable("NUGET_PACKAGES", _projectDir)
                .ExecuteWithCapturedOutput("build", "-t:Clean", $"-p:Configuration={config}", $"-bl:{logPath}")
                .EnsureSuccessful();

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
