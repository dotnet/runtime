// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
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
    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/100373")] // to be fixed by: "https://github.com/dotnet/aspnetcore/issues/54365"
    public async Task BlazorBuildRunTest(Configuration config)
    {
        string extraProperties = "<WasmEnableThreads>true</WasmEnableThreads>";
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "blazorwasm", extraProperties: extraProperties);
        bool isPublish = false;
        string frameworkDir = GetBlazorBinFrameworkDir(config, isPublish);
        BuildProject(info, config, new BuildOptions(RuntimeType: RuntimeVariant.MultiThreaded));
        // we wan to use "xharness wasm webserver" but from non-publish location
        string extraArgs = " --web-server-use-cors --web-server-use-cop --web-server-use-https --timeout=15:00:00";
        await RunForPublishWithWebServer(new BlazorRunOptions(config, ExtraArgs: extraArgs, CustomBundleDir: Path.Combine(frameworkDir, "..")));
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/100373")] // to be fixed by: "https://github.com/dotnet/aspnetcore/issues/54365"
    // [InlineData(Configuration.Debug, false)] // ActiveIssue https://github.com/dotnet/runtime/issues/98758
    // [InlineData(Configuration.Debug, true)]
    [InlineData(Configuration.Release, false)]
    // [InlineData(Configuration.Release, true)]
    public async Task BlazorPublishRunTest(Configuration config, bool aot)
    {
        string extraProperties = "<WasmEnableThreads>true</WasmEnableThreads>";
        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.BlazorBasicTestApp, "blazor_mt", extraProperties: extraProperties);
        // if (aot)
        // AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");

        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(info.ProjectFilePath)!, "wwwroot", info.ProjectName + ".lib.module.js"),
            """
            export function onRuntimeReady({ runtimeBuildInfo }) {
                console.log('Runtime is ready: ' + JSON.stringify(runtimeBuildInfo));
                console.log(`WasmEnableThreads=${runtimeBuildInfo.wasmEnableThreads}`);
            }
            """
        );

        BlazorPublish(info, config, new PublishOptions(RuntimeType: RuntimeVariant.MultiThreaded, AOT: aot));

        bool hasEmittedWasmEnableThreads = false;
        StringBuilder errorOutput = new();
        await RunForPublishWithWebServer(
                runOptions: new BlazorRunOptions(
                    Configuration: config,
                    ExtraArgs: "--web-server-use-cors --web-server-use-cop",
                    OnConsoleMessage: (type, message) =>
                    {
                        if (message.Contains("WasmEnableThreads=true"))
                            hasEmittedWasmEnableThreads = true;

                        if (type == "error")
                            errorOutput.AppendLine(message);
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
}
