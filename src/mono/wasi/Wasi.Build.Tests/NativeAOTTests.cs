// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasi.Build.Tests;

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

        string programCsContent = File.ReadAllText(Path.Combine(BuildEnvironment.TestAssetsPath, "SimpleMainWithArgs.cs"));
        programCsContent = programCsContent.Replace("return 42;", "return 0;");
        File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programCsContent);
        File.Delete(Path.Combine(_projectDir!, "runtimeconfig.template.json"));

        var buildArgs = ExpandBuildArgs(new BuildArgs(projectName, config, AOT: false, id, null));

        AddItemsPropertiesToProject(projectFile, extraProperties: 
            """
            <RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources);https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json</RestoreAdditionalProjectSources>
            <PublishAot>true</PublishAot>
            """
        );

        try
        {
            bool isWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            (_, string buildOutput) = BuildProject(
            buildArgs,
            id: id,
            new BuildProjectOptions(
                AssertAppBundle: false,
                CreateProject: false,
                Publish: true,
                TargetFramework: DefaultTargetFramework,
                ExpectSuccess: isWindowsPlatform
            )
        );

        if (isWindowsPlatform)
        {
            string outputDir = Path.Combine(_projectDir!, "bin", config, DefaultTargetFramework, BuildEnvironment.DefaultRuntimeIdentifier, "native");
            string outputFileName = $"{id}.wasm";

            Assert.True(File.Exists(Path.Combine(outputDir, outputFileName)), $"Expected {outputFileName} to exist in {outputDir}");
            Assert.Contains("Generating native code", buildOutput);

            using var runCommand = new ToolCommand(BuildEnvironment.GetExecutableName(@"wasmtime"), _testOutput)
                .WithWorkingDirectory(outputDir);

            var result = runCommand.Execute(outputFileName).EnsureSuccessful();
            Assert.Contains("Hello, Wasi Console!", result.Output);
        }
        else
        {
            Assert.Contains("NETSDK1204", buildOutput); // Ahead-of-time compilation is not supported on the current platform 'linux-x64'
        }
    }
        finally
        {
            _testOutput.WriteLine($"Content of {_nugetPackagesDir}");
            foreach (string file in Directory.EnumerateFiles(_nugetPackagesDir, "*", SearchOption.AllDirectories))
                _testOutput.WriteLine(file);
        }
    }
}
