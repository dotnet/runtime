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

public class TimezoneTests : BuildTestBase
{
    public TimezoneTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
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
    public void ConsoleWithTimezonesForSingleFileBundle(string config, bool relinking, bool invariantTimezone)
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
}
