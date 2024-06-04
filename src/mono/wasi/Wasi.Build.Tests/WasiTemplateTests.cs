// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class WasiTemplateTests : BuildTestBase
{
    public WasiTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Debug", /*singleFileBundle*/ false)]
    [InlineData("Release", /*singleFileBundle*/ false)]
    [InlineData("Debug", /*singleFileBundle*/ true)]
    [InlineData("Release", /*singleFileBundle*/ true)]
    public void ConsoleBuildAndRunAOT(string config, bool singleFileBundle)
    {
        // This is specfically for the case where the project file has `RunATOCompilation=true`
        // and user *builds* the project, but the above setting ends up affecting the
        // build in some unexpected way. For example by add -DENABLE_AOT=1
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "SimpleMainWithArgs.cs"), Path.Combine(_projectDir!, "Program.cs"), true);

        var buildArgs = new BuildArgs(projectName, config, /*aot*/ true, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        string extraProperties = "<RunAOTCompilation>true</RunAOTCompilation><_WasmDevel>false</_WasmDevel>";
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
        RunWithoutBuild(config, id);
    }

    [Theory]
    [MemberData(nameof(TestDataForConsolePublishAndRun))]
    public void ConsoleBuildThenRunThenPublish(string config, bool singleFileBundle, bool aot)
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
        RunWithoutBuild(config, id);

        if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
            throw new XunitException($"Test bug: could not get the build product in the cache");

        File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

        _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));
    }

    [Theory]
    [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ false)]
    [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ true)]
    [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ false)]
    [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ true)]
    public void ConsoleBuildAndRunForDifferentOutputPaths(string config, bool appendRID, bool useArtifacts)
    {
        string extraPropertiesForDBP = "";
        if (appendRID)
            extraPropertiesForDBP += "<AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>";
        if (useArtifacts)
            extraPropertiesForDBP += "<UseArtifactsOutput>true</UseArtifactsOutput><ArtifactsPath>.</ArtifactsPath>";

        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        if (!string.IsNullOrEmpty(extraPropertiesForDBP))
            AddItemsPropertiesToProject(Path.Combine(Path.GetDirectoryName(projectFile)!, "Directory.Build.props"),
                                        extraPropertiesForDBP);

        var buildArgs = new BuildArgs(projectName, config, false, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        Publish: false,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));

        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {config} x y z")
                                    .EnsureSuccessful();

        Assert.Contains("Hello, Wasi Console!", res.Output);
    }
}
