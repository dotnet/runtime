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

public class BuildThenRun : BuildTestBase
{
    public BuildThenRun(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
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

    [Theory]
    [InlineData("Debug", /*aot*/ false, /*singleFileBundle*/ false)]
    [InlineData("Debug", /*aot*/ true, /*singleFileBundle*/ false)]
    [InlineData("Release", /*aot*/ false, /*singleFileBundle*/ false)]
    [InlineData("Release", /*aot*/ true, /*singleFileBundle*/ false)]
    [InlineData("Debug", /*aot*/ false, /*singleFileBundle*/ true)]
    // [InlineData("Debug", /*aot*/ true, /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    [InlineData("Release", /*aot*/ false, /*singleFileBundle*/ true)]
    // [InlineData("Release", /*aot*/ true, /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    public void ConsoleBuildThenRunThenPublish(string config, bool aot, bool singleFileBundle)
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

        string runArgs = $"run --no-build -c {config}";
        runArgs += " x y z";
        var res = new RunCommand(s_buildEnv, _testOutput, label: id)
                            .WithWorkingDirectory(_projectDir!)
                            .ExecuteWithCapturedOutput(runArgs)
                            .EnsureExitCode(expectedExitCode);

        Assert.Contains("Hello, Wasi Console!", res.Output);
        Assert.Contains("args[0] = x", res.Output);
        Assert.Contains("args[1] = y", res.Output);
        Assert.Contains("args[2] = z", res.Output);

        if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
            throw new XunitException($"Test bug: could not get the build product in the cache");

        File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

        _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false,
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));
    }

    public static TheoryData<string, bool, bool> TestDataForConsoleTimezonesSingleFile()
    {
        var data = new TheoryData<string, bool, bool>();
        data.Add("Debug", false, false);
        data.Add("Debug", true, true);
        data.Add("Release", false, false); // Release relinks by default
        return data;
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [MemberData(nameof(TestDataForConsoleTimezonesSingleFile))]
    public void ConsoleWithTimezonesPublishAndRunForSingleFileBundle(string config, bool relinking, bool invariantTimezone)
    {
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "Timezones.cs"), Path.Combine(_projectDir!, "Program.cs"), true);

        string extraProperties = "<WasmSingleFileBundle>true</WasmSingleFileBundle>";
        if (relinking)
            extraProperties += "<WasmBuildNative>true</WasmBuildNative>";
        if (invariantTimezone)
            extraProperties += "<InvariantTimezone>true</InvariantTimezone>";

        AddItemsPropertiesToProject(projectFile, extraProperties);

        var buildArgs = new BuildArgs(projectName, config, /*aot*/false, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        bool expectRelinking = config == "Release" || relinking;
        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: !expectRelinking,
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));

        int expectedExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 1 : 42;

        string runArgs = $"run --no-build -c {config}";
        runArgs += " x y z";
        var res = new RunCommand(s_buildEnv, _testOutput, label: id)
                            .WithWorkingDirectory(_projectDir!)
                            .ExecuteWithCapturedOutput(runArgs)
                            .EnsureExitCode(expectedExitCode);

        Assert.Contains("Hello, Wasi Console!", res.Output);
        Assert.Contains("args[0] = x", res.Output);
        Assert.Contains("args[1] = y", res.Output);
        Assert.Contains("args[2] = z", res.Output);
        if(invariantTimezone)
        {
            Assert.Contains("Could not find Asia/Tokyo", res.Output);
        }
        else
        {
            Assert.Contains("Asia/Tokyo BaseUtcOffset is 09:00:00", res.Output);
        }
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
