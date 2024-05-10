// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Wasm.Build.Tests.Blazor;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class NativeAOTTests : BlazorWasmTestBase
{
    public NativeAOTTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task PublishAndRun()
    {
        string config = "Release";
        string assetName = "NativeAOT";
        string id = $"browser_{config}_{GetRandomId()}";

        InitBlazorWasmProjectDir(id);
        Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, assetName), Path.Combine(_projectDir!));
        string projectName = Path.GetFileNameWithoutExtension(_projectDir!);

        (_, string buildOutput) = BuildTemplateProject(
            ExpandBuildArgs(new BuildArgs(projectName, config, AOT: false, id, null)),
            id: id,
            new BuildProjectOptions(
                AssertAppBundle: false,
                CreateProject: false,
                Publish: true,
                TargetFramework: DefaultTargetFramework
            )
        );

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string outputDir = Path.Combine(_projectDir!, "bin", config, DefaultTargetFramework, BuildEnvironment.DefaultRuntimeIdentifier, "native");

            List<string> consoleOutput = new();
            BlazorRunOptions blazorRunOptions = new(
                CheckCounter: false,
                Config: config,
                OnConsoleMessage: (_, message) => consoleOutput.Add(message.Text),
                Host: BlazorRunHost.WebServer
            );
            await BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files", outputDir, blazorRunOptions);

            Assert.True(consoleOutput.Contains("Hello, NativeAOT!"), $"Expected 'Hello, NativeAOT!' wasn't emitted by the test app. Output was: {String.Join(", ", consoleOutput)}");
        }
        else
        {
            Assert.Contains("NETSDK1204", buildOutput); // Ahead-of-time compilation is not supported on the current platform 'linux-x64'
        }
    }
}
