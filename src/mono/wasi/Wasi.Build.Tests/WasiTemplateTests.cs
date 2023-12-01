// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Wasm.Build.Tests;
using System.Collections.Generic;
using System.Linq;

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
    // [InlineData("Debug", /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    // [InlineData("Release", /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    public void ConsoleBuildAndRunAOT(string config, bool singleFileBundle)
    {
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

    public static IEnumerable<object?[]> TestDataForConsolePublishAndRun() =>
        new IEnumerable<object?>[]
        {
            new object?[] { "Debug" },
            new object?[] { "Release" }
        }
        .AsEnumerable()
        .MultiplyWithSingleArgs(true, false) /*propertyValue*/
        .MultiplyWithSingleArgs(true, false) /*aot*/
        .UnwrapItemsAsArrays();

    [Theory]
    [InlineData("Debug", /*aot*/ false, /*singleFileBundle*/ false)]
    [InlineData("Debug", /*aot*/ true, /*singleFileBundle*/ false)]
    [InlineData("Release", /*aot*/ false, /*singleFileBundle*/ false)]
    [InlineData("Release", /*aot*/ true, /*singleFileBundle*/ false)]
    [InlineData("Debug", /*aot*/ false, /*singleFileBundle*/ true)]
    // [InlineData("Debug", /*aot*/ true, /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    [InlineData("Release", /*aot*/ false, /*singleFileBundle*/ true)]
    // [InlineData("Release", /*aot*/ true, /*singleFileBundle*/ true)] // https://github.com/dotnet/runtime/issues/95273
    // [MemberData(nameof(TestDataForConsolePublishAndRun))] // use when the issue is resolved
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

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [MemberData(nameof(TestDataForConsolePublishAndRun))]
    public void ConsolePublishAndRunForSingleFileBundle_InvariantTimeZone(string config, bool invariantTimezone, bool aot)
    {
        string extraProperties = invariantTimezone ? "<InvariantTimezone>true</InvariantTimezone>" : "";
        CommandResult res = ConsolePublishAndRunForSingleFileBundleInternal(config, "InvariantTimezones.cs", aot, extraProperties: extraProperties);
        if(invariantTimezone)
            Assert.Contains("Could not find Asia/Tokyo", res.Output);
        else
            Assert.Contains("Asia/Tokyo BaseUtcOffset is 09:00:00", res.Output);
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [MemberData(nameof(TestDataForConsolePublishAndRun))]
    public void ConsolePublishAndRunForSingleFileBundle_InvariantGlobalization(string config, bool invariantGlobalization, bool aot)
    {
        string extraProperties = invariantGlobalization ? "<InvariantGlobalization>true</InvariantGlobalization>" : "";
        CommandResult res = ConsolePublishAndRunForSingleFileBundleInternal(config, "InvariantGlobalization.cs", aot, extraProperties: extraProperties);
        Assert.Contains("Number: 1", res.Output);
    }

    private CommandResult ConsolePublishAndRunForSingleFileBundleInternal(string config, string programFileName, bool aot, string extraProperties = "")
    {
        if (string.IsNullOrEmpty(programFileName))
            throw new ArgumentException("Cannot be empty", nameof(programFileName));

        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, programFileName), Path.Combine(_projectDir!, "Program.cs"), true);

        extraProperties += "<WasmSingleFileBundle>true</WasmSingleFileBundle>";
        AddItemsPropertiesToProject(projectFile, extraProperties);

        var buildArgs = new BuildArgs(projectName, config, aot, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false, // singlefilebundle will always relink
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));
        return RunWithoutBuild(config, id);
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

    private CommandResult RunWithoutBuild(string config, string id)
    {
        string runArgs = $"run --no-build -c {config}";
        runArgs += " x y z";
        // ActiveIssue: https://github.com/dotnet/runtime/issues/82515
        int expectedExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 1 : 42;
        CommandResult res = new RunCommand(s_buildEnv, _testOutput, label: id)
                            .WithWorkingDirectory(_projectDir!)
                            .ExecuteWithCapturedOutput(runArgs)
                            .EnsureExitCode(expectedExitCode);

        Assert.Contains("Hello, Wasi Console!", res.Output);
        Assert.Contains("args[0] = x", res.Output);
        Assert.Contains("args[1] = y", res.Output);
        Assert.Contains("args[2] = z", res.Output);
        return res;
    }
}
