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
    public void NetProjects_NativeReference(string config)
        => BuildNetProject(config, aot: false, @"<NativeFileReference Include=""native-lib.o"" />");

    public static TheoryData<string, bool> NetTestData = new()
    {
        { "Debug", /*aot*/ true },
        { "Debug", /*aot*/ false },
        { "Release", /*aot*/ true },
        { "Release", /*aot*/ false }
    };

    // FIXME: test for WasmBuildNative=true?
    [Theory]
    [MemberData(nameof(NetTestData))]
    public void NetProjects_AOT(string config, bool aot)
        => BuildNetProject(config, aot: aot);

    private void BuildNetProject(string config, bool aot, string? extraItems=null)
    {
        string id = $"BlazorApp_{config}_{aot}_{Path.GetRandomFileName()}";
        InitBlazorWasmProjectDir(id);

        string directoryBuildTargets = @"<Project>
        <Target Name=""PrintAllProjects"" BeforeTargets=""Build"">
                <Message Text=""** UsingBrowserRuntimeWorkload: '$(UsingBrowserRuntimeWorkload)'"" Importance=""High"" />
            </Target>
        </Project>";

        File.WriteAllText(Path.Combine(_projectDir!, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_projectDir!, "Directory.Build.targets"), directoryBuildTargets);

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "BlazorApp"), Path.Combine(_projectDir!));

        string projectFile = Path.Combine(_projectDir!, "BlazorApp.csproj");
        AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

        string publishLogPath = Path.Combine(logPath, $"{id}.binlog");
        CommandResult result = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!)
                                        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                        .ExecuteWithCapturedOutput("publish",
                                                                    $"-bl:{publishLogPath}",
                                                                    (aot ? "-p:RunAOTCompilation=true" : ""),
                                                                    $"-p:Configuration={config}");

        result.EnsureSuccessful();
        Assert.Contains("** UsingBrowserRuntimeWorkload: 'true'", result.Output);

        string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish: true, framework: "net7.0");
        AssertBlazorBootJson(config, isPublish: true, binFrameworkDir: binFrameworkDir);
        // dotnet.wasm here would be from nuget like:
        // /Users/radical/.nuget/packages/microsoft.netcore.app.runtime.browser-wasm/<tfm>/runtimes/browser-wasm/native/dotnet.wasm
    }
}
