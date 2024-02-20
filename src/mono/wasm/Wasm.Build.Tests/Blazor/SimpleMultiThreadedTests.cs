// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Wasm.Build.Tests.Blazor;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests.MT.Blazor;

public class SimpleMultiThreadedTests : BlazorWasmTestBase
{
    public SimpleMultiThreadedTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    // dotnet-run needed for running with *build* so wwwroot has the index.html etc
    // [Theory]
    // [InlineData("Debug")]
    // [InlineData("Release")]
    // public async Task BlazorBuildRunTest(string config)
    // {
    //     string id = $"blazor_mt_{config}_{GetRandomId()}";
    //     string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

    //     AddItemsPropertiesToProject(projectFile, "<WasmEnableThreads>true</WasmEnableThreads>");
    //     BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack, RuntimeType: RuntimeType.MultiThreaded));
    //     // await BlazorRunForBuildWithDotnetRun(config);

    //     await BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files --web-server-use-cors --web-server-use-cop --web-server-use-https --timeout=15:00:00",
    //                          Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(config, forPublish: false), "..")));
    // }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [InlineData("Debug", false)]
    // [InlineData("Debug", true)]
    [InlineData("Release", false)]
    // [InlineData("Release", true)]
    public async Task BlazorPublishRunTest(string config, bool aot)
    {
        string id = $"blazor_mt_{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
        AddItemsPropertiesToProject(projectFile, "<WasmEnableThreads>true</WasmEnableThreads>");
        // if (aot)
        // AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");

        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(projectFile)!, "wwwroot", id + ".lib.module.js"),
            """
            export function onRuntimeReady({ runtimeBuildInfo }) {
                console.log('Runtime is ready: ' + JSON.stringify(runtimeBuildInfo));
                console.log(`WasmEnableThreads=${runtimeBuildInfo.wasmEnableThreads}`);
            }
            """
        );

        BlazorPublish(new BlazorBuildOptions(
            id,
            config,
            aot ? NativeFilesType.AOT
                : (config == "Release" ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack),
            RuntimeType: RuntimeVariant.MultiThreaded));

        bool hasEmittedWasmEnableThreads = false;
        StringBuilder errorOutput = new();
        await BlazorRunForPublishWithWebServer(
                runOptions: new BlazorRunOptions(
                    Config: config,
                    ExtraArgs: "--web-server-use-cors --web-server-use-cop",
                    OnConsoleMessage: (message) =>
                    {
                        if (message.Text.Contains("WasmEnableThreads=true"))
                            hasEmittedWasmEnableThreads = true;

                        if (message.Type == "error")
                            errorOutput.AppendLine(message.Text);
                    },
                    OnErrorMessage: (message) =>
                    {
                        errorOutput.AppendLine(message);
                    }));

        if (errorOutput.Length > 0)
            throw new XunitException($"Errors found in browser console output:\n{errorOutput}");

        if (!hasEmittedWasmEnableThreads)
            throw new XunitException($"The test didn't emit expected message 'WasmEnableThreads=true'");
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [InlineData("Release")]
    public async Task SwitchSingleAndMultiThreaded(string config)
    {
        string id = $"blazor_mt_{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

        // Build and run multithreaded
        _testOutput.WriteLine("Publish and run MT");
        await PublishAndRunAsync(id, config, true, "First MT");

        File.Delete(
            Directory
                .EnumerateFiles(_provider.FindBinFrameworkDir(config, true, DefaultTargetFramework), "dotnet.native.worker.*")
                .First()
        );

        // Build and run singlethreaded without clean
        _testOutput.WriteLine("Publish and run ST");
        await PublishAndRunAsync(id, config, false, "ST after MT");

        // Build and run multithreaded again
        _testOutput.WriteLine("Publish and run MT");
        await PublishAndRunAsync(id, config, false, "MT after ST");
    }

    private async Task PublishAndRunAsync(string id, string config, bool isMultiThreaded, string label) 
    {
        BlazorPublish(
            new BlazorBuildOptions(
                id,
                config,
                NativeFilesType.Relinked,
                RuntimeType: isMultiThreaded ? RuntimeVariant.MultiThreaded : RuntimeVariant.SingleThreaded,
                Label: label
            ), 
            isMultiThreaded ? new string[] { "-p:WasmEnableThreads=true"} : new string[0]
        );

        StringBuilder errorOutput = new();
        await BlazorRunForPublishWithWebServer(
            runOptions: new BlazorRunOptions(
                Config: config,
                ExtraArgs: isMultiThreaded ? "--web-server-use-cors --web-server-use-cop" : string.Empty,
                OnConsoleMessage: (message) =>
                {
                    if (message.Type == "error")
                        errorOutput.AppendLine(message.Text);
                },
                OnErrorMessage: (message) =>
                {
                    errorOutput.AppendLine(message);
                }
            )
        );

        if (errorOutput.Length > 0)
            throw new XunitException($"Errors found in browser console output:\n{errorOutput}");
    }
}
