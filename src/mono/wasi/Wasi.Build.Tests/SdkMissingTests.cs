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

#nullable enable

namespace Wasi.Build.Tests;

public class SdkMissingTests : BuildTestBase
{
    public SdkMissingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static TheoryData<string, string, bool> TestDataForNativeBuildFails(string extraProperties)
        => new TheoryData<string, string, bool>
            {
                { "Debug", extraProperties, false },
                { "Debug", extraProperties, true },
                { "Release", extraProperties, false },
                { "Release", extraProperties, true }
            };

    [Theory]
    [MemberData(nameof(TestDataForNativeBuildFails), "<WasmSingleFileBundle>true</WasmSingleFileBundle>")]
    [MemberData(nameof(TestDataForNativeBuildFails), "<InvariantGlobalization>true</InvariantGlobalization>")]
    public void NativeBuildOrPublishFails(string config, string extraProperties, bool publish)
    {
        string output = BuildWithInvalidSdkPath(config, extraProperties, publish, expectSuccess: false);
        Assert.Contains("SDK is required for building native files.", output);
    }

    [Theory]
    [InlineData("Debug", "<RunAOTCompilation>true</RunAOTCompilation>", false)]
    [InlineData("Release", "<RunAOTCompilation>true</RunAOTCompilation>", true)]
    public void AOTFailsOnlyOnPublish(string config, string extraProperties, bool publish)
    {
        string output = BuildWithInvalidSdkPath(config, extraProperties, publish, expectSuccess: !publish);
        if (publish)
            Assert.Contains("SDK is required for AOT'ing assemblies", output);
        else
            Assert.DoesNotContain("SDK is required", output);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void SimpleBuildDoesNotFail(string config)
        => BuildWithInvalidSdkPath(config, "", publish: false, expectSuccess: true);

    private string BuildWithInvalidSdkPath(string config, string extraProperties, bool publish, bool expectSuccess)
    {
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        var buildArgs = new BuildArgs(projectName, config, /*aot*/ true, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);
        AddItemsPropertiesToProject(projectFile, extraProperties);

        (_, string output) = BuildProject(buildArgs,
                                id: id,
                                new BuildProjectOptions(
                                    DotnetWasmFromRuntimePack: true,
                                    CreateProject: false,
                                    Publish: publish,
                                    TargetFramework: BuildTestBase.DefaultTargetFramework,
                                    ExtraBuildEnvironmentVariables: new Dictionary<string, string> { { "WASI_SDK_PATH", "x" } },
                                    ExpectSuccess: expectSuccess));

        return output;
    }
}
