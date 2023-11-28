// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class BuildThenPublishTests : BuildTestBase
{
    public BuildThenPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug", /*aot*/ false, /*singleFileBundle*/ false)]
    [InlineData("Debug", /*aot*/ true, /*singleFileBundle*/ false)]
    [InlineData("Release", /*aot*/ false, /*singleFileBundle*/ false)]
    [InlineData("Release", /*aot*/ true, /*singleFileBundle*/ false)]
    [InlineData("Debug", /*aot*/ false, /*singleFileBundle*/ true)]
    // [InlineData("Debug", /*aot*/ true, /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    [InlineData("Release", /*aot*/ false, /*singleFileBundle*/ true)]
    // [InlineData("Release", /*aot*/ true, /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    public void ConsoleBuildThenPublish(string config, bool aot, bool singleFileBundle)
    {
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "SimpleMainWithArgs.cs"), Path.Combine(_projectDir!, "Program.cs"), true);

        var buildArgs = new BuildArgs(projectName, config, aot, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        string extraProperties = "";
        if (aot)
            extraProperties = "<RunAOTCompilation>true</RunAOTCompilation><_WasmDevel>false</_WasmDevel>";
        if (singleFileBundle)
            extraProperties += "<WasmSingleFileBundle>true</WasmSingleFileBundle>";
        if (!string.IsNullOrEmpty(extraProperties))
        AddItemsPropertiesToProject(projectFile, extraProperties);

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        Publish: false,
                        TargetFramework: BuildTestBase.DefaultTargetFramework));

        // ActiveIssue: https://github.com/dotnet/runtime/issues/82515
        int expectedExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 1 : 42;

        if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
            throw new XunitException($"Test bug: could not get the build product in the cache");

        File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

        _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

        bool expectRelinking = config == "Release";
        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: !expectRelinking,
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));
    }
}
