// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class ConfigSrcTests : BuildTestBase
{
    public ConfigSrcTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
    {}

    // NOTE: port number determinizes dynamically, so could not generate absolute URI
    [Theory]
    [BuildAndRun(host: RunHost.V8 | RunHost.NodeJS)]
    public void ConfigSrcAbsolutePath(BuildArgs buildArgs, RunHost host, string id)
    {
        buildArgs = buildArgs with { ProjectName = $"configsrcabsolute_{buildArgs.Config}_{buildArgs.AOT}" };
        buildArgs = ExpandBuildArgs(buildArgs);

        BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                            DotnetWasmFromRuntimePack: !(buildArgs.AOT || buildArgs.Config == "Release")));

        string binDir = GetBinDir(baseDir: _projectDir!, config: buildArgs.Config);
        string bundleDir = Path.Combine(binDir, "AppBundle");
        string configSrc = Path.GetFullPath(Path.Combine(bundleDir, "mono-config.json"));

        RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id, extraXHarnessMonoArgs: $"--config-src={configSrc}");
    }
}
