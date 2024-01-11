// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class MiscTests2 : BlazorWasmTestBase
{
    public MiscTests2(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task NativeRef_EmitsWarningBecauseItRequiresWorkloadAsync(string config)
    {
        CommandResult res = await PublishForRequiresWorkloadTestAsync(config, extraItems: "<NativeFileReference Include=\"native-lib.o\" />");
        res.EnsureSuccessful();
        Assert.Matches("warning : .*but the native references won't be linked in", res.Output);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task AOT_FailsBecauseItRequiresWorkloadAsync(string config)
    {
        CommandResult res = await PublishForRequiresWorkloadTestAsync(config, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");
        Assert.NotEqual(0, res.ExitCode);
        Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task AOT_And_NativeRef_FailBecauseTheyRequireWorkloadAsync(string config)
    {
        CommandResult res = await PublishForRequiresWorkloadTestAsync(config,
                                extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>",
                                extraItems: "<NativeFileReference Include=\"native-lib.o\" />");

        Assert.NotEqual(0, res.ExitCode);
        Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
    }

    private async Task<CommandResult> PublishForRequiresWorkloadTestAsync(string config, string extraItems="", string extraProperties="")
    {
        string id = $"needs_workload_{config}_{GetRandomId()}";
        await CreateBlazorWasmTemplateProjectAsync(id);

        AddItemsPropertiesToProject(Path.Combine(_projectDir!, $"{id}.csproj"),
                                    extraProperties: extraProperties,
                                    extraItems: extraItems);

        string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}.binlog");
        return await new DotNetCommand(s_buildEnv, _testOutput)
                        .WithWorkingDirectory(_projectDir!)
                        .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                        .ExecuteWithCapturedOutputAsync("publish",
                                                    $"-bl:{publishLogPath}",
                                                    $"-p:Configuration={config}");
    }
}
