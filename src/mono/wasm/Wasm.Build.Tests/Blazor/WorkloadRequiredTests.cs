// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class WorkloadRequiredTests : BlazorWasmTestBase
{
    /* Keep in sync with settings in wasm.proj, and WasmApp.Native.targets .
     * The `triggerValue` here is opposite of the default used when building the runtime pack
     * (see wasm.proj), and thus requiring a native build
     */
    public static (string propertyName, bool triggerValue)[] PropertiesWithTriggerValues = new[]
    {
        ("RunAOTCompilation", true),
        ("WasmEnableLegacyJsInterop", false),
        ("WasmEnableSIMD", false),
        ("WasmEnableExceptionHandling", false),
        ("InvariantTimezone", true),
        //("InvariantGlobalization", true), - not applicable for blazor
        ("WasmNativeStrip", false)
    };

    public WorkloadRequiredTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static TheoryData<string, string, bool> SettingDifferentFromValuesInRuntimePack(bool forPublish)
    {
        TheoryData<string, string, bool> data = new();

        string[] configs = new[] { "Debug", "Release" };
        foreach (var defaultPair in PropertiesWithTriggerValues)
        {
            foreach (string config in configs)
            {
                data.Add(config, $"<{defaultPair.propertyName}>{defaultPair.triggerValue}</{defaultPair.propertyName}>", true);
                data.Add(config, $"<{defaultPair.propertyName}>{!defaultPair.triggerValue}</{defaultPair.propertyName}>", false);
            }
        }

        return data;
    }

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(SettingDifferentFromValuesInRuntimePack), parameters: false)]
    public void WorkloadRequiredForBuild(string config, string extraProperties, bool workloadNeeded)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: false);

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(SettingDifferentFromValuesInRuntimePack), parameters: false)]
    public void WorkloadRequiredForPublish(string config, string extraProperties, bool workloadNeeded)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: true);

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug", "<InvariantGlobalization>true</InvariantGlobalization>", /*workloadNeeded*/ false, /*publish*/ false)]
    [InlineData("Debug", "<InvariantGlobalization>false</InvariantGlobalization>", /*workloadNeeded*/ false, /*publish*/ false)]
    [InlineData("Release", "<InvariantGlobalization>true</InvariantGlobalization>", /*workloadNeeded*/ false, /*publish*/ true)]
    [InlineData("Release", "<InvariantGlobalization>false</InvariantGlobalization>", /*workloadNeeded*/ false, /*publish*/ true)]
    public void WorkloadNotRequiredForInvariantGlobalization(string config, string extraProperties, bool workloadNeeded, bool publish)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: publish);

    private void CheckWorkloadRequired(string config, string extraProperties, bool workloadNeeded, bool publish)
    {
        string id = $"props_req_workload_{(publish ? "publish" : "build")}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
        AddItemsPropertiesToProject(projectFile, extraProperties,
            atTheEnd: @"<Target Name=""StopBuildBeforeCompile"" BeforeTargets=""Compile"">
                    <Error Text=""Stopping the build"" />
            </Target>");

        CommandResult result;
        if (publish)
            (result, _) = BlazorPublish(new BlazorBuildOptions(id, config, ExpectSuccess: false));
        else
            (result, _) = BlazorBuild(new BlazorBuildOptions(id, config, ExpectSuccess: false));

        if (workloadNeeded)
        {
            Assert.Contains("following workloads must be installed: wasm-tools", result.Output);
            Assert.DoesNotContain("error : Stopping the build", result.Output);
        }
        else
        {
            Assert.DoesNotContain("following workloads must be installed: wasm-tools", result.Output);
            Assert.Contains("error : Stopping the build", result.Output);
        }
    }
}
