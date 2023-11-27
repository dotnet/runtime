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

public class PublishThenRunTests : BuildTestBase
{
    public PublishThenRunTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static TheoryData<string, bool, bool> TestDataForConsolePublishThenRunWithWorkloads()
    {
        var data = new TheoryData<string, bool, bool>();
        data.Add("Debug", false, false);
        data.Add("Debug", true, true);
        data.Add("Release", false, false); // Release relinks by default
        return data;
    }


    public static TheoryData<string, bool, bool> TestDataForConsolePublishThenRun()
    {
        var data = new TheoryData<string, bool, bool>();
        data.Add("Debug", true, true);
        data.Add("Release", true, true);
        data.Add("Debug", true, false);
        data.Add("Release", true, false);
        data.Add("Debug", false, false);
        data.Add("Release", false, false);
        return data;
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [MemberData(nameof(TestDataForConsolePublishThenRunWithWorkloads))]
    public void ConsolePublishAndRunForSingleFileBundle(string config, bool relinking, bool invariantTimezone) =>
        ConsolePublishThenRunForDifferentBundlings(config, relinking, invariantTimezone, false, true);


    [Theory]
    [MemberData(nameof(TestDataForConsolePublishThenRun))]
    public void ConsolePublishAndRun(string config, bool aot, bool singleFileBundle) =>
        ConsolePublishThenRunForDifferentBundlings(config, false, false, aot, singleFileBundle);

    private void ConsolePublishThenRunForDifferentBundlings(string config, bool relinking, bool invariantTimezone, bool aot, bool singleFileBundle)
    {
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "SimpleMainWithArgs.cs"), Path.Combine(_projectDir!, "Program.cs"), true);

        string extraProperties = @$"<WasmSingleFileBundle>{singleFileBundle}</WasmSingleFileBundle>
            <InvariantTimezone>{invariantTimezone}</InvariantTimezone>";
        if (relinking)
            extraProperties += "<WasmBuildNative>true</WasmBuildNative>";
        if (aot)
            extraProperties += "<RunAOTCompilation>true</RunAOTCompilation><_WasmDevel>false</_WasmDevel>";

        AddItemsPropertiesToProject(projectFile, extraProperties);

        var buildArgs = new BuildArgs(projectName, config, /*aot*/aot, id, null);
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
}
