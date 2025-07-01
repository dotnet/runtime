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

    public static TheoryData<Configuration, string, bool> SettingDifferentFromValuesInRuntimePack()
    {
        TheoryData<Configuration, string, bool> data = new();

        var configs = new[] { Configuration.Debug, Configuration.Release };
        foreach (var defaultPair in PropertiesWithTriggerValues)
        {
            foreach (Configuration config in configs)
            {
                data.Add(config, $"<{defaultPair.propertyName}>{defaultPair.triggerValue}</{defaultPair.propertyName}>", true);
                data.Add(config, $"<{defaultPair.propertyName}>{!defaultPair.triggerValue}</{defaultPair.propertyName}>", false);
            }
        }

        return data;
    }

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(SettingDifferentFromValuesInRuntimePack))]
    public void WorkloadRequiredForBuild(Configuration config, string extraProperties, bool workloadNeeded)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: false);

    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(SettingDifferentFromValuesInRuntimePack))]
    public void WorkloadRequiredForPublish(Configuration config, string extraProperties, bool workloadNeeded)
        => CheckWorkloadRequired(config, extraProperties, workloadNeeded, publish: true);

    public static TheoryData<Configuration, bool, bool> InvariantGlobalizationTestData(bool publish)
    {
        TheoryData<Configuration, bool, bool> data = new();
        foreach (Configuration config in new[] { Configuration.Debug, Configuration.Release })
        {
            data.Add(config, /*invariant*/ true, /*publish*/ publish);
            data.Add(config, /*invariant*/ false, /*publish*/ publish);
        }
        return data;
    }

    [Theory, TestCategory("no-workload")]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public async Task DefaultTemplate_WithoutWorkload(Configuration config)
    {
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "blz_no_workload");
        BlazorBuild(info, config);
        await RunForBuildWithDotnetRun(new BlazorRunOptions(config));

        BlazorPublish(info, config, new PublishOptions(UseCache: false));
        await RunForPublishWithWebServer(new BlazorRunOptions(config));
    }

    [Theory, TestCategory("no-workload")]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void NativeRef_EmitsWarningBecauseItRequiresWorkload(Configuration config)
    {
        CommandResult res = PublishForRequiresWorkloadTest(config, extraItems: "<NativeFileReference Include=\"native-lib.o\" />");
        res.EnsureSuccessful();
        Assert.Matches("warning : .*but the native references won't be linked in", res.Output);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void AOT_FailsBecauseItRequiresWorkload(Configuration config)
    {
        CommandResult res = PublishForRequiresWorkloadTest(config, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");
        Assert.NotEqual(0, res.ExitCode);
        Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void AOT_And_NativeRef_FailBecauseTheyRequireWorkload(Configuration config)
    {
        CommandResult res = PublishForRequiresWorkloadTest(config,
                                extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>",
                                extraItems: "<NativeFileReference Include=\"native-lib.o\" />");

        Assert.NotEqual(0, res.ExitCode);
        Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
    }


    [Theory, TestCategory("no-workload")]
    [MemberData(nameof(InvariantGlobalizationTestData), parameters: /*publish*/ false)]
    [MemberData(nameof(InvariantGlobalizationTestData), parameters: /*publish*/ true)]
    public async Task WorkloadNotRequiredForInvariantGlobalization(Configuration config, bool invariant, bool publish)
    {
        string prefix = $"props_req_workload_{(publish ? "publish" : "build")}";
        string extraProperties = invariant ? $"<InvariantGlobalization>true</InvariantGlobalization>" : "";
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, prefix, extraProperties: extraProperties);
        string ccText = "currentCount++;";
        // UpdateFile throws if code that is to be replaced does not exist
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string>
        {
            { ccText, $"{ccText}\nTestInvariantCulture();" },
            { "private int currentCount = 0;", $"{s_invariantCultureMethodForBlazor}" }
        });
        string counterPath = Path.Combine(_projectDir, "Pages", "Counter.razor");
        string allText = File.ReadAllText(counterPath);
        _testOutput.WriteLine($"Updated counter.razor: {allText}");

        var globalizationMode = invariant ? GlobalizationMode.Invariant : GlobalizationMode.Sharded;
        _ = publish ?
            PublishProject(info, config, new PublishOptions(GlobalizationMode: globalizationMode)) :
            BuildProject(info, config, new BuildOptions(GlobalizationMode: globalizationMode));

        BlazorRunOptions runOptions = new(config);
        RunResult result = publish ? await RunForPublishWithWebServer(runOptions) : await RunForBuildWithDotnetRun(runOptions);

        if (invariant)
        {
            Assert.Contains(result.TestOutput, m => m.Contains("Could not create es-ES culture"));
            // For invariant, we get:
            //    Could not create es-ES culture: Argument_CultureNotSupportedInInvariantMode Arg_ParamName_Name, name
            //    Argument_CultureInvalidIdentifier, es-ES
            //  .. which is expected.
            //
            // Assert.Contains("es-ES is an invalid culture identifier.", output);
            Assert.Contains(result.TestOutput, m => m.Contains("CurrentCulture.NativeName: Invariant Language (Invariant Country)"));
            Assert.All(result.TestOutput, m => Assert.DoesNotContain("es-ES: Is-LCID-InvariantCulture", m));
        }
        else
        {
            Assert.All(result.TestOutput, m => Assert.DoesNotContain("Could not create es-ES culture", m));
            Assert.All(result.TestOutput, m => Assert.DoesNotContain("invalid culture", m));
            Assert.All(result.TestOutput, m => Assert.DoesNotContain("CurrentCulture.NativeName: Invariant Language (Invariant Country)", m));
            Assert.Contains(result.TestOutput, m => m.Contains("es-ES: Is-LCID-InvariantCulture: False"));
            Assert.Contains(result.TestOutput, m => m.Contains("NativeName: espa\u00F1ol (Espa\u00F1a)"));
            // ignoring the last line of the output which prints the current culture
        }
    }

    private CommandResult PublishForRequiresWorkloadTest(Configuration config, string extraItems="", string extraProperties="")
    {
        ProjectInfo info = CopyTestAsset(
            config, aot: false, TestAsset.BlazorBasicTestApp, "needs_workload", extraProperties: extraProperties, extraItems: extraItems);

        string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, info.ProjectName, $"{info.ProjectName}.binlog");
        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput);
        return cmd.WithWorkingDirectory(_projectDir)
                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                    .ExecuteWithCapturedOutput("publish",
                                                $"-bl:{publishLogPath}",
                                                $"-p:Configuration={config}");
    }

    private void CheckWorkloadRequired(Configuration config, string extraProperties, bool workloadNeeded, bool publish)
    {
        string prefix = $"props_req_workload_{(publish ? "publish" : "build")}";
        string insertAtEnd = @"<Target Name=""StopBuildBeforeCompile"" BeforeTargets=""Compile"">
                    <Error Text=""Stopping the build"" />
            </Target>";
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, prefix, extraProperties: extraProperties, insertAtEnd: insertAtEnd);
        (string _, string output) = publish ?
            PublishProject(info, config, new PublishOptions(ExpectSuccess: false)) :
            BuildProject(info, config, new BuildOptions(ExpectSuccess: false));

        if (workloadNeeded)
        {
            Assert.Contains("following workloads must be installed: wasm-tools", output);
            Assert.DoesNotContain("error : Stopping the build", output);
        }
        else
        {
            Assert.DoesNotContain("following workloads must be installed: wasm-tools", output);
            Assert.Contains("error : Stopping the build", output);
        }
    }

    private static string s_invariantCultureMethodForBlazor = """
        private int currentCount = 0;
        public int TestInvariantCulture()
        {
            // https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
            try
            {
                System.Globalization.CultureInfo culture = new ("es-ES", false);
                System.Console.WriteLine($"TestOutput -> es-ES: Is-LCID-InvariantCulture: {culture.LCID == System.Globalization.CultureInfo.InvariantCulture.LCID}, NativeName: {culture.NativeName}");
            }
            catch (System.Globalization.CultureNotFoundException cnfe)
            {
                System.Console.WriteLine($"TestOutput -> Could not create es-ES culture: {cnfe.Message}");
            }

            System.Console.WriteLine($"TestOutput -> CurrentCulture.NativeName: {System.Globalization.CultureInfo.CurrentCulture.NativeName}");
            return 42;
        }
    """;
}
