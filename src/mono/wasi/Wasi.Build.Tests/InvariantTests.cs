// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class InvariantTests : BuildTestBase
{
    public InvariantTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
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
}
