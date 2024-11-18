// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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

    private CommandResult PublishForRequiresWorkloadTest(Configuration config, string extraItems="", string extraProperties="")
    {
        ProjectInfo info = CopyTestAsset(
            config, aot: false, BasicTestApp, "needs_workload", extraProperties: extraProperties, extraItems: extraItems);

        string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, info.ProjectName, $"{info.ProjectName}.binlog");
        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput);
        return cmd.WithWorkingDirectory(_projectDir!)
                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                    .ExecuteWithCapturedOutput("publish",
                                                $"-bl:{publishLogPath}",
                                                $"-p:Configuration={config}");
    }
}
