// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class NativeAOTTests : BuildTestBase
{
    public NativeAOTTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public void PublishAndRun()
    {
        const string config = "Release";
        string id = $"nativeaot_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "SimpleMainWithArgs.cs"), Path.Combine(_projectDir!, "Program.cs"), true);
        File.Delete(Path.Combine(_projectDir!, "runtimeconfig.template.json"));

        var buildArgs = ExpandBuildArgs(new BuildArgs(projectName, config, AOT: false, id, null));

        AddItemsPropertiesToProject(projectFile, extraProperties: "<PublishAot>true</PublishAot>");

        (_, string buildOutput) = BuildProject(
            buildArgs,
            id: id,
            new BuildProjectOptions(
                AssertAppBundle: false,
                CreateProject: false,
                Publish: true,
                TargetFramework: DefaultTargetFramework
            )
        );

        string outputDir = Path.Combine(_projectDir!, "bin", config, DefaultTargetFramework, BuildEnvironment.DefaultRuntimeIdentifier, "native");
        string outputFileName = $"{id}.wasm";

        Assert.True(File.Exists(Path.Combine(outputDir, outputFileName)), $"Expected {outputFileName} to exist in {outputDir}");
        Assert.Contains("Generating native code", buildOutput);

        using var runCommand = new ToolCommand(BuildEnvironment.GetExecutableName(@"wasmtime"), _testOutput)
            .WithWorkingDirectory(outputDir);

        var result = runCommand.Execute(outputFileName).EnsureSuccessful();
        Assert.Contains("Hello, Wasi Console!", result.Output);
    }
}
