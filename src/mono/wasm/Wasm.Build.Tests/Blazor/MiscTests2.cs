// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class MiscTests2 : BuildTestBase
{
    public MiscTests2(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void NativeRef_EmitsWarningBecauseItRequiresWorkload(string config)
    {
        CommandResult res = PublishForRequiresWorkloadTest(config, extraItems: "<NativeFileReference Include=\"native-lib.o\" />");
        res.EnsureSuccessful();
        Assert.Matches("warning : .*but the native references won't be linked in", res.Output);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void AOT_FailsBecauseItRequiresWorkload(string config)
    {
        CommandResult res = PublishForRequiresWorkloadTest(config, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");
        Assert.NotEqual(0, res.ExitCode);
        Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void AOT_And_NativeRef_FailBecauseTheyRequireWorkload(string config)
    {
        CommandResult res = PublishForRequiresWorkloadTest(config,
                                extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>",
                                extraItems: "<NativeFileReference Include=\"native-lib.o\" />");

        Assert.NotEqual(0, res.ExitCode);
        Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
    }

    private CommandResult PublishForRequiresWorkloadTest(string config, string extraItems="", string extraProperties="")
    {
        string id = $"needs_workload_{config}_{Path.GetRandomFileName()}";
        CreateBlazorWasmTemplateProject(id);

        AddItemsPropertiesToProject(Path.Combine(_projectDir!, $"{id}.csproj"),
                                    extraProperties: extraProperties,
                                    extraItems: extraItems);

        string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}.binlog");
        return new DotNetCommand(s_buildEnv, _testOutput)
                        .WithWorkingDirectory(_projectDir!)
                        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                        .ExecuteWithCapturedOutput("publish",
                                                    $"-bl:{publishLogPath}",
                                                    $"-p:Configuration={config}");
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void Net50Projects_NativeReference(string config)
        => BuildNet50Project(config, aot: false, expectError: true, @"<NativeFileReference Include=""native-lib.o"" />");

    public static TheoryData<string, bool, bool> Net50TestData = new()
    {
        { "Debug", /*aot*/ true, /*expectError*/ true },
        { "Debug", /*aot*/ false, /*expectError*/ false },
        { "Release", /*aot*/ true, /*expectError*/ true },
        { "Release", /*aot*/ false, /*expectError*/ false }
    };

    // FIXME: test for WasmBuildNative=true?
    [Theory]
    [MemberData(nameof(Net50TestData))]
    public void Net50Projects_AOT(string config, bool aot, bool expectError)
        => BuildNet50Project(config, aot: aot, expectError: expectError);

    private void BuildNet50Project(string config, bool aot, bool expectError, string? extraItems=null)
    {
        string id = $"Blazor_net50_{config}_{aot}_{Path.GetRandomFileName()}";
        InitBlazorWasmProjectDir(id);

        string directoryBuildTargets = @"<Project>
        <Target Name=""PrintAllProjects"" BeforeTargets=""Build"">
                <Message Text=""** UsingBrowserRuntimeWorkload: '$(UsingBrowserRuntimeWorkload)'"" Importance=""High"" />
            </Target>
        </Project>";

        File.WriteAllText(Path.Combine(_projectDir!, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_projectDir!, "Directory.Build.targets"), directoryBuildTargets);

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "Blazor_net50"), Path.Combine(_projectDir!));

        string projectFile = Path.Combine(_projectDir!, "Blazor_net50.csproj");
        AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

        string publishLogPath = Path.Combine(logPath, $"{id}.binlog");
        CommandResult result = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!)
                                        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                        .ExecuteWithCapturedOutput("publish",
                                                                    $"-bl:{publishLogPath}",
                                                                    (aot ? "-p:RunAOTCompilation=true" : ""),
                                                                    $"-p:Configuration={config}");

        if (expectError)
        {
            result.EnsureExitCode(1);
            Assert.Contains("are only supported for projects targeting net6.0+", result.Output);
        }
        else
        {
            result.EnsureSuccessful();
            Assert.Contains("** UsingBrowserRuntimeWorkload: 'false'", result.Output);

            string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish: true, framework: "net5.0");
            AssertBlazorBootJson(config, isPublish: true, isNet7AndBelow: true, binFrameworkDir: binFrameworkDir);
            // dotnet.wasm here would be from 5.0 nuget like:
            // /Users/radical/.nuget/packages/microsoft.netcore.app.runtime.browser-wasm/5.0.9/runtimes/browser-wasm/native/dotnet.wasm
        }
    }
}
