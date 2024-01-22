// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class WorkloadRequiredTests : BlazorWasmTestBase
{
    /* Keep in sync with settings in browser.proj, and WasmApp.Native.targets .
     * The `triggerValue` here is opposite of the default used when building the runtime pack
     * (see browser.proj), and thus requiring a native build
     */
    public static (string propertyName, bool triggerValue)[] PropertiesWithTriggerValues = new[]
    {
        ("RunAOTCompilation", true),
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

    public static TheoryData<string, string, bool> SettingDifferentFromValuesInRuntimePack()
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
    [MemberData(nameof(SettingDifferentFromValuesInRuntimePack))]
    public void WorkloadRequiredForBuild(string config, string extraProperties, bool workloadNeeded)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: false);

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(SettingDifferentFromValuesInRuntimePack))]
    public void WorkloadRequiredForPublish(string config, string extraProperties, bool workloadNeeded)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: true);

    public static TheoryData<string, bool, bool> InvariantGlobalizationTestData(bool publish)
    {
        TheoryData<string, bool, bool> data = new();
        foreach (string config in new[] { "Debug", "Release" })
        {
            data.Add(config, /*invariant*/ true, /*publish*/ publish);
            data.Add(config, /*invariant*/ false, /*publish*/ publish);
        }
        return data;
    }

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(InvariantGlobalizationTestData), parameters: /*publish*/ false)]
    [MemberData(nameof(InvariantGlobalizationTestData), parameters: /*publish*/ true)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task WorkloadNotRequiredForInvariantGlobalization(string config, bool invariant, bool publish)
    {
        string id = $"props_req_workload_{(publish ? "publish" : "build")}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

        if (invariant)
            AddItemsPropertiesToProject(projectFile, extraProperties: "<InvariantGlobalization>true</InvariantGlobalization>");

        string counterPath = Path.Combine(Path.GetDirectoryName(projectFile)!, "Pages", "Counter.razor");
        string allText = File.ReadAllText(counterPath);
        string ccText = "currentCount++;";
        if (allText.IndexOf(ccText) < 0)
            throw new Exception("Counter.razor does not have the expected content. Test needs to be updated.");

        allText = allText.Replace(ccText, $"{ccText}{Environment.NewLine}TestInvariantCulture();");
        allText += s_invariantCultureMethodForBlazor;
        File.WriteAllText(counterPath, allText);
        _testOutput.WriteLine($"Updated counter.razor: {allText}");

        CommandResult result;
        GlobalizationMode mode = invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded;
        if (publish)
        {
            (result, _) = BlazorPublish(
                            new BlazorBuildOptions(
                                id,
                                config,
                                ExpectSuccess: true,
                                GlobalizationMode: mode));
        }
        else
        {
            (result, _) = BlazorBuild(
                            new BlazorBuildOptions(
                                id,
                               config,
                               ExpectSuccess: true,
                               GlobalizationMode: mode));
        }

        StringBuilder sbOutput = new();
        await BlazorRunTest(new BlazorRunOptions()
        {
            Config = config,
            Host = publish ? BlazorRunHost.WebServer : BlazorRunHost.DotnetRun,
            OnConsoleMessage = msg =>
            {
                sbOutput.AppendLine(msg.Text);
            }
        });

        string output = sbOutput.ToString();
        if (invariant)
        {
            Assert.Contains("Could not create es-ES culture", output);
            // For invariant, we get:
            //    Could not create es-ES culture: Argument_CultureNotSupportedInInvariantMode Arg_ParamName_Name, name
            //    Argument_CultureInvalidIdentifier, es-ES
            //  .. which is expected.
            //
            // Assert.Contains("es-ES is an invalid culture identifier.", output);
            Assert.Contains("CurrentCulture.NativeName: Invariant Language (Invariant Country)", output);
            Assert.DoesNotContain($"es-ES: Is-LCID-InvariantCulture:", output);
        }
        else
        {
            Assert.DoesNotContain("Could not create es-ES culture", output);
            Assert.DoesNotContain("invalid culture", output);
            Assert.DoesNotContain("CurrentCulture.NativeName: Invariant Language (Invariant Country)", output);
            Assert.Contains("es-ES: Is-LCID-InvariantCulture: False, NativeName: es (ES)", output);

            // ignoring the last line of the output which prints the current culture
        }
    }

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

    private static string s_invariantCultureMethodForBlazor = """
    @code {
        public int TestInvariantCulture()
        {
            // https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
            try
            {
                System.Globalization.CultureInfo culture = new ("es-ES", false);
                System.Console.WriteLine($"es-ES: Is-LCID-InvariantCulture: {culture.LCID == System.Globalization.CultureInfo.InvariantCulture.LCID}, NativeName: {culture.NativeName}");
            }
            catch (System.Globalization.CultureNotFoundException cnfe)
            {
                System.Console.WriteLine($"Could not create es-ES culture: {cnfe.Message}");
            }

            System.Console.WriteLine($"CurrentCulture.NativeName: {System.Globalization.CultureInfo.CurrentCulture.NativeName}");
            return 42;
        }
    }
    """;
}
