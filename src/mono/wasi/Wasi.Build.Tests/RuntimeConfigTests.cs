// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class RuntimeConfigTests : BuildTestBase
{
    public RuntimeConfigTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/95345")]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingRuntimeConfigTemplateJson(bool singleFileBundle)
    {
        string config = "Release";
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        File.Delete(Path.Combine(_projectDir!, "runtimeconfig.template.json"));

        var buildArgs = new BuildArgs(projectName, config, AOT: true, ProjectFileContents: id, ExtraBuildArgs: null);
        buildArgs = ExpandBuildArgs(buildArgs);
        AddItemsPropertiesToProject(projectFile, singleFileBundle ? "<WasmSingleFileBundle>true</WasmSingleFileBundle>" : "");

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false,
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));

        string runArgs = $"run --no-silent --no-build -c {config}";
        new RunCommand(s_buildEnv, _testOutput, label: id)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput(runArgs)
                .EnsureSuccessful();
    }
}
