// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.NET.Sdk.WebAssembly;
using Microsoft.NET.WebAssembly.Webcil;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class EventPipeDiagnosticsTests : BlazorWasmTestBase
{
    private static readonly string uploadPattern = "^[a-zA-Z0-9_]+\\.nettrace$";

    // Generous enough for a slow CI machine, but far below the Helix work item budget so a stuck
    // collection is reported as a test failure instead of killing the whole work item.
    private static readonly TimeSpan s_traceCollectionTimeout = TimeSpan.FromMinutes(3);

    public EventPipeDiagnosticsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }


    [Fact]
    [TestCategory("native"), TestCategory("mono")]
    public Task BlazorEventPipeTestWithCpuSamplesAOT() => BlazorEventPipeTestWithCpuSamplesCore(Configuration.Release, aot: true, readyToRun: false);

    [Theory]
    [InlineData(Configuration.Debug, false)]
    [InlineData(Configuration.Release, false)]
    public Task BlazorEventPipeTestWithCpuSamples(Configuration config, bool aot)
        => BlazorEventPipeTestWithCpuSamplesCore(config, aot, readyToRun: false);

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    [TestCategory("no-workload")]
    public Task BlazorEventPipeTestWithCpuSamplesReadyToRun(Configuration config)
        => BlazorEventPipeTestWithCpuSamplesCore(config, aot: false, readyToRun: true);

    private async Task BlazorEventPipeTestWithCpuSamplesCore(Configuration config, bool aot, bool readyToRun)
    {
        string extraProperties = $@"
                <WasmPerformanceInstrumentation>all</WasmPerformanceInstrumentation>
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmDebugLevel>0</WasmDebugLevel>
                <PublishReadyToRun>{(readyToRun ? "true" : "false")}</PublishReadyToRun>
                <PublishTrimmed>false</PublishTrimmed>
                <WBTDevServer>true</WBTDevServer>
            ";
        if (readyToRun)
            extraProperties += "<AllowUnsafeBlocks>true</AllowUnsafeBlocks>";

        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.BlazorBasicTestApp,
            readyToRun ? "blazor_cpu_samples_r2r" : "blazor_cpu_samples_interp",
            extraProperties: extraProperties,
            extraItems: $"""<WasmEnvironmentVariable Include="DOTNET_EventPipeThreadSamplingRate" Value="{int.MaxValue}" />""");

        UpdateCounterPage(useCpuSamplingTarget: true);
        if (readyToRun)
            AddCpuSamplingScenarios();

        string extraBuildArgs = readyToRun
            ? $"{GetR2RBuildArgs(config)} -p:UsingBrowserRuntimeWorkload=false"
            : string.Empty;
        BlazorPublish(info, config, new PublishOptions(
            UseCache: false,
            AssertAppBundle: false,
            ExtraMSBuildArgs: extraBuildArgs));

        async Task CollectCpuSamplesTest(IPage page)
        {
            await SetupCounterPage(page, "cpuprofile.nettrace", "globalThis.getDotnetRuntime(0).collectCpuSamples({ durationSeconds: 5.0 })", uploadTrace: false);
            await ClickAndCollect(page, Path.Combine(info.LogPath, "cpuprofile.nettrace"));
        }

        var runOptions = new BlazorRunOptions(
            Configuration: config,
            Test: CollectCpuSamplesTest,
            TimeoutSeconds: 60,
            CheckCounter: false);

        await RunForPublishWithWebServer(runOptions);

        bool sampledMethodFound = false;
        bool readyToRunMethodFound = false;
        bool reversePInvokeMethodFound = false;
        using (var source = TraceLog.OpenOrConvert(ConvertTrace(info, "cpuprofile.nettrace")))
        {
            sampledMethodFound = source.CallStacks.Any(stack => stack.CodeAddress.FullMethodName.StartsWith(
                "BlazorBasicTestApp.Pages.Counter.CpuSamplingTarget(", StringComparison.Ordinal) &&
                (!readyToRun || (stack.CodeAddress.Address & 0x80000000UL) != 0));
            if (!sampledMethodFound)
            {
                foreach (var stack in source.CallStacks)
                {
                    _testOutput.WriteLine($"Stack: {stack.CodeAddress.FullMethodName}");
                }
            }

            readyToRunMethodFound = source.CodeAddresses.Any(address => address.FullMethodName.StartsWith("System.Buffer.Memmove(", StringComparison.Ordinal));
            if (readyToRun)
            {
                reversePInvokeMethodFound = source.CallStacks.Any(stack => stack.CodeAddress.FullMethodName.StartsWith(
                    "BlazorBasicTestApp.CpuSamplingScenarios.ReversePInvokeTarget(", StringComparison.Ordinal) &&
                    (stack.CodeAddress.Address & 0x80000000UL) != 0);
            }
        }

        Assert.True(sampledMethodFound, readyToRun
            ? "The cpuprofile.nettrace should contain a Wasm virtual-IP stack frame for the R2R 'Counter.CpuSamplingTarget' method"
            : "The cpuprofile.nettrace should contain stack frames for the 'Counter.CpuSamplingTarget' method");
        if (readyToRun)
            Assert.True(reversePInvokeMethodFound, "The cpuprofile.nettrace should contain a Wasm virtual-IP stack frame for the R2R reverse P/Invoke entry samplepoint");
        if (BuildTestBase.IsCoreClrRuntime && !readyToRun)
        {
            Assert.True(readyToRunMethodFound, "The cpuprofile.nettrace should contain rundown information for the ReadyToRun 'System.Buffer.Memmove' method");
        }
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [InlineData(Configuration.Debug, false, true)]
    [InlineData(Configuration.Release, false, true)]
    [InlineData(Configuration.Release, false, false)]
    [TestCategory("no-workload"), TestCategory("coreclr")]
    public async Task BlazorEventPipeTestWithInterpPgo(Configuration config, bool aot, bool instrumentInterpreter)
    {
        // Publishing without ReadyToRun isolates ordinary interpreter fallback from compiled bodies.
        // The SDK instrumentation property remains reserved for ReadyToRun producer publishes.
        string extraProperties = @"
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmDebugLevel>0</WasmDebugLevel>
                <WasmEnableThreads>false</WasmEnableThreads>
                <PublishReadyToRun>false</PublishReadyToRun>
                <PublishTrimmed>false</PublishTrimmed>
            ";
        string extraItems = instrumentInterpreter ? """
            <WasmEnvironmentVariable Include="DOTNET_InterpPGO" Value="1" />
            <WasmEnvironmentVariable Include="DOTNET_InterpPGOMethods" Value="*Counter:IncrementCount" />
            """ : "";

        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.BlazorBasicTestApp, "blazor_interp_pgo", extraProperties: extraProperties, extraItems: extraItems);

        UpdateCounterPage();

        BlazorPublish(info, config, new PublishOptions(
            UseCache: false,
            AssertAppBundle: false,
            ExtraMSBuildArgs: "-p:UsingBrowserRuntimeWorkload=false"));

        async Task CollectInterpPgoTest(IPage page)
        {
            Assert.Equal(instrumentInterpreter ? "1" : "", await page.EvaluateAsync<string>(
                "globalThis.getDotnetRuntime(0).getConfig().environmentVariables.DOTNET_InterpPGO ?? ''"));
            await SetupCounterPage(page, "pgo.nettrace",
                "globalThis.getDotnetRuntime(0).collectPgoTrace({ durationSeconds: 5.0 })", uploadTrace: false);
            if (!instrumentInterpreter)
                await page.Locator("text=\"Click me\"").ClickAsync();
            await ClickAndCollect(page, Path.Combine(info.LogPath, "pgo.nettrace"));
            Assert.Equal($"Current count: {(instrumentInterpreter ? 5 : 6)}",
                await page.Locator("p[role='status']").InnerHTMLAsync());
        }

        await RunForPublishWithWebServer(new BlazorRunOptions(
            Configuration: config,
            Test: CollectInterpPgoTest,
            TimeoutSeconds: 60,
            CheckCounter: false
        ));

        ValidateInterpPgoTrace(info, config, "pgo.nettrace", "IncrementCount", instrumentInterpreter);
    }

    [ConditionalFact(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [TestCategory("no-workload"), TestCategory("coreclr")]
    public async Task BlazorPgoTraceSkipsIncompatibleMethodProfiles()
    {
        const Configuration Config = Configuration.Debug;
        const string CompilerLog = "pgo-mixed.jit.txt";
        ProjectInfo info = CopyTestAsset(Config, aot: false, TestAsset.BlazorBasicTestApp, "blazor_mixed_pgo",
            extraProperties: $"""
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmEnableThreads>false</WasmEnableThreads>
                <PublishReadyToRun>true</PublishReadyToRun>
                <PublishReadyToRunComposite>true</PublishReadyToRunComposite>
                <PublishTrimmed>false</PublishTrimmed>
                <PublishReadyToRunPgoInstrumentation>true</PublishReadyToRunPgoInstrumentation>
                <PublishReadyToRunCrossgen2ExtraArgs>$(PublishReadyToRunCrossgen2ExtraArgs);--optimize;--parallelism:1;--codegenopt:JitDisasm=*WasmPgoScenarios:*;--codegenopt:JitStdOutFile=&quot;$(MSBuildProjectDirectory)/{CompilerLog}&quot;</PublishReadyToRunCrossgen2ExtraArgs>
                """,
            extraItems: """
                <WasmEnvironmentVariable Include="DOTNET_ReadyToRunExcludeList" Value="MixedProfileLibrary" />
                <WasmEnvironmentVariable Include="DOTNET_InterpPGO" Value="1" />
                <WasmEnvironmentVariable Include="DOTNET_InterpPGOMethods" Value="*WasmPgoScenarios:MixedProfileTarget" />
                <Compile Remove="MixedProfileLibrary\**\*.cs" />
                <ProjectReference Include="MixedProfileLibrary\MixedProfileLibrary.csproj" />
                """);

        string library = Path.Combine(_projectDir, "MixedProfileLibrary");
        Directory.CreateDirectory(library);
        File.WriteAllText(Path.Combine(library, "MixedProfileLibrary.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{DefaultTargetFrameworkForBlazor}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(library, "WasmPgoScenarios.cs"), """
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.

            using System.Runtime.CompilerServices;

            namespace MixedProfileLibrary;

            public static class WasmPgoScenarios
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int MixedProfileTarget(int value) => value * 7;
            }
            """);
        File.WriteAllText(Path.Combine(_projectDir, "WasmPgoScenarios.cs"), """
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.

            using System.Runtime.CompilerServices;

            namespace BlazorBasicTestApp;

            internal static class WasmPgoScenarios
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int UnrelatedProfileTarget(int value) => value + 1;

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int MixedProfileCaller(int value) => MixedProfileLibrary.WasmPgoScenarios.MixedProfileTarget(value);
            }
            """);
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string>
        {
            ["currentCount++;"] = """
                int input = BlazorBasicTestApp.WasmPgoScenarios.UnrelatedProfileTarget(currentCount);
                int inlined = BlazorBasicTestApp.WasmPgoScenarios.MixedProfileCaller(input);
                var method = typeof(MixedProfileLibrary.WasmPgoScenarios).GetMethod(nameof(MixedProfileLibrary.WasmPgoScenarios.MixedProfileTarget))!;
                currentCount = inlined + (int)method.Invoke(null, new object[] { input })!;
                """
        });

        BlazorPublish(info, Config, new PublishOptions(UseCache: false, AssertAppBundle: false,
            ExtraMSBuildArgs: GetPgoBuildArgs(Config)));
        SaveCompilerLog(info, CompilerLog);
        AssertCompilerInlines(CompilerLog, "MixedProfileCaller", 1);

        string tracePath = Path.Combine(info.LogPath, "mixed.nettrace");
        bool skippedIncompatibleProfile = false;
        await RunForPublishWithWebServer(new BlazorRunOptions(Config, CheckCounter: false, TimeoutSeconds: 60,
            OnConsoleMessage: (_, message) => skippedIncompatibleProfile |=
                message.Contains("Skipping incompatible PGO instrumentation schemas", StringComparison.Ordinal),
            Test: async page =>
            {
                await SetupCounterPage(page, "mixed.nettrace",
                    "globalThis.getDotnetRuntime(0).collectPgoTrace({ durationSeconds: 5.0 })", uploadTrace: false);
                await ClickAndCollect(page, tracePath);
            }));

        using var source = new EventPipeEventSource(tracePath);
        long? mixedMethodId = null;
        bool mixedProfileExported = false;
        bool unrelatedProfileExported = false;
        bool capturedMixedMethodIdentity = false;
        source.Clr.All += data =>
        {
            if (data is MethodJittingStartedTraceData { MethodName: "MixedProfileTarget" } jit)
                mixedMethodId = jit.MethodID;
            else if (data is JitInstrumentationDataVerboseTraceData profile)
            {
                mixedProfileExported |= profile.MethodName == "MixedProfileTarget";
                unrelatedProfileExported |= profile.MethodName == "UnrelatedProfileTarget";
            }
            else if (data is MethodDetailsTraceData details && unrelatedProfileExported)
                capturedMixedMethodIdentity |= details.MethodID == mixedMethodId;
        };
        source.Process();
        Assert.NotNull(mixedMethodId);
        Assert.False(mixedProfileExported);
        Assert.True(unrelatedProfileExported, "The unrelated method's profile was not exported.");
        Assert.True(capturedMixedMethodIdentity, "The mixed method's identity was not captured after exporting profiles.");
        Assert.True(skippedIncompatibleProfile, "The runtime did not report the incompatible profile.");
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [InlineData(Configuration.Debug, false, false)]
    [InlineData(Configuration.Release, false, true)]
    [InlineData(Configuration.Release, true, false)]
    [TestCategory("no-workload"), TestCategory("coreclr")]
    public async Task BlazorEventPipeTestWithSparseEdgePgoReadyToRun(
        Configuration config, bool cpuSampling, bool composite)
    {
        // Optimize crossgen2, not Csc: both publishes must retain the same configuration-specific IL.
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "blazor_basic_block_pgo",
            extraProperties: $"""
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmDebugLevel>0</WasmDebugLevel>
                <WasmEnableThreads>false</WasmEnableThreads>
                <PublishReadyToRun>true</PublishReadyToRun>
                <PublishReadyToRunComposite>{composite.ToString().ToLowerInvariant()}</PublishReadyToRunComposite>
                <PublishTrimmed>false</PublishTrimmed>
                <PublishReadyToRunPgoInstrumentation Condition="'$(WasmBuildTestConsumePgo)' != 'true'">true</PublishReadyToRunPgoInstrumentation>
                <PublishReadyToRunPgoInstrumentation Condition="'$(WasmBuildTestConsumePgo)' == 'true'">false</PublishReadyToRunPgoInstrumentation>
                <WasmPerformanceInstrumentation Condition="'$(WasmBuildTestConsumePgo)' != 'true'">{(cpuSampling ? "all" : "")}</WasmPerformanceInstrumentation>
                <_WasmBuildTestCompilerLogPath>$([System.IO.Path]::Combine('$(MSBuildProjectDirectory)', '$(WasmBuildTestCompilerLog)'))</_WasmBuildTestCompilerLogPath>
                <PublishReadyToRunCrossgen2ExtraArgs>$(PublishReadyToRunCrossgen2ExtraArgs);--optimize;--parallelism:1;--codegenopt:JitDisasm=*WasmPgoScenarios:*;--codegenopt:JitStdOutFile=&quot;$(_WasmBuildTestCompilerLogPath)&quot;</PublishReadyToRunCrossgen2ExtraArgs>
                """,
            extraItems: """
                <PublishReadyToRunPgoFiles Include="$(WasmBuildTestReadyToRunProfile)" Condition="'$(WasmBuildTestReadyToRunProfile)' != ''" />
                <WasmEnvironmentVariable Include="DOTNET_EventPipeThreadSamplingRate" Value="2147483647" />
                <Compile Remove="FallbackLibrary\**\*.cs" />
                <ProjectReference Include="FallbackLibrary\FallbackLibrary.csproj" />
                <PublishReadyToRunExclude Include="FallbackLibrary.dll" Condition="'$(WasmBuildTestConsumePgo)' != 'true' and '$(PublishReadyToRunComposite)' != 'true'" />
                """);

        AddBasicBlockPgoScenarios();
        string fallbackDirectory = Path.Combine(_projectDir, "FallbackLibrary");
        Directory.CreateDirectory(fallbackDirectory);
        File.WriteAllText(Path.Combine(fallbackDirectory, "FallbackLibrary.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{DefaultTargetFrameworkForBlazor}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(fallbackDirectory, "Fallback.cs"), """
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.

            using System.Runtime.CompilerServices;

            namespace FallbackLibrary;

            public static class WasmPgoScenarios
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int InterpOnly(int value) => value * 7;
            }
            """);
        File.AppendAllText(Path.Combine(_projectDir, "Pages", "Counter.razor"), """

            @code {
                protected override void OnInitialized()
                {
                    if (FallbackLibrary.WasmPgoScenarios.InterpOnly(6) != 42)
                        throw new System.Exception("Fallback method returned an unexpected result");
                }
            }
            """);
        string buildArgs = GetPgoBuildArgs(config);
        (_, string buildOutput) = BlazorPublish(info, config, new PublishOptions(
            UseCache: false, AssertAppBundle: false,
            ExtraMSBuildArgs: $"{buildArgs} -p:WasmBuildTestCompilerLog=pgo-instrumented.jit.txt"));
        SaveCompilerLog(info, "pgo-instrumented.jit.txt");
        string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
        AssetsData producerAssets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
        string unlistedArtifact = Path.Combine(frameworkDir, "unlisted-compiler-artifact.wasm");
        File.Copy(Path.Combine(frameworkDir, producerAssets.assembly[0].name), unlistedArtifact);
        Dictionary<string, Guid> producerMvids;
        try
        {
            producerMvids = GetPublishedModuleMvids(config);
        }
        finally
        {
            File.Delete(unlistedArtifact);
        }

        if (config == Configuration.Release)
        {
            AssertCompilerInlines("pgo-instrumented.jit.txt", "DirectCalls", 2);
            AssertCompilerInlines("pgo-instrumented.jit.txt", "GenericCalls", 3);
            AssertCompilerInlines("pgo-instrumented.jit.txt", "FoldedCalls", 1);
        }

        string tracePath = Path.Combine(info.LogPath, "basic-block.nettrace");
        await RunForPublishWithWebServer(new BlazorRunOptions(config, CheckCounter: false, TimeoutSeconds: 60,
            Test: async page =>
            {
                Assert.Equal("", await page.EvaluateAsync<string>(
                    "globalThis.getDotnetRuntime(0).getConfig().environmentVariables.DOTNET_InterpPGO ?? ''"));
                Assert.Equal(cpuSampling ? "*" : "", await page.EvaluateAsync<string>(
                    "globalThis.getDotnetRuntime(0).getConfig().environmentVariables.DOTNET_WasmPerformanceInstrumentation ?? ''"));
                await SetupCounterPage(page, "basic-block.nettrace",
                    "globalThis.getDotnetRuntime(0).collectPgoTrace({ durationSeconds: 5.0 })", uploadTrace: false);
                await ClickAndCollect(page, tracePath);
                Assert.Equal("Current count: 159", await page.Locator("p[role='status']").InnerHTMLAsync());
            }));

        string mibcPath = CreateBasicBlockPgoProfile(info, config, buildOutput, tracePath, producerMvids,
            interpretedFallback: !composite);
        if (cpuSampling)
        {
            string cpuTrace = Path.Combine(info.LogPath, "basic-block-cpu.nettrace");
            await RunForPublishWithWebServer(new BlazorRunOptions(config, CheckCounter: false, TimeoutSeconds: 60,
                Test: async page =>
                {
                    await SetupCounterPage(page, "basic-block-cpu.nettrace",
                        "globalThis.getDotnetRuntime(0).collectCpuSamples({ durationSeconds: 5.0 })", uploadTrace: false);
                    await ClickAndCollect(page, cpuTrace);
                }));
            using var source = TraceLog.OpenOrConvert(ConvertTrace(info, "basic-block-cpu.nettrace"));
            Assert.Contains(source.CallStacks, stack =>
                stack.CodeAddress.FullMethodName.StartsWith("BlazorBasicTestApp.WasmPgoScenarios.CpuSamplingTarget(", StringComparison.Ordinal) &&
                (stack.CodeAddress.Address & 0x80000000UL) != 0);
        }

        // Fingerprinted producer assets must not mask an incremental consumer publish.
        Directory.Delete(GetBlazorBinFrameworkDir(config, forPublish: true), recursive: true);
        BlazorPublish(info, config, new PublishOptions(
            UseCache: false, AssertAppBundle: false, Label: "consume-basic-block",
            ExtraMSBuildArgs: $"{buildArgs} -p:WasmBuildTestConsumePgo=true -p:WasmBuildTestCompilerLog=pgo-consumed.jit.txt -p:WasmBuildTestReadyToRunProfile=\"{mibcPath}\""));
        SaveCompilerLog(info, "pgo-consumed.jit.txt");
        Dictionary<string, Guid> consumerMvids = GetPublishedModuleMvids(config);
        Assert.Equal(
            producerMvids.OrderBy(module => module.Key, StringComparer.Ordinal).ToArray(),
            consumerMvids.OrderBy(module => module.Key, StringComparer.Ordinal).ToArray());
        AssertMeasuredPgoCount("Helper", 15);
        AssertMeasuredPgoCount("EntryAndBackedge", 2);
        AssertMeasuredPgoCount("IlZeroBackedge", 1);
        AssertMeasuredPgoCount("SwitchEdges", 10);
        AssertMeasuredPgoCount("ExceptionEdges", 2);
        AssertCompilerInlines("pgo-consumed.jit.txt", "GenericCalls", 3, withPgo: 3);
        AssertCompilerInlines("pgo-consumed.jit.txt", "DirectCalls", 2, withPgo: 2);
        AssertCompilerInlines("pgo-consumed.jit.txt", "FoldedCalls", 1, withPgo: 1);
        Assert.Contains("; ReadyToRun compilation", GetCompilerMethodListing("pgo-consumed.jit.txt", "InterpOnly"));
        await RunForPublishWithWebServer(new BlazorRunOptions(config, CheckCounter: false, TimeoutSeconds: 60,
            Test: async page =>
            {
                await page.Locator("text=Counter").ClickAsync();
                for (int i = 0; i < 5; i++)
                    await page.Locator("text=\"Click me\"").ClickAsync();
                Assert.Equal("Current count: 159", await page.Locator("p[role='status']").InnerHTMLAsync());
                await page.EvaluateAsync("console.log('WASM EXIT 0')");
            }));
    }

    [ConditionalFact(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [TestCategory("no-workload"), TestCategory("coreclr")]
    public async Task BlazorReadyToRunTypeProfileRoundtrip()
    {
        const Configuration Config = Configuration.Debug;
        const string CompilerLog = "cast-profile.jit.txt";
        ProjectInfo info = CopyTestAsset(Config, aot: false, TestAsset.BlazorBasicTestApp, "blazor_cast_pgo",
            extraProperties: $"""
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmEnableThreads>false</WasmEnableThreads>
                <PublishReadyToRun>true</PublishReadyToRun>
                <PublishReadyToRunComposite>false</PublishReadyToRunComposite>
                <PublishTrimmed>false</PublishTrimmed>
                <PublishReadyToRunPgoInstrumentation Condition="'$(WasmBuildTestCastProfile)' == ''">true</PublishReadyToRunPgoInstrumentation>
                <PublishReadyToRunCrossgen2ExtraArgs>$(PublishReadyToRunCrossgen2ExtraArgs);--optimize;--parallelism:1;--codegenopt:JitDisasm=*WasmPgoScenarios:Profiled*;--codegenopt:JitStdOutFile=&quot;$(MSBuildProjectDirectory)/{CompilerLog}&quot;</PublishReadyToRunCrossgen2ExtraArgs>
                """,
            extraItems: """
                <PublishReadyToRunPgoFiles Include="$(WasmBuildTestCastProfile)" Condition="'$(WasmBuildTestCastProfile)' != ''" />
                """);

        File.WriteAllText(Path.Combine(_projectDir, "WasmPgoScenarios.cs"), """
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.

            using System;
            using System.Runtime.CompilerServices;

            namespace BlazorBasicTestApp;

            internal static class WasmPgoScenarios
            {
                private interface ITarget { }
                private sealed class CastHot : ITarget { }
                private sealed class CastCold : ITarget { }

                private interface IReceiver { int Run(); }
                private abstract class Receiver : IReceiver { public abstract int Run(); }
                private sealed class ReceiverHot : Receiver
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    public override int Run() => 10;
                }
                private sealed class ReceiverCold : Receiver
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    public override int Run() => 20;
                }

                private sealed class DelegateReceiver
                {
                    private readonly int _offset;

                    public DelegateReceiver(int offset) => _offset = offset;

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public int DelegateHot(int value) => _offset + value + 731;

                    public int DelegateCold(int value) => _offset - value;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static ITarget? ProfiledCast(object? input) => input as ITarget;

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static int ProfiledInterface(IReceiver input) => input.Run() + 1;

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static int ProfiledVirtual(Receiver input) => input.Run() + 1;

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static int ProfiledDelegate(Func<int, int> target, int value) => target(value) ^ 1;

                public static void CheckTypes(int iteration)
                {
                    bool hot = iteration < 4;
                    Receiver receiver = hot ? new ReceiverHot() : new ReceiverCold();
                    int expected = hot ? 11 : 21;
                    if (ProfiledInterface(receiver) != expected || ProfiledVirtual(receiver) != expected)
                        throw new Exception("Dispatch returned the wrong result.");

                    var target = new DelegateReceiver(200);
                    Func<int, int> callback = hot ? target.DelegateHot : target.DelegateCold;
                    if (ProfiledDelegate(callback, iteration) != ((hot ? 931 + iteration : 200 - iteration) ^ 1))
                        throw new Exception("Delegate dispatch returned the wrong result.");

                    if (iteration >= 5)
                    {
                        if (ProfiledCast(null) is not null || ProfiledCast(new object()) is not null)
                            throw new Exception("Cast did not preserve the null or miss path.");

                        Func<int, int> multicast = target.DelegateHot;
                        multicast += target.DelegateCold;
                        if (ProfiledDelegate(multicast, iteration) != ((200 - iteration) ^ 1) ||
                            ProfiledDelegate(static value => 2 * value, iteration) != (2 * iteration ^ 1))
                            throw new Exception("Delegate dispatch did not preserve the multicast or static fallback.");

                        try
                        {
                            ProfiledDelegate(null!, iteration);
                            throw new Exception("A null delegate did not throw.");
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                    else
                    {
                        object input = hot ? new CastHot() : new CastCold();
                        if (!ReferenceEquals(input, ProfiledCast(input)))
                            throw new Exception("Cast did not return the input object.");
                    }
                }
            }
            """);
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string>
        {
            ["currentCount++;"] = "BlazorBasicTestApp.WasmPgoScenarios.CheckTypes(currentCount++);",
        });

        string[] profiledMethods = ["ProfiledCast", "ProfiledInterface", "ProfiledVirtual"];
        string buildArgs = GetPgoBuildArgs(Config);
        (_, string buildOutput) = BlazorPublish(info, Config, new PublishOptions(UseCache: false, AssertAppBundle: false,
            ExtraMSBuildArgs: buildArgs));
        SaveCompilerLog(info, CompilerLog);
        foreach (string method in profiledMethods)
            Assert.Contains("CORINFO_HELP_CLASSPROFILE64", GetCompilerMethodListing(CompilerLog, method));
        Assert.Contains("CORINFO_HELP_DELEGATEPROFILE64", GetCompilerMethodListing(CompilerLog, "ProfiledDelegate"));
        string referenceDir = CopyPgoReferences(info, Config, buildOutput, GetPublishedModuleMvids(Config), "cast-references");

        string tracePath = Path.Combine(info.LogPath, "cast.nettrace");
        await RunForPublishWithWebServer(new BlazorRunOptions(Config, CheckCounter: false, TimeoutSeconds: 60,
            Test: async page =>
            {
                await SetupCounterPage(page, "cast.nettrace",
                    "globalThis.getDotnetRuntime(0).collectPgoTrace({ durationSeconds: 5.0 })", uploadTrace: false);
                await ClickAndCollect(page, tracePath);
                Assert.Equal("Current count: 5", await page.Locator("p[role='status']").InnerHTMLAsync());
            }));

        string tool = Path.Combine(AppContext.BaseDirectory, "dotnet-pgo", "dotnet-pgo.dll");
        string profile = Path.Combine(info.LogPath, "cast.mibc");
        string dump = Path.Combine(info.LogPath, "cast.json");
        using (var command = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
            command.ExecuteWithCapturedOutput($"exec \"{tool}\" create-mibc --trace \"{tracePath}\" --reference \"{Path.Combine(referenceDir, "*.dll")}\" --output \"{profile}\"").EnsureSuccessful();
        using (var command = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
            command.ExecuteWithCapturedOutput($"exec \"{tool}\" dump --input \"{profile}\" --output \"{dump}\"").EnsureSuccessful();
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(dump));
        foreach (string name in profiledMethods)
        {
            JsonElement method = Assert.Single(doc.RootElement.GetProperty("Methods").EnumerateArray(),
                method => method.GetProperty("Method").GetString()!.Contains($".{name}(", StringComparison.Ordinal));
            JsonElement[] schema = method.GetProperty("InstrumentationData").EnumerateArray().ToArray();
            JsonElement count = Assert.Single(schema, record => record.GetProperty("InstrumentationKind").GetString() == "HandleHistogramLongCount");
            JsonElement types = Assert.Single(schema, record => record.GetProperty("InstrumentationKind").GetString() == "HandleHistogramTypes");
            Assert.Equal(5, count.GetProperty("Data").GetInt64());
            Assert.Equal(count.GetProperty("ILOffset").GetInt32(), types.GetProperty("ILOffset").GetInt32());
            string[] samples = types.GetProperty("Data").EnumerateArray().Select(value => value.GetString()!).ToArray();
            Assert.Equal(5, samples.Length);
            string typePrefix = name == "ProfiledCast" ? "Cast" : "Receiver";
            Assert.Equal(4, samples.Count(value => value.EndsWith(typePrefix + "Hot", StringComparison.Ordinal)));
            Assert.Single(samples, value => value.EndsWith(typePrefix + "Cold", StringComparison.Ordinal));
        }

        JsonElement delegateMethod = Assert.Single(doc.RootElement.GetProperty("Methods").EnumerateArray(),
            method => method.GetProperty("Method").GetString()!.Contains(".ProfiledDelegate(", StringComparison.Ordinal));
        JsonElement[] delegateSchema = delegateMethod.GetProperty("InstrumentationData").EnumerateArray().ToArray();
        JsonElement delegateCount = Assert.Single(delegateSchema, record => record.GetProperty("InstrumentationKind").GetString() == "HandleHistogramLongCount");
        JsonElement delegateTargets = Assert.Single(delegateSchema, record => record.GetProperty("InstrumentationKind").GetString() == "HandleHistogramMethods");
        Assert.Equal(5, delegateCount.GetProperty("Data").GetInt64());
        Assert.Equal(delegateCount.GetProperty("ILOffset").GetInt32(), delegateTargets.GetProperty("ILOffset").GetInt32());
        string[] targets = delegateTargets.GetProperty("Data").EnumerateArray().Select(value => value.GetString()!).ToArray();
        Assert.Equal(5, targets.Length);
        Assert.Equal(4, targets.Count(value => value.Contains(".DelegateHot(", StringComparison.Ordinal)));
        Assert.Single(targets, value => value.Contains(".DelegateCold(", StringComparison.Ordinal));

        Directory.Delete(GetBlazorBinFrameworkDir(Config, forPublish: true), recursive: true);
        File.Delete(Path.Combine(_projectDir, CompilerLog));
        BlazorPublish(info, Config, new PublishOptions(UseCache: false, AssertAppBundle: false, Label: "consume-cast-profile",
            ExtraMSBuildArgs: $"{buildArgs} -p:WasmBuildTestCastProfile=\"{profile}\""));
        File.Copy(Path.Combine(_projectDir, CompilerLog), Path.Combine(info.LogPath, "cast-consumed.jit.txt"));
        string listing = GetCompilerMethodListing(CompilerLog, "ProfiledCast");
        Assert.Contains("; ReadyToRun compilation", listing);
        Assert.DoesNotContain("CORINFO_HELP_CLASSPROFILE64", listing);
        Assert.Matches(@"(?m)^\s+i32\.eq\s*$", listing);
        Assert.Contains("CORINFO_HELP_ISINSTANCEOFANY", listing);
        foreach (string name in profiledMethods.Skip(1))
        {
            string dispatch = GetCompilerMethodListing(CompilerLog, name);
            Assert.Contains("; ReadyToRun compilation", dispatch);
            Assert.DoesNotContain("CORINFO_HELP_CLASSPROFILE64", dispatch);
            Assert.Matches(@"(?m)^\s+i32\.(eq|ne)\s*$", dispatch);
            Assert.Matches(@"\b(?:deferred\.call|call(?:_indirect)?)\b[^\r\n]*ReceiverHot:Run", dispatch);
            Assert.Contains(name == "ProfiledInterface" ? "CORINFO_HELP_VIRTUAL_FUNC_PTR" : "+Receiver:Run", dispatch);
        }
        string delegateDispatch = GetCompilerMethodListing(CompilerLog, "ProfiledDelegate");
        Assert.Contains("; ReadyToRun compilation", delegateDispatch);
        Assert.DoesNotContain("CORINFO_HELP_DELEGATEPROFILE64", delegateDispatch);
        Assert.Matches(@"(?m)^\s+i32\.(eq|ne)\s*$", delegateDispatch);
        Assert.Matches(@"(?m)\bi32\.const 731[ \t]*\r?$", delegateDispatch);
        Assert.Contains("call_indirect", delegateDispatch);
        await RunForPublishWithWebServer(new BlazorRunOptions(Config, CheckCounter: false, TimeoutSeconds: 60,
            Test: async page =>
            {
                await page.Locator("text=Counter").ClickAsync();
                for (int i = 0; i < 6; i++)
                    await page.Locator("text=\"Click me\"").ClickAsync();
                Assert.Equal("Current count: 6", await page.Locator("p[role='status']").InnerHTMLAsync());
                await page.EvaluateAsync("console.log('WASM EXIT 0')");
            }));
    }

    private string GetPgoBuildArgs(Configuration appConfiguration)
    {
        Configuration compilerConfiguration = appConfiguration;
        if (EnvironmentVariables.BuiltNuGetsPath is string packagesPath)
        {
            string? configurationName = new DirectoryInfo(packagesPath).Parent?.Name;
            if (Enum.TryParse(configurationName, out Configuration packageConfiguration) &&
                packageConfiguration is Configuration.Debug or Configuration.Release)
                compilerConfiguration = packageConfiguration;
        }

        string compilerArgs = GetR2RBuildArgs(compilerConfiguration);
        // Helix deploys compiler NuGets; only local runs can use an in-build compiler.
        if (!EnvironmentVariables.IsRunningOnCI)
            Assert.Contains("-p:Crossgen2InBuildDir=", compilerArgs);
        _testOutput.WriteLine($"PGO compiler configuration: {compilerConfiguration}; " +
            (string.IsNullOrEmpty(compilerArgs) ? "SDK-resolved crossgen2 package" : compilerArgs));

        return $"{compilerArgs} -p:UsingBrowserRuntimeWorkload=false";
    }

    private void SaveCompilerLog(ProjectInfo info, string fileName)
    {
        string path = Path.Combine(_projectDir, fileName);
        Assert.True(File.Exists(path),
            $"Crossgen2 did not emit {path}; expected JitDisasm=*WasmPgoScenarios:* to match the test methods.");
        File.Copy(path, Path.Combine(info.LogPath, fileName));
    }

    private string GetCompilerMethodListing(string fileName, string method)
    {
        string path = Path.Combine(_projectDir, fileName);
        Assert.True(File.Exists(path), $"Missing compiler evidence: {path}");
        Match listing = Regex.Match(File.ReadAllText(path),
            $@"(?ms)^; Assembly listing for method [^\r\n]*WasmPgoScenarios:{Regex.Escape(method)}\([^\r\n]*\r?\n(?<body>.*?)(?=^; Assembly listing for method |\z)");
        Assert.True(listing.Success, $"Missing R2R compiler listing for WasmPgoScenarios:{method} in {path}");

        return listing.Groups["body"].Value;
    }

    private void AssertMeasuredPgoCount(string method, long count)
    {
        string listing = GetCompilerMethodListing("pgo-consumed.jit.txt", method);
        Assert.Contains("; ReadyToRun compilation", listing);
        // Normal optimization can reclassify an incorporated edge profile as synthesized.
        Match hotness = Regex.Match(listing, @"; with (?:Static|Synthesized) PGO: fgCalledCount is ([0-9]+(?:\.[0-9]+)?)");
        Assert.True(hotness.Success, $"No PGO entry count for {method}:{Environment.NewLine}{listing}");
        Assert.Equal((double)count, double.Parse(hotness.Groups[1].Value, CultureInfo.InvariantCulture));
    }

    private void AssertCompilerInlines(string fileName, string method, int total, int? withPgo = null)
    {
        string listing = GetCompilerMethodListing(fileName, method);
        Assert.Contains("; ReadyToRun compilation", listing);
        Match inlines = Regex.Match(listing,
            @"; (\d+) inlinees with PGO data; (\d+) single block inlinees; (\d+) inlinees without PGO data");
        Assert.True(inlines.Success, $"Missing inline evidence for {method}:{Environment.NewLine}{listing}");
        Assert.Equal(total, Enumerable.Range(1, 3).Sum(i => int.Parse(inlines.Groups[i].Value, CultureInfo.InvariantCulture)));
        if (withPgo is int expected)
            Assert.Equal(expected, int.Parse(inlines.Groups[1].Value, CultureInfo.InvariantCulture));
    }

    private Dictionary<string, Guid> GetPublishedModuleMvids(Configuration config)
    {
        var modules = new Dictionary<string, Guid>(StringComparer.Ordinal);
        string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
        AssetsData assets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
        // Composite owners and unlisted compiler artifacts are not original IL assemblies.
        foreach (WebcilAsset asset in assets.coreAssembly.Concat(assets.assembly).Where(asset => asset.isCompositeImage != true))
        {
            using var stream = File.OpenRead(Path.Combine(frameworkDir, asset.name));
            using var reader = new WebcilReader(stream);
            MetadataReader metadata = reader.GetMetadataReader();
            modules.Add(metadata.GetString(metadata.GetAssemblyDefinition().Name),
                metadata.GetGuid(metadata.GetModuleDefinition().Mvid));
        }

        return modules;
    }

    private string CopyPgoReferences(ProjectInfo info, Configuration config, string buildOutput,
        IReadOnlyDictionary<string, Guid> publishedMvids, string directoryName)
    {
        string? runtimePack = ProjectProviderBase.TryGetRuntimePackDirFromBuildOutput(buildOutput);
        Assert.NotNull(runtimePack);
        string referenceDir = Path.Combine(info.LogPath, directoryName);
        Directory.CreateDirectory(referenceDir);

        // The SDK's app TFM and the local runtime pack TFM may differ. Resolve references by the
        // MVIDs from boot-listed assets, not by the TFM or an arbitrary bin directory.
        var candidates = new Dictionary<Guid, string>();
        foreach (string directory in new[] {
            Path.Combine(_projectDir, "bin", config.ToString()),
            Path.Combine(runtimePack!, "runtimes", "browser-wasm", "lib") })
        {
            foreach (string path in Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                using var stream = File.OpenRead(path);
                if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
                    continue;
                stream.Position = 0;
                using var reader = new PEReader(stream);
                if (!reader.HasMetadata)
                    continue;
                MetadataReader metadata = reader.GetMetadataReader();
                candidates.TryAdd(metadata.GetGuid(metadata.GetModuleDefinition().Mvid), path);
            }
        }

        foreach ((string name, Guid mvid) in publishedMvids)
        {
            Assert.True(candidates.TryGetValue(mvid, out string? reference),
                $"No matching IL reference for published {name}, MVID {mvid}");
            File.Copy(reference!, Path.Combine(referenceDir, name + ".dll"), overwrite: true);
        }
        Assert.True(File.Exists(Path.Combine(referenceDir, "System.Private.CoreLib.dll")));
        Assert.True(File.Exists(Path.Combine(referenceDir, "BlazorBasicTestApp.dll")));

        return referenceDir;
    }

    private string CreateBasicBlockPgoProfile(ProjectInfo info, Configuration config, string buildOutput, string tracePath,
        IReadOnlyDictionary<string, Guid> publishedMvids, bool interpretedFallback)
    {
        Assert.True(File.Exists(tracePath), $"Missing trace: {tracePath}");
        string referenceDir = CopyPgoReferences(info, config, buildOutput, publishedMvids, "basic-block-references");
        if (config == Configuration.Release)
        {
            using var stream = File.OpenRead(Path.Combine(referenceDir, "BlazorBasicTestApp.dll"));
            using var reader = new PEReader(stream);
            MetadataReader metadata = reader.GetMetadataReader();
            MethodDefinitionHandle handle = Assert.Single(metadata.MethodDefinitions,
                methodHandle => metadata.GetString(metadata.GetMethodDefinition(methodHandle).Name) == "IlZeroBackedge");
            byte[]? il = reader.GetMethodBody(metadata.GetMethodDefinition(handle).RelativeVirtualAddress).GetILBytes();
            Assert.NotNull(il);
            Assert.Equal((byte)OpCodes.Ldsfld.Value, il[0]);
            Assert.Equal((byte)OpCodes.Ret.Value, il[^1]);
            Assert.Equal((byte)OpCodes.Bgt_S.Value, il[^3]);
            Assert.Equal(0, il.Length - 1 + (sbyte)il[^2]);
        }

        string tool = Path.Combine(AppContext.BaseDirectory, "dotnet-pgo", "dotnet-pgo.dll");
        Assert.True(File.Exists(tool), $"Missing dotnet-pgo payload: {tool}");
        string ordinaryMibcPath = Path.Combine(info.LogPath, "ordinary.mibc");
        string ordinaryDumpPath = Path.Combine(info.LogPath, "ordinary.json");
        using (var command = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
            command.ExecuteWithCapturedOutput($"exec \"{tool}\" create-mibc --trace \"{tracePath}\" --reference \"{Path.Combine(referenceDir, "*.dll")}\" --output \"{ordinaryMibcPath}\"").EnsureSuccessful();
        using (var command = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
            command.ExecuteWithCapturedOutput($"exec \"{tool}\" dump --input \"{ordinaryMibcPath}\" --output \"{ordinaryDumpPath}\"").EnsureSuccessful();
        using JsonDocument ordinaryDump = JsonDocument.Parse(File.ReadAllText(ordinaryDumpPath));
        JsonElement[] ordinaryMethods = ordinaryDump.RootElement.GetProperty("Methods").EnumerateArray().ToArray();
        if (interpretedFallback)
            Assert.DoesNotContain(ordinaryMethods, method =>
                method.GetProperty("Method").GetString()!.Contains("FallbackLibrary.WasmPgoScenarios.InterpOnly(", StringComparison.Ordinal));
        string mibcPath = Path.Combine(info.LogPath, "basic-block.mibc");
        string dumpPath = Path.Combine(info.LogPath, "basic-block.json");
        using (var command = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
            command.ExecuteWithCapturedOutput($"exec \"{tool}\" create-mibc --include-interpreted-rundown --trace \"{tracePath}\" --reference \"{Path.Combine(referenceDir, "*.dll")}\" --output \"{mibcPath}\"").EnsureSuccessful();
        using (var command = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
            command.ExecuteWithCapturedOutput($"exec \"{tool}\" dump --input \"{mibcPath}\" --output \"{dumpPath}\"").EnsureSuccessful();

        using JsonDocument dump = JsonDocument.Parse(File.ReadAllText(dumpPath));
        JsonElement[] allMethods = dump.RootElement.GetProperty("Methods").EnumerateArray().ToArray();
        JsonElement[] methods = allMethods
            .Where(method => method.GetProperty("Method").GetString()!.Contains("BlazorBasicTestApp.WasmPgoScenarios.", StringComparison.Ordinal))
            .ToArray();
        JsonElement ordinaryHelper = Assert.Single(ordinaryMethods, method => IsMethod(method, "Helper") &&
            method.GetProperty("Method").GetString()!.Contains("BlazorBasicTestApp.WasmPgoScenarios.", StringComparison.Ordinal));
        Assert.Equal(ordinaryHelper.GetProperty("InstrumentationData").GetRawText(),
            Assert.Single(methods, method => IsMethod(method, "Helper")).GetProperty("InstrumentationData").GetRawText());
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "Helper")), 15);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "NestedInlinee")), 15);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "DirectCalls")), 1, loopIterations: 10);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "GenericCalls")), 1);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "FoldedCalls")), 1);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "ConstantInlinee")), 1);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "EntryAndBackedge")), 2, loopIterations: 7);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "IlZeroBackedge")), 1,
            ilZeroBackedge: config == Configuration.Release);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "SwitchEdges")), 10);
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "ExceptionEdges")), 2);
        JsonElement comparison = Assert.Single(allMethods, method => method.GetProperty("Method").GetString()!
            .Contains("System.MemoryExtensions.SequenceEqual<int8>(", StringComparison.Ordinal));
        JsonElement[] comparisonRecords = comparison.GetProperty("InstrumentationData").EnumerateArray().ToArray();
        JsonElement sampleCount = Assert.Single(comparisonRecords,
            record => record.GetProperty("InstrumentationKind").GetString() == "ValueHistogramLongCount");
        JsonElement samples = Assert.Single(comparisonRecords,
            record => record.GetProperty("InstrumentationKind").GetString() == "ValueHistogram");
        Assert.Equal(sampleCount.GetProperty("ILOffset").GetInt32(), samples.GetProperty("ILOffset").GetInt32());
        Assert.Equal(5L, sampleCount.GetProperty("Data").GetInt64());
        Assert.Equal(new[] { "4", "4", "4", "8", "8" },
            samples.GetProperty("Data").EnumerateArray().Select(sample => sample.GetString()).Order(StringComparer.Ordinal));
        AssertEdgeCounts(Assert.Single(methods, method => IsMethod(method, "CompareBytes")), 5);
        JsonElement fallback = Assert.Single(allMethods, method =>
            method.GetProperty("Method").GetString()!.Contains("FallbackLibrary.WasmPgoScenarios.InterpOnly(", StringComparison.Ordinal));
        if (interpretedFallback)
            Assert.False(fallback.TryGetProperty("InstrumentationData", out _), fallback.ToString());
        else
            AssertEdgeCounts(fallback, 1);
        Assert.False(fallback.TryGetProperty("ExclusiveWeight", out _), fallback.ToString());
        Assert.DoesNotContain(methods, method => IsMethod(method, "NeverCalled"));
        JsonElement[] generics = methods.Where(method => IsMethod(method, "GenericEntries")).ToArray();
        Assert.Equal(2, generics.Length);
        AssertEdgeCounts(Assert.Single(generics, method => method.GetProperty("Method").GetString()!.Contains("__Canon", StringComparison.Ordinal)), 7);
        AssertEdgeCounts(Assert.Single(generics, method => method.GetProperty("Method").GetString()!.Contains("<int32>", StringComparison.Ordinal)), 6);
        JsonElement deferredFill = Assert.Single(allMethods, method => method.GetProperty("Method").GetString()!.Contains(
            "BlazorBasicTestApp.IntrinsicPgoCalls.DeferredFillCalls(", StringComparison.Ordinal) &&
            method.TryGetProperty("InstrumentationData", out _));
        AssertEdgeCounts(deferredFill, 1);

        return mibcPath;

        static bool IsMethod(JsonElement method, string name)
            => Regex.IsMatch(method.GetProperty("Method").GetString()!, $@"\.{Regex.Escape(name)}(?:<|\()");

        static JsonElement[] GetCounters(JsonElement method)
        {
            Assert.False(method.TryGetProperty("ExclusiveWeight", out _), $"Counts must not become CPU sample weights: {method}");

            return method.GetProperty("InstrumentationData").EnumerateArray()
                .Where(element => element.GetProperty("InstrumentationKind").GetString() is not
                    ("Version" or "NumRuns" or "ValueHistogramLongCount" or "ValueHistogram")).ToArray();
        }

        static void AssertEdgeCounts(JsonElement method, long entries, long? loopIterations = null, bool ilZeroBackedge = false)
        {
            JsonElement[] counters = GetCounters(method);
            Assert.NotEmpty(counters);
            Assert.All(counters, counter => Assert.Equal("EdgeLongCount", counter.GetProperty("InstrumentationKind").GetString()));
            Assert.Equal(entries, counters.Where(counter =>
                counter.GetProperty("Other").GetInt32() == 0 &&
                (!ilZeroBackedge || counter.GetProperty("ILOffset").GetInt32() != 0))
                .Sum(counter => counter.GetProperty("Data").GetInt64()));
            if (ilZeroBackedge)
                Assert.Equal(6, Assert.Single(counters, counter => counter.GetProperty("ILOffset").GetInt32() == 0 &&
                    counter.GetProperty("Other").GetInt32() == 0).GetProperty("Data").GetInt64());
            if (loopIterations is long iterations)
            {
                Assert.True(counters.Length > 1, $"Expected loop and return edge counters: {method}");
                Assert.Contains(counters, counter => counter.GetProperty("Data").GetInt64() == iterations);
            }
        }
    }

    private void AddBasicBlockPgoScenarios()
    {
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string>
        {
            ["currentCount++;"] = """
                currentCount = BlazorBasicTestApp.WasmPgoScenarios.RunOnce();
                BlazorBasicTestApp.WasmPgoScenarios.CpuSamplingTarget(currentCount);
                """
        });
        File.WriteAllText(Path.Combine(_projectDir, "WasmPgoScenarios.cs"), """
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.

            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            namespace BlazorBasicTestApp;

            public static class WasmPgoScenarios
            {
                private static bool s_ran;
                private static int s_result;
                private static int s_backedgeRemaining = 7;

                public static int RunOnce()
                {
                    if (s_ran)
                        return s_result;
                    s_ran = true;
                    int result = DirectCalls();
                    var helper = typeof(WasmPgoScenarios).GetMethod(nameof(Helper))!;
                    for (int i = 0; i < 5; i++)
                        result += (int)helper.Invoke(null, new object[] { i })!;
                    result += EntryAndBackedge(0) + EntryAndBackedge(7) + GenericCalls();
                    IlZeroBackedge();
                    if (s_backedgeRemaining != 0)
                        throw new Exception("The IL-zero backedge did not complete");
                    if (FoldedCalls() != 8)
                        throw new Exception("Constant-folded inlinee returned an unexpected result");
                    int switchResult = 0;
                    for (int i = 0; i < 10; i++)
                        switchResult += SwitchEdges(i % 5);
                    if (switchResult != 24 || ExceptionEdges(false) + ExceptionEdges(true) != 18)
                        throw new Exception("Switch or exception profiling changed execution");
                    sbyte[] left = new sbyte[8];
                    sbyte[] right = new sbyte[8];
                    right[7] = 1;
                    for (int i = 0; i < 5; i++)
                    {
                        int length = i < 3 ? 4 : 8;
                        if (CompareBytes(left, right, length) != (length == 4))
                            throw new Exception("Value profiling changed the comparison");
                    }
                    IntrinsicPgoCalls.ValidateCalls();
                    IntrinsicPgoCalls.DeferredFillCalls();
                    if (result != 159)
                        throw new Exception($"Unexpected PGO scenario result: {result}");
                    s_result = result;

                    return result;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int DirectCalls()
                {
                    int result = 0;
                    for (int i = 0; i < 10; i++)
                        result += Helper(i);

                    return result;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int Helper(int value) => NestedInlinee(value) + 1;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int NestedInlinee(int value) => value * 2;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int GenericEntries<T>(T value) => value is not null ? 1 : 0;

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int GenericCalls()
                {
                    int result = 0;
                    for (int i = 0; i < 3; i++)
                        result += GenericEntries<string>("value");
                    for (int i = 0; i < 4; i++)
                        result += GenericEntries<object>("value");
                    for (int i = 0; i < 6; i++)
                        result += GenericEntries<int>(i);

                    return result;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int FoldedCalls() => ConstantInlinee(7);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int ConstantInlinee(int value) => value + 1;

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int EntryAndBackedge(int iterations)
                {
                    int result = 0;
                    for (int i = 0; i < iterations; i++)
                        result += i;

                    return result;
                }

                public static int NeverCalled(int value) => value + 1;

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static bool CompareBytes(sbyte[] left, sbyte[] right, int length) =>
                    ((ReadOnlySpan<sbyte>)left.AsSpan(0, length)).SequenceEqual(right.AsSpan(0, length));

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int SwitchEdges(int value)
                {
                    switch (value)
                    {
                        case 0:
                            return 1;
                        case 1:
                        case 2:
                            return 2;
                        case 3:
                            return 3;
                        default:
                            return 4;
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int ExceptionEdges(bool fail)
                {
                    try
                    {
                        if (fail)
                            throw new InvalidOperationException();
                        return 7;
                    }
                    catch (InvalidOperationException)
                    {
                        return 11;
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void IlZeroBackedge()
                {
                Repeat:
                    s_backedgeRemaining--;
                    if (s_backedgeRemaining > 0)
                        goto Repeat;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static int CpuSamplingTarget(int value)
                {
                    for (int i = 0; i < 100_000; i++)
                        value = unchecked(value * 31 + i);

                    return value;
                }
            }

            public static class IntrinsicPgoCalls
            {
                public enum DeferredFillValue : int { First = 11, Last = 29 }
                public struct IntrinsicElement { public object? Reference; }
                public struct ExceptionalElement { public object? Reference; }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void ValidateCalls()
                {
                    var values = new IntrinsicElement[1];
                    ref IntrinsicElement first = ref MemoryMarshal.GetArrayDataReference(values);
                    first.Reference = values;
                    if (!ReferenceEquals(values[0].Reference, values))
                        throw new Exception("Expanded intrinsic returned an unexpected reference");
                    if (!RuntimeHelpers.IsReferenceOrContainsReferences<IntrinsicElement>())
                        throw new Exception("Constant-folded intrinsic did not detect a reference field");
                    try
                    {
                        BodyThrow();
                        throw new Exception("The intrinsic body did not throw");
                    }
                    catch (NullReferenceException)
                    {
                    }
                    try
                    {
                        FailedArgument();
                        throw new Exception("Argument evaluation did not throw");
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void DeferredFillCalls()
                {
                    Span<DeferredFillValue> values = stackalloc DeferredFillValue[1];
                    values.Fill(DeferredFillValue.First);
                    values.Fill(DeferredFillValue.Last);
                    if (values[0] != DeferredFillValue.Last)
                        throw new Exception("Deferred Fill produced an unexpected value");
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static ref ExceptionalElement BodyThrow()
                    => ref MemoryMarshal.GetArrayDataReference<ExceptionalElement>(null!);

                [MethodImpl(MethodImplOptions.NoInlining)]
                public static ref ExceptionalElement FailedArgument()
                    => ref MemoryMarshal.GetArrayDataReference(ThrowArrayArgument());

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static ExceptionalElement[] ThrowArrayArgument()
                    => throw new InvalidOperationException();
            }
            """);
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
    [InlineData("-p:PublishReadyToRun=false", "PublishReadyToRunPgoInstrumentation requires PublishReadyToRun=true", true)]
    [InlineData("-p:EnableDiagnostics=false", "PublishReadyToRunPgoInstrumentation requires EnableDiagnostics=true", true)]
    [InlineData("-p:WasmEnableThreads=true", "PublishReadyToRunPgoInstrumentation supports only single-threaded", true)]
    [InlineData("-p:PublishReadyToRunPgoInstrumentation=all", "PublishReadyToRunPgoInstrumentation must be true or false", true)]
    [InlineData("", "PublishReadyToRunPgoInstrumentation is supported only when publishing", false)]
    [TestCategory("no-workload"), TestCategory("coreclr")]
    public void BlazorPgoRejectsUnsupportedConfiguration(string extraArgs, string expectedError, bool publish)
    {
        ProjectInfo info = CopyTestAsset(Configuration.Debug, aot: false, TestAsset.BlazorBasicTestApp, "blazor_invalid_pgo",
            extraProperties: """
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmEnableThreads>false</WasmEnableThreads>
                <PublishReadyToRun>true</PublishReadyToRun>
                <PublishReadyToRunPgoInstrumentation>true</PublishReadyToRunPgoInstrumentation>
                """);
        string args = $"{GetPgoBuildArgs(Configuration.Debug)} {extraArgs}";
        (_, string output) = publish
            ? BlazorPublish(info, Configuration.Debug,
                new PublishOptions(ExpectSuccess: false, AssertAppBundle: false, ExtraMSBuildArgs: args))
            : BlazorBuild(info, Configuration.Debug,
                new BuildOptions(ExpectSuccess: false, AssertAppBundle: false, ExtraMSBuildArgs: args));
        Assert.Contains(expectedError, output);
    }

    [Fact]
    public async Task BlazorEventPipeTestWithMetrics()
    {
        string extraProperties = @"
                <EnableDiagnostics>true</EnableDiagnostics>
                <EventSourceSupport>true</EventSourceSupport>
                <MetricsSupport>true</MetricsSupport>
                <WBTDevServer>true</WBTDevServer>
            ";

        ProjectInfo info = CopyTestAsset(Configuration.Release, aot: false, TestAsset.BlazorBasicTestApp, "blazor_metrics", extraProperties: extraProperties);

        UpdateCounterPage();
        BuildProject(info, Configuration.Release, new BuildOptions(AssertAppBundle: false));

        async Task CollectMetricsTest(IPage page)
        {
            await SetupCounterPage(page, "metrics.nettrace", "globalThis.getDotnetRuntime(0).collectMetrics({ durationSeconds: 5.0, skipDownload: true })");
            await ClickAndCollect(page);
        }

        // Run the test using the custom handler
        await RunForBuildWithDotnetRun(new BlazorRunOptions(
            Configuration: Configuration.Release,
            Test: CollectMetricsTest,
            TimeoutSeconds: 60,
            CheckCounter: false,
            ServerEnvironment: new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = info.LogPath,
                ["DEVSERVER_UPLOAD_PATTERN"] = uploadPattern
            }
        ));

        var actualInstruments = ExtractInstrumentNames(info, "metrics.nettrace");
        var expectedInstruments = new[]
        {
            "System.Diagnostics.Metrics/instrumentName/dotnet.assembly.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.exceptions",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.collections",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.heap.total_allocated",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.last_collection.heap.fragmentation.size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.last_collection.heap.size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.last_collection.memory.committed_size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.pause.time",
            "System.Diagnostics.Metrics/instrumentName/dotnet.jit.compilation.time",
            "System.Diagnostics.Metrics/instrumentName/dotnet.jit.compiled_il.size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.jit.compiled_methods",
            "System.Diagnostics.Metrics/instrumentName/dotnet.monitor.lock_contentions",
            "System.Diagnostics.Metrics/instrumentName/dotnet.process.cpu.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.process.memory.working_set",
            "System.Diagnostics.Metrics/instrumentName/dotnet.thread_pool.queue.length",
            "System.Diagnostics.Metrics/instrumentName/dotnet.thread_pool.thread.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.thread_pool.work_item.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.timer.count",
        };

        foreach (var expectedInstrument in expectedInstruments)
        {
            Assert.True(actualInstruments.ContainsKey(expectedInstrument), $"The metrics.nettrace should contain instrument: {expectedInstrument}");
        }
    }

    [Fact]
    public async Task BlazorEventPipeTestWithHeapDump()
    {
        string extraProperties = @"
                <EnableDiagnostics>true</EnableDiagnostics>
                <WBTDevServer>true</WBTDevServer>
            ";

        ProjectInfo info = CopyTestAsset(Configuration.Release, aot: false, TestAsset.BlazorBasicTestApp, "blazor_gc_dump", extraProperties: extraProperties);

        UpdateCounterPage();

        // Build the project
        BuildProject(info, Configuration.Release, new BuildOptions(AssertAppBundle: false));

        // Create a custom test handler that will navigate to Counter page, collect CPU samples,
        // click the button, and upload the trace
        async Task CollectGcDumpTest(IPage page)
        {
            await SetupCounterPage(page, "gcdump.nettrace", "globalThis.getDotnetRuntime(0).collectGcDump({ durationSeconds: 5.0, skipDownload: true })");
            await ClickAndCollect(page);
        }

        // Run the test using the custom handler
        await RunForBuildWithDotnetRun(new BlazorRunOptions(
            Configuration: Configuration.Release,
            Test: CollectGcDumpTest,
            TimeoutSeconds: 60,
            CheckCounter: false,
            ServerEnvironment: new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = info.LogPath,
                ["DEVSERVER_UPLOAD_PATTERN"] = uploadPattern
            }
        ));

        var actualEvents = ExtractEventNames(info, "gcdump.nettrace");
        var expectedEvents = new[]
        {
            "Microsoft-Windows-DotNETRuntime/GC/Start",
            "Microsoft-Windows-DotNETRuntime/GC/Stop",
            "Microsoft-Windows-DotNETRuntime/GC/BulkEdge",
            "Microsoft-Windows-DotNETRuntime/GC/BulkNode",
            "Microsoft-Windows-DotNETRuntime/GC/BulkRootEdge",
            "Microsoft-Windows-DotNETRuntime/Type/BulkType"
        };

        foreach (var expectedEvent in expectedEvents)
        {
            Assert.True(actualEvents.ContainsKey(expectedEvent), $"The metrics.nettrace should contain event: {expectedEvent}");
        }
    }

    private void ValidateInterpPgoTrace(ProjectInfo info, Configuration config, string traceFileName,
        string expectedMethod, bool instrumentInterpreter)
    {
        string tracePath = Path.GetFullPath(Path.Combine(info.LogPath, traceFileName));
        Assert.True(File.Exists(tracePath), $"PGO trace {tracePath} was not created");

        // The untrimmed IL assemblies next to the app (bin/<Config>/<TFM>) share the MVID of the served
        // webcil, so dotnet-pgo can resolve the interpreter events against them.
        string referenceDir = Path.Combine(_projectDir, "bin", config.ToString(), DefaultTargetFrameworkForBlazor);
        Assert.True(Directory.Exists(referenceDir), $"Reference assembly directory {referenceDir} was not found");

        // dotnet-pgo is deployed next to the test by the _AddDotnetPgoToTestPayload target.
        string pgoTool = Path.Combine(AppContext.BaseDirectory, "dotnet-pgo", "dotnet-pgo.dll");
        Assert.True(File.Exists(pgoTool), $"dotnet-pgo was not found at {pgoTool}");

        string mibcPath = Path.Combine(info.LogPath, "pgo.mibc");
        string discoveryArgs = instrumentInterpreter ? "" :
            $"--include-interpreted-rundown --include-methods \"\\.{Regex.Escape(expectedMethod)}\\(\"";

        using (var createCmd = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
        {
            createCmd.ExecuteWithCapturedOutput(
                $"exec \"{pgoTool}\" create-mibc {discoveryArgs} --trace \"{tracePath}\" --reference \"{Path.Combine(referenceDir, "*.dll")}\" --output \"{mibcPath}\"")
                .EnsureSuccessful();
        }
        Assert.True(File.Exists(mibcPath), $"dotnet-pgo did not produce {mibcPath}");

        string dumpPath = Path.Combine(info.LogPath, "pgo.dump.txt");
        using (var dumpCmd = new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false).WithWorkingDirectory(_projectDir))
        {
            dumpCmd.ExecuteWithCapturedOutput($"exec \"{pgoTool}\" dump --input \"{mibcPath}\" --output \"{dumpPath}\"").EnsureSuccessful();
        }
        string dumpText = File.ReadAllText(dumpPath);
        Assert.Contains(expectedMethod, dumpText);
        using JsonDocument dump = JsonDocument.Parse(dumpText);
        if (!instrumentInterpreter)
        {
            JsonElement presence = Assert.Single(dump.RootElement.GetProperty("Methods").EnumerateArray());
            Assert.False(presence.TryGetProperty("InstrumentationData", out _), presence.ToString());
            Assert.False(presence.TryGetProperty("ExclusiveWeight", out _), presence.ToString());

            return;
        }
        JsonElement method = Assert.Single(dump.RootElement.GetProperty("Methods").EnumerateArray()
            .Where(element => element.TryGetProperty("InstrumentationData", out JsonElement instrumentation) &&
                instrumentation.EnumerateArray().Any(record =>
                    record.GetProperty("InstrumentationKind").GetString() is not ("Version" or "NumRuns"))));
        Assert.Contains("." + expectedMethod + "(", method.GetProperty("Method").GetString(), StringComparison.Ordinal);
        JsonElement entry = Assert.Single(method.GetProperty("InstrumentationData").EnumerateArray()
            .Where(element => element.GetProperty("InstrumentationKind").GetString() is not ("Version" or "NumRuns")));
        Assert.Equal("BasicBlockIntCount", entry.GetProperty("InstrumentationKind").GetString());
        Assert.Equal(0, entry.GetProperty("ILOffset").GetInt32());
        Assert.Equal(5, entry.GetProperty("Data").GetInt64());
        Assert.DoesNotContain("BasicBlockLongCount", dumpText);
    }

    private string ConvertTrace(ProjectInfo info, string fileName)
    {
        var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, fileName));
        Assert.True(File.Exists(traceFilePath), $"Trace file {traceFilePath} was not created");
        var conversionLog = new StringBuilder();
        using var sw = new StringWriter(conversionLog);
        var options = new TraceLogOptions
        {
            ConversionLog = sw,
        };
        var traceConvertedPath = TraceLog.CreateFromEventTraceLogFile(traceFilePath, null, options);
        _testOutput.WriteLine(conversionLog.ToString());
        Assert.True(File.Exists(traceConvertedPath), $"Trace file {traceConvertedPath} was not created");
        return traceConvertedPath;
    }

    private static Dictionary<string, int> ExtractEventNames(ProjectInfo info, string fileName)
    {
        var dictionary = new Dictionary<string, int>();
        var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, fileName));
        using (var source = new EventPipeEventSource(traceFilePath))
        {
            source.Clr.All += (data) =>
            {
                var key = $"{data.ProviderName}/{data.EventName}";
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = dictionary[key] + 1;
                }
                else
                {
                    dictionary[key] = 1;
                }
            };
            source.Process();
        }

        return dictionary;
    }

    private static Dictionary<string, int> ExtractInstrumentNames(ProjectInfo info, string fileName)
    {
        var dictionary = new Dictionary<string, int>();
        var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, fileName));
        using (var source = new EventPipeEventSource(traceFilePath))
        {
            source.Dynamic.All += (data) =>
            {
                foreach (var arg in data.PayloadNames)
                {
                    var key = $"{data.ProviderName}/{arg}/{data.PayloadByName(arg)}";
                    if (dictionary.ContainsKey(key))
                    {
                        dictionary[key] = dictionary[key] + 1;
                    }
                    else
                    {
                        dictionary[key] = 1;
                    }
                }
            };
            source.Process();
        }

        return dictionary;
    }

    private void UpdateCounterPage(bool useCpuSamplingTarget = false)
    {
        var replacements = new Dictionary<string, string> {
                {
                    @"currentCount++;",
                    """
                    for(int i = 0; i < 100; i++)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("Incrementing count: ");
                        sb.Append(i);
                        sb.Append(" ");
                        sb.Append(DateTime.Now.ToString("O"));
                        if( i % 50 == 0)
                        {
                            Console.WriteLine(sb.ToString());
                        }
                    }
                    ${increment}
                    """.Replace("${increment}", useCpuSamplingTarget ? "currentCount = CpuSamplingTarget(currentCount);" : "currentCount++;")
                }
            };

        if (useCpuSamplingTarget)
        {
            replacements.Add(
                @"private async Task IncrementCount()",
                """
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                private static int CpuSamplingTarget(int value)
                {
                    for (int i = 0; i < 100_000; i++)
                    {
                        value = unchecked((value * 31) + i);
                    }
                    return value;
                }

                private async Task IncrementCount()
                """);
        }

        UpdateFile(Path.Combine("Pages", "Counter.razor"), replacements);
    }

    private void AddCpuSamplingScenarios()
    {
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string>
        {
            ["for (int i = 0; i < 100_000; i++)"] = """
                BlazorBasicTestApp.CpuSamplingScenarios.Run(value);
                for (int i = 0; i < 100_000; i++)
                """
        });
        File.WriteAllText(Path.Combine(_projectDir, "CpuSamplingScenarios.cs"), """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            namespace BlazorBasicTestApp;

            public static unsafe class CpuSamplingScenarios
            {
                private struct References
                {
                    public object First, Second, Third, Fourth;
                }

                private sealed class Holder
                {
                    public References Value;
                }

                public static void Run(int value)
                {
                    var marker = new object();
                    var source = new Holder { Value = new References { First = marker, Second = marker, Third = marker, Fourth = marker } };
                    var destination = new Holder();
                    if (!ReferenceEquals(CopyReferences(destination, source, value != 0), marker) ||
                        !ReferenceEquals(destination.Value.Second, marker) ||
                        !ReferenceEquals(destination.Value.Third, marker))
                        throw new Exception("NoGC reference copy failed");

                    if (FinallyAndFilter() != 11)
                        throw new Exception("Nested finally/filter failed");

                    delegate* unmanaged<int, int> callback = &ReversePInvokeTarget;
                    if (callback(value) != unchecked(value + 1))
                        throw new Exception("Reverse P/Invoke callback failed");
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static object CopyReferences(Holder destination, Holder source, bool first)
                {
                    destination.Value = source.Value;
                    if (first)
                        return destination.Value.First;
                    return destination.Value.Fourth;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static int FinallyAndFilter()
                {
                    int state = 0;
                    try
                    {
                        try
                        {
                            throw new InvalidOperationException();
                        }
                        finally
                        {
                            state++;
                        }
                    }
                    catch (InvalidOperationException exception) when (Filter(exception, ref state))
                    {
                        return state;
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static bool Filter(Exception exception, ref int state)
                {
                    state += 10;
                    return exception is InvalidOperationException;
                }

                [UnmanagedCallersOnly]
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static int ReversePInvokeTarget(int value) => unchecked(value + 1);
            }
            """);
    }

    private async Task SetupCounterPage(IPage page, string fileName, string traceCommand, bool uploadTrace = true)
    {
        await Task.Delay(500);
        // Navigate to the Counter page
        await page.Locator("text=Counter").ClickAsync();

        // Verify we're on the Counter page
        var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 0", txt);

        var up = """
        globalThis.collectAndUpload = async () => {
            console.log(`Tracing ${filename} started ${new Date().toISOString()}`);
            
            const traces = await ${traceCommand};

            console.log(`Tracing done ${new Date().toISOString()}`);

            if (!${uploadTrace}) {
                console.log(`WASM EXIT 0`);
                return;
            }

            // concatenate the buffers into a single Uint8Array
            const concatenated = new Uint8Array(traces.reduce((acc, curr) => acc + curr.byteLength, 0));
            let offset = 0;
            for (const trace of traces) {
                concatenated.set(new Uint8Array(trace), offset);
                offset += trace.byteLength;
            }

            console.log(`File ${filename} size to upload: ${concatenated.byteLength} bytes ${new Date().toISOString()}`);
            await fetch(`/upload/${filename}`, {
                headers: {
                    'Content-Type': 'application/octet-stream'
                },
                method: 'POST',
                body: concatenated
            });
            console.log(`File uploaded successfully: ${new Date().toISOString()}`);
            await new Promise(resolve => setTimeout(resolve, 1000));
            console.log(`Shutting down: ${new Date().toISOString()}`);
            console.log(`WASM EXIT 0`);
        };
        console.log(`globalThis.collectAndUpload method created ${new Date().toISOString()}`);
        """;
        up = up.Replace("${traceCommand}", traceCommand)
            .Replace("${filename}", fileName)
            .Replace("${uploadTrace}", uploadTrace ? "true" : "false");
        await page.EvaluateAsync(up);
    }

    private async Task ClickAndCollect(IPage page, string? downloadPath = null)
    {
        // A runtime trap never rejects donePromise: the runtime catches it, reports it as a console
        // error and exits non-zero, leaving the promise unsettled forever.
        var runtimeFailed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        string? lastConsoleError = null;

        void OnPageError(object? sender, string error) => runtimeFailed.TrySetResult(error);
        void OnConsoleMessage(object? sender, IConsoleMessage message)
        {
            if (message.Type == "error")
                lastConsoleError = message.Text;

            Match exit = BrowserRunner.s_exitRegex.Match(message.Text);
            if (exit.Success && exit.Groups["exitCode"].Value != "0")
                runtimeFailed.TrySetResult(lastConsoleError ?? $"the app exited with code {exit.Groups["exitCode"].Value}");
        }

        page.PageError += OnPageError;
        page.Console += OnConsoleMessage;

        try
        {
            Task<IDownload>? downloadTask = downloadPath is null ? null : page.WaitForDownloadAsync();

            // Use void to prevent Playwright from awaiting the returned Promise,
            // so tracing runs in parallel with button clicks below.
            await page.EvaluateAsync(@"void (globalThis.donePromise = globalThis.collectAndUpload())");
            _testOutput.WriteLine($"Installed script: {DateTime.Now.ToString("O")}");

            // Click the button a few times while tracing is running
            for (int i = 0; i < 5; i++)
            {
                await page.Locator("text=\"Click me\"").ClickAsync();
                await Task.Delay(10);
            }
            _testOutput.WriteLine($"Done clicking: {DateTime.Now.ToString("O")}");

            var txt2 = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.NotEqual("Current count: 0", txt2);

            // Wait for trace collection and upload to complete. EvaluateAsync has no timeout of its
            // own, unlike the Locator calls above, so bound it explicitly.
            Task collected = page.EvaluateAsync(@"globalThis.donePromise");
            Task finished = await Task.WhenAny(collected, runtimeFailed.Task, Task.Delay(s_traceCollectionTimeout));
            if (finished != collected)
            {
                // Tearing down the page faults the pending evaluate; keep that from resurfacing as an
                // unobserved task exception in a later test.
                _ = collected.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

                Assert.Fail(runtimeFailed.Task.IsCompleted
                    ? $"The runtime failed while collecting the trace: {runtimeFailed.Task.Result}"
                    : $"Trace collection did not complete within {s_traceCollectionTimeout.TotalSeconds}s.");
            }

            await collected;
            if (downloadTask is not null && downloadPath is not null)
                await (await downloadTask).SaveAsAsync(downloadPath);
        }
        finally
        {
            page.PageError -= OnPageError;
            page.Console -= OnConsoleMessage;
        }
    }
}
