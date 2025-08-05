// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class BuildPublishTests : BuildTestBase
{
    public BuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("Release")]
    public void BuildInLongPathSingleFileBundle(string config)
    {
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole", projectParentDir: Path.Combine("reallyLongPath", "toProlongPathsToLinkedFiles", "andMakeTheResultingLinkingCommandExtremelyLong"));
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "SimpleMainWithArgs.cs"), Path.Combine(_projectDir!, "Program.cs"), true);

        var buildArgs = new BuildArgs(projectName, config, /*aot*/ false, id, null);
        buildArgs = ExpandBuildArgs(buildArgs);

        // only single file bundle linking exceeds the char limit
        AddItemsPropertiesToProject(projectFile, "<WasmSingleFileBundle>true</WasmSingleFileBundle>");

        (string projectDir, string buildOutput) = BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        Publish: false,
                        TargetFramework: BuildTestBase.DefaultTargetFramework));
    }
}
