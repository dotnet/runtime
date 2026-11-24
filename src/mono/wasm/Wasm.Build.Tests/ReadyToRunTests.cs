// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.WebAssembly;
using Microsoft.NET.WebAssembly.Webcil;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using SL = Microsoft.Build.Logging.StructuredLogger;

#nullable enable

namespace Wasm.Build.Tests
{
    // CoreCLR browser-wasm ships ReadyToRun images as webcil-in-wasm; a non-zero R2R table size in the
    // System.Private.CoreLib webcil is the marker that R2R was produced and staged. These tests cover R2R in
    // both build and publish, with and without IL trimming, driving all pages in a real browser. CoreCLR only.
    [TestCategory("coreclr")]
    // Keeps this class out of the CoreCLR no-workload Helix work item, which runs it in a separate one by
    // class name instead (see _SplitOutReadyToRunTests in src/libraries/sendtohelix-wasm.targets).
    [TestCategory("r2r")]
    public class ReadyToRunTests : BlazorWasmTestBase
    {
        private const int InteractionTimeoutMs = 60_000;

        public ReadyToRunTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
            _enablePerTestCleanup = true;
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [TestCategory("no-workload")]
        public Task PublishDeferredReadyToRun(bool nativeRelink, bool failDownload) =>
            PublishDeferredReadyToRunCore(nativeRelink, failDownload, useWorkload: false);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task PublishDeferredReadyToRunWithWorkload(bool failDownload) =>
            PublishDeferredReadyToRunCore(nativeRelink: true, failDownload, useWorkload: true);

        private async Task PublishDeferredReadyToRunCore(bool nativeRelink, bool failDownload, bool useWorkload)
        {
            Configuration config = Configuration.Release;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_deferred",
                extraProperties: $"""
                    <PublishReadyToRun>true</PublishReadyToRun>
                    <PublishReadyToRunComposite>true</PublishReadyToRunComposite>
                    <WasmEnableDeferredR2R>true</WasmEnableDeferredR2R>
                    <WasmEnableIntegrityCheck>{nativeRelink.ToString().ToLowerInvariant()}</WasmEnableIntegrityCheck>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    <Features>$(Features);runtime-async=on</Features>
                    """);
            File.AppendAllText(Path.Combine(_projectDir, "Program.cs"), """

                [System.Runtime.Versioning.SupportedOSPlatform("browser")]
                public static partial class DeferredR2RProbe
                {
                    private sealed class LayoutProbe : System.ComponentModel.CancelEventArgs
                    {
                        public byte Marker = 42;
                        public LayoutProbe() : base(false) { }
                    }

                    private static readonly LayoutProbe s_layout = new();

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    public static int ReadLayoutMarker() => s_layout.Marker;

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    public static int AddOne(int value) => Echo(value) + 1;

                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    private static T Echo<T>(T value) => value;

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int EntryPoint() =>
                        new System.Func<int, int>(AddOne).Method.MethodHandle.GetFunctionPointer().ToInt32();

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    public static int Cold(int value) => value * 2;

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int ColdEntryPoint() =>
                        new System.Func<int, int>(Cold).Method.MethodHandle.GetFunctionPointer().ToInt32();

                    private static readonly System.Threading.Tasks.TaskCompletionSource<int> s_resume = new();

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static System.Threading.Tasks.Task<int> Suspend() => AwaitResume(s_resume.Task);

                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    private static async System.Threading.Tasks.Task<int> AwaitResume(System.Threading.Tasks.Task<int> task) =>
                        await task + 1;

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static void Resume() => s_resume.SetResult(41);

                    private interface IProbe { int Apply(int value); }
                    private sealed class Probe : IProbe
                    {
                        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                        int IProbe.Apply(int value) => value + 1;
                    }

                    private static readonly IProbe s_probe = new Probe();
                    private static readonly System.Func<int, int> s_delegate = AddOne;

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int ViaMethodImpl(int value) => s_probe.Apply(value);

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int ViaDelegate(int value) => s_delegate(value);

                    private struct InterpretedValue { public int Marker; }

                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
                    private static int GenericSize<T>(T value) => System.Runtime.CompilerServices.Unsafe.SizeOf<T>();

                    private static System.Reflection.MethodInfo InterpretedMethod() =>
                        (typeof(DeferredR2RProbe).GetMethod(nameof(GenericSize),
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            ?? throw new System.MissingMethodException(nameof(GenericSize)))
                            .MakeGenericMethod(typeof(InterpretedValue));

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int InterpretedSize() =>
                        (int)InterpretedMethod().Invoke(null, new object[] { new InterpretedValue { Marker = 42 } })!;

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int InterpretedEntryPoint() =>
                        InterpretedMethod().MethodHandle.GetFunctionPointer().ToInt32();

                    private static System.Reflection.ConstructorInfo StringConstructor() =>
                        typeof(string).GetConstructor(new[] { typeof(char[]) })
                            ?? throw new System.MissingMethodException(".ctor(char[])");

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static string ViaStringConstructor() =>
                        (string)StringConstructor().Invoke(new object[] { new[] { 'O', 'K' } });

                    [System.Runtime.InteropServices.JavaScript.JSExport]
                    public static int StringConstructorEntryPoint() =>
                        StringConstructor().MethodHandle.GetFunctionPointer().ToInt32();
                }
                """);
            UpdateFile(Path.Combine("wwwroot", "index.html"), new Dictionary<string, string>
            {
                {
                    """<script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>""",
                    """
                    <script src="_framework/blazor.webassembly#[.{fingerprint}].js" autostart="false"></script>
                    <script>
                        if (location.search.includes("deferred-setup")) {
                            document.querySelector("#app").classList.add("page");
                        } else {
                            Blazor.start({
                                configureRuntime: dotnet => dotnet
                                    .withConfig({ maxParallelDownloads: location.search.includes("single-slot") ? 1 : 16, enableDownloadRetry: true })
                                    .withDotnetReady(async () => {
                                        const runtime = getDotnetRuntime(0);
                                        const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
                                        const probe = exports.DeferredR2RProbe;
                                        const value = probe.AddOne(41);
                                        const pointer = probe.EntryPoint() >>> 0;
                                        globalThis.deferredStartupProbe = {
                                            value, interpreterData: runtime.getHeapU32(pointer + 8),
                                            flags: runtime.getHeapU32(pointer + 12)
                                        };
                                        globalThis.deferredVmInitialized = true;
                                    })
                            });
                        }
                    </script>
                    """
                }
            });
            string extraArgs = $"{GetR2RBuildArgs(config, composite: true)} -p:WasmBuildNative={nativeRelink.ToString().ToLowerInvariant()} -p:UsingBrowserRuntimeWorkload={useWorkload.ToString().ToLowerInvariant()}";
            BlazorPublish(info, config, new PublishOptions(ExtraMSBuildArgs: extraArgs), isNativeBuild: nativeRelink);
            string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
            AssetsData assets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
            WebcilAsset composite = Assert.Single(assets.r2rAssembly);
            Assert.True(composite.isCompositeImage);
            Assert.DoesNotContain(assets.coreAssembly, asset => asset.isCompositeImage == true);
            // Component wrappers contain getWebcilSize and patchWebcilHeader, but no managed functions.
            Assert.All(assets.coreAssembly.Concat(assets.assembly), asset => Assert.True(asset.tableSize is null or 2));
            AssertTrimmedClosureIsFullyStaged(config, frameworkDir);

            var failedExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                BrowserPath: "index.html?deferred-setup",
                CheckCounter: false,
                DetectRuntimeFailures: !failDownload,
                ExpectedExitCode: failDownload ? null : 0,
                OnConsoleMessage: (_, message) =>
                {
                    if (message.Contains("WASM EXIT 1", System.StringComparison.Ordinal))
                        failedExit.TrySetResult();
                },
                Test: async page =>
                {
                    var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    int lateRequests = 0;
                    var releaseEager = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (!nativeRelink)
                    {
                        WebcilAsset eager = assets.assembly.Last();
                        await page.RouteAsync($"**/{eager.name}", async route =>
                        {
                            await releaseEager.Task;
                            await route.ContinueAsync();
                        });
                    }
                    await page.RouteAsync($"**/{composite.name}", async route =>
                    {
                        if (++lateRequests == 1 && nativeRelink)
                        {
                            await route.FulfillAsync(new() { Status = 503, ContentType = "application/wasm", Body = "Retry late image" });
                            return;
                        }
                        requested.TrySetResult();
                        await release.Task;
                        if (failDownload)
                            await route.FulfillAsync(new() { Status = 404, ContentType = "application/wasm", Body = "Missing late image" });
                        else
                            await route.ContinueAsync();
                    });
                    string baseUrl = new System.Uri(page.Url).GetLeftPart(System.UriPartial.Authority);
                    try
                    {
                        await page.GotoAsync(nativeRelink ? $"{baseUrl}?single-slot" : baseUrl,
                            new() { WaitUntil = WaitUntilState.Commit });
                        if (!nativeRelink)
                        {
                            // A free download slot must start R2R even while an eager response blocks VM initialization.
                            await requested.Task.WaitAsync(System.TimeSpan.FromMilliseconds(InteractionTimeoutMs));
                            Assert.False(await page.EvaluateAsync<bool>("globalThis.deferredVmInitialized === true"));
                            releaseEager.TrySetResult();
                        }
                        await page.Locator(".page").WaitForAsync(new() { Timeout = InteractionTimeoutMs });
                        await requested.Task.WaitAsync(System.TimeSpan.FromSeconds(60));
                        await InteractAllPagesAsync(page);
                        uint[] before = await page.EvaluateAsync<uint[]>("""
                            async () => {
                                const runtime = getDotnetRuntime(0);
                                const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
                                globalThis.deferredProbe = exports.DeferredR2RProbe;
                                const pointer = deferredProbe.EntryPoint() >>> 0;
                                const cold = deferredProbe.ColdEntryPoint() >>> 0;
                                globalThis.suspendedRuntimeAsync = deferredProbe.Suspend();
                                if (deferredProbe.ViaMethodImpl(41) !== 42 || deferredProbe.ViaDelegate(41) !== 42)
                                    throw new Error("Initial interface/delegate dispatch failed.");
                                const constructor = deferredProbe.StringConstructorEntryPoint() >>> 0;
                                if (deferredProbe.ViaStringConstructor() !== "OK")
                                    throw new Error("Initial string constructor dispatch failed.");
                                return [deferredProbe.AddOne(41), pointer, runtime.getHeapU32(pointer),
                                    cold, runtime.getHeapU32(cold), deferredProbe.InterpretedEntryPoint() >>> 0,
                                    deferredProbe.InterpretedSize(), constructor, runtime.getHeapU32(constructor + 8)];
                            }
                            """);
                        Assert.Equal(42u, before[0]);
                        Assert.Equal(4u, before[6]);
                        Assert.NotEqual(0u, before[8]);
                        Assert.Equal(42, await page.EvaluateAsync<int>("deferredProbe.ReadLayoutMarker()"));
                        release.TrySetResult();
                        if (failDownload)
                        {
                            await failedExit.Task.WaitAsync(System.TimeSpan.FromSeconds(60));
                            JsonElement failure = await page.EvaluateAsync<JsonElement>("getDotnetRuntime(0).INTERNAL.getDeferredR2RState()");
                            Assert.Equal("failed", failure.GetProperty("status").GetString());
                            Assert.Equal(0, failure.GetProperty("publishedMethods").GetInt32());
                            return;
                        }

                        await page.EvaluateAsync("getDotnetRuntime(0).INTERNAL.waitForDeferredR2R()");
                        Assert.Equal(4u, await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[1]} + 12) & 4"));
                        Assert.Equal(4u, await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[3]} + 12) & 4"));
                        uint[] after = await page.EvaluateAsync<uint[]>("""
                            () => {
                                const pointer = deferredProbe.EntryPoint() >>> 0;
                                return [deferredProbe.AddOne(41), pointer, getDotnetRuntime(0).getHeapU32(pointer)];
                            }
                            """);
                        Assert.Equal(before[0], after[0]);
                        Assert.Equal(before[1], after[1]);
                        Assert.NotEqual(before[2], after[2]);
                        Assert.Equal(0u, await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[1]} + 12) & 4"));
                        Assert.Equal(84, await page.EvaluateAsync<int>("deferredProbe.Cold(42)"));
                        Assert.Equal(84, await page.EvaluateAsync<int>("deferredProbe.Cold(42)"));
                        Assert.NotEqual(before[4], await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[3]})"));
                        Assert.Equal(0u, await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[3]} + 12) & 4"));
                        Assert.Equal(42, await page.EvaluateAsync<int>("deferredProbe.ViaMethodImpl(41)"));
                        Assert.Equal(42, await page.EvaluateAsync<int>("deferredProbe.ViaDelegate(41)"));
                        Assert.Equal(4, await page.EvaluateAsync<int>("deferredProbe.InterpretedSize()"));
                        Assert.Equal(4, await page.EvaluateAsync<int>("deferredProbe.InterpretedSize()"));
                        Assert.Equal(4u, await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[5]} + 12) & 4"));
                        Assert.Equal("OK", await page.EvaluateAsync<string>("deferredProbe.ViaStringConstructor()"));
                        Assert.Equal("OK", await page.EvaluateAsync<string>("deferredProbe.ViaStringConstructor()"));
                        Assert.Equal(0u, await page.EvaluateAsync<uint>(
                            $"getDotnetRuntime(0).getHeapU32({before[7]} + 12) & 4"));
                        await page.EvaluateAsync("deferredProbe.Resume()");
                        Assert.Equal(42, await page.EvaluateAsync<int>("globalThis.suspendedRuntimeAsync"));
                        Assert.Equal(42, await page.EvaluateAsync<int>("deferredProbe.ReadLayoutMarker()"));
                        JsonElement state = await page.EvaluateAsync<JsonElement>("getDotnetRuntime(0).INTERNAL.getDeferredR2RState()");
                        Assert.Equal("completed", state.GetProperty("status").GetString());
                        Assert.Equal("lazy", state.GetProperty("activationMode").GetString());
                        Assert.True(state.GetProperty("interpreterSuspensions").GetInt32() > 0);
                        Assert.True(state.GetProperty("publishedMethods").GetInt32() > 0);
                        Assert.Equal(nativeRelink ? 2 : 1, lateRequests);
                        double started = state.GetProperty("downloadStartedTime").GetDouble();
                        Assert.True(started >= state.GetProperty("downloadQueuedTime").GetDouble());
                        Assert.True(state.GetProperty("compiledTime").GetDouble() >= started);
                        if (!nativeRelink)
                            Assert.True(started < state.GetProperty("eagerReadyTime").GetDouble());
                        await InteractAllPagesAsync(page);
                        if (nativeRelink)
                        {
                            await page.UnrouteAllAsync();
                            await page.EvaluateAsync("""
                                async () => {
                                    const asset = getDotnetRuntime(0).getConfig().resources.r2rAssembly[0];
                                    const response = await fetch(asset.resolvedUrl, { cache: "reload", integrity: asset.hash });
                                    if (!response.ok) throw new Error(`Could not warm R2R cache: ${response.status}`);
                                    // HTTP retention of large uncompressed images is discretionary.
                                    const cache = await caches.open("deferred-r2r-test");
                                    await cache.put(asset.resolvedUrl, response);
                                }
                                """);
                            await page.AddInitScriptAsync("""
                                (() => {
                                    const original = globalThis.fetch.bind(globalThis);
                                    const gate = new Promise(resolve => { globalThis.deferredNormalR2RRelease = resolve; });
                                    const cachedResponses = new WeakSet();
                                    globalThis.deferredCachedR2RCalls = { compile: 0, instantiate: 0, beforeDotnetReady: false };
                                    const compile = WebAssembly.compileStreaming.bind(WebAssembly);
                                    WebAssembly.compileStreaming = response => {
                                        if (cachedResponses.has(response)) deferredCachedR2RCalls.compile++;
                                        return compile(response);
                                    };
                                    const instantiate = WebAssembly.instantiateStreaming.bind(WebAssembly);
                                    WebAssembly.instantiateStreaming = (response, imports) => {
                                        if (cachedResponses.has(response)) {
                                            deferredCachedR2RCalls.instantiate++;
                                            deferredCachedR2RCalls.beforeDotnetReady = !globalThis.deferredVmInitialized;
                                        }
                                        return instantiate(response, imports);
                                    };
                                    globalThis.fetch = (input, init) => {
                                        const url = typeof input === "string" ? input : input.url;
                                        if (!url.includes(".r2r.") || !url.endsWith(".wasm")) return original(input, init);
                                        if (init?.cache === "only-if-cached")
                                            return caches.open("deferred-r2r-test").then(cache => cache.match(input))
                                                .then(response => {
                                                    if (response?.ok) {
                                                        if (location.search.includes("buffered-r2r")) {
                                                            const headers = new Headers(response.headers);
                                                            headers.set("Content-Type", "application/octet-stream");
                                                            response = new Response(response.body, { headers });
                                                        }
                                                        cachedResponses.add(response);
                                                    }
                                                    return response ?? new Response("", { status: 504 });
                                                });
                                        return gate.then(() => original(input, init));
                                    };
                                })();
                                """);
                            foreach (bool buffered in new[] { false, true })
                            {
                                await page.GotoAsync($"{baseUrl}?single-slot{(buffered ? "&buffered-r2r" : string.Empty)}",
                                    new() { WaitUntil = WaitUntilState.Commit });
                                await page.Locator(".page").WaitForAsync(new() { Timeout = InteractionTimeoutMs });
                                uint[] first = await page.EvaluateAsync<uint[]>("""
                                    [deferredStartupProbe.value, deferredStartupProbe.interpreterData, deferredStartupProbe.flags & 4]
                                    """);
                                Assert.Equal(42u, first[0]);
                                Assert.Equal(0u, first[1]);
                                Assert.Equal(0u, first[2]);
                                int[] calls = await page.EvaluateAsync<int[]>("""
                                    [deferredCachedR2RCalls.compile, deferredCachedR2RCalls.instantiate,
                                        deferredCachedR2RCalls.beforeDotnetReady ? 1 : 0]
                                    """);
                                Assert.Equal(new[] { 0, buffered ? 0 : 1, buffered ? 0 : 1 }, calls);
                                await page.EvaluateAsync("deferredNormalR2RRelease()");
                                await page.EvaluateAsync("getDotnetRuntime(0).INTERNAL.waitForDeferredR2R()");
                                JsonElement cached = await page.EvaluateAsync<JsonElement>("getDotnetRuntime(0).INTERNAL.getDeferredR2RState()");
                                Assert.Equal("cached-native", cached.GetProperty("startupMode").GetString());
                                Assert.Equal("cache-hit", cached.GetProperty("cacheStatus").GetString());
                                Assert.Equal("eager", cached.GetProperty("activationMode").GetString());
                                await InteractAllPagesAsync(page);
                            }
                            await page.EvaluateAsync("""
                                () => {
                                    for (const key of Object.keys(localStorage)) {
                                        if (key.startsWith("dotnet-deferred-r2r:v1:"))
                                            localStorage.setItem(key, "another-resource-hash");
                                    }
                                }
                                """);
                            await page.GotoAsync($"{baseUrl}?single-slot", new() { WaitUntil = WaitUntilState.Commit });
                            await page.Locator(".page").WaitForAsync(new() { Timeout = InteractionTimeoutMs });
                            Assert.NotEqual(0u, await page.EvaluateAsync<uint>("deferredStartupProbe.interpreterData"));
                            await page.EvaluateAsync("deferredNormalR2RRelease()");
                            await page.EvaluateAsync("getDotnetRuntime(0).INTERNAL.waitForDeferredR2R()");
                            JsonElement changed = await page.EvaluateAsync<JsonElement>("getDotnetRuntime(0).INTERNAL.getDeferredR2RState()");
                            Assert.Equal("deferred", changed.GetProperty("startupMode").GetString());
                            Assert.Equal("history-miss", changed.GetProperty("cacheStatus").GetString());
                            await page.EvaluateAsync("""
                                async () => {
                                    const asset = getDotnetRuntime(0).getConfig().resources.r2rAssembly[0];
                                    const cache = await caches.open("deferred-r2r-test");
                                    await cache.put(asset.resolvedUrl, new Response("Cached failure", { status: 404 }));
                                }
                                """);
                            await page.GotoAsync($"{baseUrl}?single-slot", new() { WaitUntil = WaitUntilState.Commit });
                            await page.Locator(".page").WaitForAsync(new() { Timeout = InteractionTimeoutMs });
                            Assert.NotEqual(0u, await page.EvaluateAsync<uint>("deferredStartupProbe.interpreterData"));
                            await page.EvaluateAsync("deferredNormalR2RRelease()");
                            await page.EvaluateAsync("getDotnetRuntime(0).INTERNAL.waitForDeferredR2R()");
                            JsonElement rejected = await page.EvaluateAsync<JsonElement>("getDotnetRuntime(0).INTERNAL.getDeferredR2RState()");
                            Assert.Equal("deferred", rejected.GetProperty("startupMode").GetString());
                            Assert.Equal("cache-response-error", rejected.GetProperty("cacheStatus").GetString());
                        }
                    }
                    finally
                    {
                        releaseEager.TrySetResult();
                        release.TrySetResult();
                    }
                }));
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        [TestCategory("no-workload")]
        public async Task BuildRunAllPages(Configuration config)
        {
            // A build stages the prebuilt framework R2R images from the runtime pack (no per-app crossgen2).
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_build",
                extraProperties: config == Configuration.Release ? "<PublishReadyToRun>true</PublishReadyToRun>" : "");
            (_, string output) = BlazorBuild(info, config,
                new BuildOptions(ExtraMSBuildArgs: config == Configuration.Debug ? "-p:WasmEnableHotReload=true" : ""));

            string webcilDir = GetBuildWebcilDir(config);
            AssertPrebuiltFrameworkReadyToRun(webcilDir, output);
            AssertNoDuplicateAssemblies(webcilDir);
            AssertPerAppCrossgenRan(config, expected: false);

            await RunForBuildWithDotnetRun(new BlazorRunOptions(config,
                CheckCounter: false,
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
        }

        [Theory]
        [InlineData(Configuration.Debug, /*trimmed*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [InlineData(Configuration.Release, /*trimmed*/ false)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPages(Configuration config, bool trimmed)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: false, composite: false);

        [Theory]
        [InlineData(Configuration.Release, /*trimmed*/ true, /*nativeRelink*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ false, /*nativeRelink*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ true, /*nativeRelink*/ true)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPagesComposite(Configuration config, bool trimmed, bool nativeRelink)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink, composite: true);

        // CoreCLR relinks dotnet.native.wasm for Blazor when WasmBuildNative=true; the relink is driven by the
        // IsBrowserWasmProject triggers in BrowserWasmApp.CoreCLR.targets. AssertBundle(isNativeBuild: true)
        // proves the served dotnet.native.wasm was relinked rather than the runtime-pack prebuilt.
        [Theory]
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [InlineData(Configuration.Release, /*trimmed*/ false)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPagesNativeRelink(Configuration config, bool trimmed)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: true, composite: false);

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Debug, false, false, null, null, "", false, false)]
        [InlineData(Configuration.Release, false, false, null, null, "", true, true)]
        [InlineData(Configuration.Release, false, true, null, null, "", false, false)]
        [InlineData(Configuration.Release, false, true, "true", "false", "", true, false)]
        [InlineData(Configuration.Release, false, true, "false", "true", "", false, true)]
        [InlineData(Configuration.Debug, false, false, "true", "true", "--optimize", true, true)]
        [InlineData(Configuration.Debug, true, false, null, null, "", false, false)]
        [InlineData(Configuration.Release, true, false, null, null, "", true, true)]
        [InlineData(Configuration.Release, true, true, null, null, "", false, false)]
        [InlineData(Configuration.Release, true, true, "true", "false", "", true, false)]
        [InlineData(Configuration.Release, true, true, "false", "true", "", false, true)]
        [InlineData(Configuration.Debug, true, false, "true", "true", "--optimize", true, true)]
        [TestCategory("no-workload")]
        public void PublishCrossgen2ArgumentsReflectWasmDebugInfoProperties(
            Configuration config,
            bool composite,
            bool debuggerSupport,
            string? stripInliningInfo,
            string? stripDebugInfo,
            string extraCrossgen2Args,
            bool expectStripInliningInfo,
            bool expectStripDebugInfo)
        {
            string extraProperties = $"<PublishReadyToRun>true</PublishReadyToRun><PublishReadyToRunComposite>{composite.ToString().ToLowerInvariant()}</PublishReadyToRunComposite><DebuggerSupport>{debuggerSupport.ToString().ToLowerInvariant()}</DebuggerSupport>";
            if (extraCrossgen2Args.Length > 0)
                extraProperties += $"<PublishReadyToRunCrossgen2ExtraArgs>{extraCrossgen2Args}</PublishReadyToRunCrossgen2ExtraArgs>";
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_args",
                extraProperties: extraProperties);

            string r2rBuildArgs = GetR2RBuildArgs(config, composite);
            if (composite)
            {
                Assert.Contains("-p:Crossgen2SdkOverridePropsPath=", r2rBuildArgs);
                Assert.Contains("-p:Crossgen2SdkOverrideTargetsPath=", r2rBuildArgs);
            }
            List<string> extraBuildArgs = [r2rBuildArgs];
            if (stripInliningInfo is not null)
                extraBuildArgs.Add($"-p:PublishReadyToRunStripInliningInfo={stripInliningInfo}");
            if (stripDebugInfo is not null)
                extraBuildArgs.Add($"-p:PublishReadyToRunStripDebugInfo={stripDebugInfo}");

            // These arguments are computed at evaluation time, so evaluating the publish checks them without running
            // crossgen2. PublishRunAllPagesCore checks that a real publish passes them to crossgen2.
            string crossgen2Args = EvaluatePublishReadyToRunCrossgen2ExtraArgs(config, info.ProjectName, string.Join(" ", extraBuildArgs));
            AssertCrossgen2DebugInfoArgs(config, crossgen2Args, expectStripInliningInfo, expectStripDebugInfo);
            if (extraCrossgen2Args.Length > 0)
                Assert.Contains(extraCrossgen2Args, crossgen2Args);
        }

        // Composite ReadyToRun in workload mode. The wasm-opt lane runs it with WasmRunWasmOpt=true, which rewrites
        // the composite in place with wasm-opt before it is staged; it must keep the self-installing Webcil host ABI
        // and still run. Per-assembly wasm-opt coverage comes from NativeLibraryTests.ProjectWithNativeReference_ReadyToRun.
        [Theory]
        [InlineData(Configuration.Release)]
        [TestCategory("wasm-opt")]
        public async Task PublishRunAllPagesCompositeWithWorkload(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_pub_composite_workload",
                extraProperties: "<PublishReadyToRun>true</PublishReadyToRun><PublishReadyToRunComposite>true</PublishReadyToRunComposite>",
                extraItems: """<ProjectReference Include="../R2rSuffixLibrary/R2rSuffixLibrary.csproj" />""");
            LogR2RSuffixLibraryMarker();

            (string _, string publishOutput) = BlazorPublish(info, config,
                new PublishOptions(UseCache: false, ExtraMSBuildArgs: GetR2RBuildArgs(config, composite: true)));
            AssertReadyToRunWasmOpt(GetObjSubDir(config, "R2R"), publishOutput, composite: true);
            AssertCompositeReadyToRun(GetBlazorBinFrameworkDir(config, forPublish: true));

            bool suffixLibraryLoaded = false;
            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                OnConsoleMessage: (_, msg) => suffixLibraryLoaded |= msg.Contains(SuffixLibraryLoadedMessage),
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
            Assert.True(suffixLibraryLoaded, $"'{SuffixLibraryLoadedMessage}' was not logged; R2rSuffixLibrary.r2r did not load as a component assembly.");
        }

        private async Task PublishRunAllPagesCore(Configuration config, bool trimmed, bool nativeRelink, bool composite)
        {
            // Publish runs per-app crossgen2 for the whole closure, trimmed or not: even the untrimmed CoreLib
            // is a per-app image, not the runtime pack's. nativeRelink also relinks dotnet.native.wasm.
            string label = $"r2r_pub_{(trimmed ? "trim" : "notrim")}{(nativeRelink ? "_native" : "")}{(composite ? "_composite" : "")}";
            string extraItems = composite
                ? """<ProjectReference Include="../R2rSuffixLibrary/R2rSuffixLibrary.csproj" />"""
                : string.Empty;
            string compositeProperty = composite ? string.Empty : "<PublishReadyToRunComposite>false</PublishReadyToRunComposite>";
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, label,
                extraProperties: $"<PublishReadyToRun>true</PublishReadyToRun>{compositeProperty}<PublishTrimmed>{(trimmed ? "true" : "false")}</PublishTrimmed>",
                extraItems: extraItems);
            if (composite)
                LogR2RSuffixLibraryMarker();
            string extraArgs = GetR2RBuildArgs(config, composite);
            if (nativeRelink)
            {
                // CoreCLR relinks dotnet.native.wasm via the in-tree targets + EMSDK_PATH, not the browser
                // workload; WasmBuildNative=true otherwise forces UsingBrowserRuntimeWorkload=true, which
                // demands the (uninstalled) wasm-tools workload and disables the CoreCLR relink targets.
                extraArgs += " -p:WasmBuildNative=true -p:UsingBrowserRuntimeWorkload=false";
            }
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs),
                // Assert the native runtime was actually relinked (from obj), not the runtime-pack prebuilt,
                // so the relink is proven rather than silently skipped. See dotnet/runtime#133185.
                isNativeBuild: nativeRelink ? true : (bool?)null);

            string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
            if (composite)
                AssertCompositeReadyToRun(frameworkDir);
            else
                AssertCoreLibReadyToRun(frameworkDir, expectReadyToRun: true);
            AssertNoDuplicateAssemblies(frameworkDir);
            AssertNoManagedAssembliesOutsideFramework(frameworkDir);
            AssertTrimmedClosureIsFullyStaged(config, frameworkDir);
            AssertPerAppCrossgenRan(config, expected: true);
            bool expectStrip = config != Configuration.Debug;
            AssertCrossgen2DebugInfoArgs(config,
                GetCrossgen2ExtraCommandLineArgs(Path.Combine(_logPath, $"{info.ProjectName}-publish.binlog")),
                expectStripInliningInfo: expectStrip, expectStripDebugInfo: expectStrip);

            if (trimmed && !nativeRelink)
            {
                BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
                if (composite)
                    AssertCompositeReadyToRun(frameworkDir);
                AssertNoDuplicateAssemblies(frameworkDir);
                if (composite)
                    AssertSwitchingCompositeModeRecompiles(info, config, extraArgs, frameworkDir);
            }

            bool suffixLibraryLoaded = false;
            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                OnConsoleMessage: (_, msg) => suffixLibraryLoaded |= msg.Contains(SuffixLibraryLoadedMessage),
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
            if (composite)
                Assert.True(suffixLibraryLoaded, $"'{SuffixLibraryLoadedMessage}' was not logged; R2rSuffixLibrary.r2r did not load as a component assembly.");
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        [TestCategory("no-workload")]
        public void FrameworkAssembliesAreNotReadyToRunWhenDisabled(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_off",
                extraProperties: "<PublishReadyToRun>false</PublishReadyToRun>");
            BlazorBuild(info, config);

            AssertCoreLibReadyToRun(GetBuildWebcilDir(config), expectReadyToRun: false);
            AssertPerAppCrossgenRan(config, expected: false);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(false)]
        [InlineData(true)]
        [TestCategory("no-workload")]
        public void PerformanceInstrumentationWarnsForBuild(bool readyToRun)
        {
            ProjectInfo info = CopyTestAsset(
                Configuration.Debug,
                aot: false,
                TestAsset.BlazorBasicTestApp,
                $"profiling_build_r2r_{readyToRun}",
                extraProperties: $"""
                    <EnableDiagnostics>true</EnableDiagnostics>
                    <PublishReadyToRun>{readyToRun}</PublishReadyToRun>
                    <WasmPerformanceInstrumentation>all</WasmPerformanceInstrumentation>
                    """);

            (string _, string output) = BlazorBuild(
                info,
                Configuration.Debug,
                new BuildOptions(AssertAppBundle: false,
                    ExtraMSBuildArgs: "/warnaserror:false -p:UsingBrowserRuntimeWorkload=false"));

            Assert.Contains("WasmPerformanceInstrumentation during build provides incomplete sampling coverage", output);
            Assert.Contains("Publish the application for complete instrumentation coverage.", output);
        }

        // PublishReadyToRunPgoFiles drives a crossgen2 --partial image: only the profiled subset is precompiled,
        // the rest is interpreted at runtime. Republish the same project through full, roots-only partial,
        // profiled partial, and full states. This proves that the profile contributes methods, property-only
        // changes invalidate incremental R2R output in both directions, and interpreter fallback covers methods
        // left out of the eager image.
        [Theory]
        [InlineData(Configuration.Release)]
        [TestCategory("no-workload")]
        public async Task PublishPartialProfileRunsAllPages(Configuration config)
        {
            string profile = Path.Combine(BuildEnvironment.TestDataPath, "DotNet_Blazor_Wasm.mibc");
            Assert.True(File.Exists(profile), $"Missing test profile '{profile}'.");
            ProjectInfo info = CopyTestAsset(
                config,
                aot: false,
                TestAsset.BlazorBasicTestApp,
                "r2r_partial_profile",
                extraProperties: """
                    <PublishReadyToRun>true</PublishReadyToRun>
                    <PublishTrimmed>false</PublishTrimmed>
                    <PublishReadyToRunCrossgen2ExtraArgs Condition="'$(WasmBuildTestRootsOnlyPartial)' == 'true'">$(PublishReadyToRunCrossgen2ExtraArgs);--partial</PublishReadyToRunCrossgen2ExtraArgs>
                    """,
                extraItems: """<PublishReadyToRunPgoFiles Include="$(WasmBuildTestReadyToRunProfile)" Condition="'$(WasmBuildTestReadyToRunProfile)' != ''" />""");

            string frameworkDir = PublishR2RClosure(info, config, extraArgs: "", label: "full");
            AssertCoreLibReadyToRun(frameworkDir, expectReadyToRun: true);
            string r2rDir = GetObjSubDir(config, "R2R");
            long fullBytes = SumR2RTableBytes(r2rDir);
            Assert.True(fullBytes > 0, "Full-closure publish produced no R2R tables.");

            frameworkDir = PublishR2RClosure(
                info,
                config,
                extraArgs: "-p:WasmBuildTestRootsOnlyPartial=true",
                label: "roots-only");
            long rootsOnlyBytes = SumR2RTableBytes(r2rDir);
            Assert.True(rootsOnlyBytes > 0, "Roots-only partial image produced no R2R tables.");
            Assert.NotEqual(fullBytes, rootsOnlyBytes);

            frameworkDir = PublishR2RClosure(
                info,
                config,
                extraArgs: $"-p:WasmBuildTestReadyToRunProfile=\"{profile}\"",
                label: "profiled");
            AssertCoreLibReadyToRun(frameworkDir, expectReadyToRun: true);
            AssertPerAppCrossgenRan(config, expected: true);

            long profiledBytes = SumR2RTableBytes(r2rDir);
            Assert.NotEqual(rootsOnlyBytes, profiledBytes);
            Assert.NotEqual(fullBytes, profiledBytes);

            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));

            PublishR2RClosure(info, config, extraArgs: "", label: "full-restored");
            long restoredFullBytes = SumR2RTableBytes(r2rDir);
            Assert.Equal(fullBytes, restoredFullBytes);
        }

        private string PublishR2RClosure(ProjectInfo info, Configuration config, string extraArgs, string label)
        {
            string r2rArgs = $"{GetR2RBuildArgs(config)} {extraArgs}";
            BlazorPublish(
                info,
                config,
                new PublishOptions(
                    UseCache: false,
                    Label: label,
                    ExtraMSBuildArgs: r2rArgs));
            return GetBlazorBinFrameworkDir(config, forPublish: true);
        }

        [Fact]
        [TestCategory("no-workload")]
        public void DebugBuildRestagesFrameworkWhenReadyToRunChanges()
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_debug_rebuild");
            (_, string output) = BlazorBuild(info, config);
            string webcilDir = GetBuildWebcilDir(config);
            AssertPrebuiltFrameworkReadyToRun(webcilDir, output);

            BlazorBuild(info, config,
                new BuildOptions(UseCache: false, ExtraMSBuildArgs: "-p:PublishReadyToRun=false"));
            AssertCoreLibReadyToRun(webcilDir, expectReadyToRun: false);

            (_, output) = BlazorBuild(info, config, new BuildOptions(UseCache: false));
            AssertPrebuiltFrameworkReadyToRun(webcilDir, output);
            AssertPerAppCrossgenRan(config, expected: false);
        }

        [Theory]
        [InlineData(Configuration.Release)]
        [TestCategory("no-workload")]
        public void CompositeRequiresWebcil(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_composite_no_webcil",
                extraProperties: "<PublishReadyToRun>true</PublishReadyToRun><PublishReadyToRunComposite>true</PublishReadyToRunComposite><WasmEnableWebcil>false</WasmEnableWebcil>");
            (string _, string output) = BlazorPublish(info, config,
                new PublishOptions(ExpectSuccess: false, ExtraMSBuildArgs: GetR2RBuildArgs(config, composite: true)));
            Assert.Contains("PublishReadyToRunComposite for CoreCLR browser-wasm requires WebCIL-in-Wasm assemblies", output);
        }

        // An unnormalized intermediate path (e.g. ArtifactsPath=$(MSBuildThisFileDirectory)../artifacts) leaves '..'
        // segments in OutputR2RImage. The stale-image prune must still recognize the freshly emitted images.
        // A '//' link dir (no backslash, so MSBuild keeps it on Unix) makes globbed linked items differ in spelling.
        [Theory]
        [InlineData(Configuration.Release, /*composite*/ false)]
        [InlineData(Configuration.Release, /*composite*/ true)]
        [TestCategory("no-workload")]
        public void PublishWithUnnormalizedIntermediatePathKeepsReadyToRunImages(Configuration config, bool composite)
        {
            string extraItems = composite
                ? """<ProjectReference Include="../R2rSuffixLibrary/R2rSuffixLibrary.csproj" />"""
                : string.Empty;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, $"r2r_unnormalized{(composite ? "_composite" : "")}",
                extraProperties: $"<PublishReadyToRun>true</PublishReadyToRun><PublishReadyToRunComposite>{composite.ToString().ToLowerInvariant()}</PublishReadyToRunComposite><PublishTrimmed>true</PublishTrimmed>" +
                    "<IntermediateOutputPath>$(MSBuildProjectDirectory)/obj/..//obj/$(Configuration)/</IntermediateOutputPath>" +
                    "<IntermediateLinkDir>$(IntermediateOutputPath)$(TargetFramework)/linked/</IntermediateLinkDir>",
                extraItems: extraItems);
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: GetR2RBuildArgs(config, composite)));

            string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
            if (composite)
                AssertCompositeReadyToRun(frameworkDir);
            else
                AssertCoreLibReadyToRun(frameworkDir, expectReadyToRun: true);
            AssertPerAppCrossgenRan(config, expected: true);
        }

        // Navigate Home -> Counter (increment 0 -> 1) -> Weather (forecast rows) -> Home, asserting content
        // at each step. DetectRuntimeFailures (default) fails the run on any unhandled managed/JS exception.
        private static async Task InteractAllPagesAsync(IPage page)
        {
            var counterLink = page.Locator("text=Counter");
            await counterLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = InteractionTimeoutMs });
            await counterLink.ClickAsync(new() { Timeout = InteractionTimeoutMs });

            var status = page.Locator("p[role='status']");
            await status.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = InteractionTimeoutMs });
            Assert.Equal("Current count: 0", await status.InnerHTMLAsync());

            var clickMe = page.Locator("text=\"Click me\"");
            await clickMe.ClickAsync(new() { Timeout = InteractionTimeoutMs });
            await page.WaitForFunctionAsync(
                """selector => document.querySelector(selector)?.textContent?.trim() === 'Current count: 1'""",
                "p[role='status']",
                new() { Timeout = InteractionTimeoutMs });

            var weatherLink = page.Locator("text=Weather");
            await weatherLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = InteractionTimeoutMs });
            await weatherLink.ClickAsync(new() { Timeout = InteractionTimeoutMs });
            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('table tbody tr').length > 0",
                null,
                new() { Timeout = InteractionTimeoutMs });

            var homeLink = page.Locator("text=Home");
            await homeLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = InteractionTimeoutMs });
            await homeLink.ClickAsync(new() { Timeout = InteractionTimeoutMs });
            await page.Locator("h1").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = InteractionTimeoutMs });
        }

        private string GetBuildWebcilDir(Configuration config) =>
            Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFrameworkForBlazor, "webcil");

        private string GetObjSubDir(Configuration config, string name) =>
            Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFrameworkForBlazor, name);

        // Static web assets are fingerprinted as <name>.<10 chars>.wasm. The pattern is deliberately
        // case-sensitive: a case-insensitive match also eats real trailing segments like ".Components".
        private static string StripFingerprint(string filePath)
            => Regex.Replace(Path.GetFileNameWithoutExtension(filePath), @"\.[a-z0-9]{10}$", string.Empty);

        private static string[] GetStagedAssemblyNames(string frameworkDir)
            => Directory.EnumerateFiles(frameworkDir, "*.wasm")
                .Where(f => !Path.GetFileName(f).StartsWith("dotnet", System.StringComparison.Ordinal))
                .Select(StripFingerprint)
                .ToArray();

        // Fingerprinted assets land beside their predecessors instead of replacing them, so a stale copy of an
        // assembly survives as a second file and the runtime can bind the wrong version bubble.
        private static void AssertNoDuplicateAssemblies(string frameworkDir)
        {
            string[] duplicates = GetStagedAssemblyNames(frameworkDir)
                .GroupBy(n => n, System.StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key} x{g.Count()}")
                .OrderBy(n => n, System.StringComparer.Ordinal)
                .ToArray();

            Assert.True(duplicates.Length == 0,
                $"Duplicate assemblies staged in '{frameworkDir}': {string.Join(", ", duplicates)}");
        }

        // A .wasm-named R2R image that is not routed back to a managed asset is treated as native and lands in
        // the publish root, leaving the boot config without it. See dotnet/runtime#121257.
        private static void AssertNoManagedAssembliesOutsideFramework(string frameworkDir)
        {
            string? wwwrootDir = Path.GetDirectoryName(frameworkDir);
            if (wwwrootDir is null || !Directory.Exists(wwwrootDir))
                return;

            string[] stray = Directory.EnumerateFiles(wwwrootDir, "*.wasm").Select(Path.GetFileName).ToArray()!;
            Assert.True(stray.Length == 0,
                $"Managed assemblies leaked outside _framework into '{wwwrootDir}': {string.Join(", ", stray)}");
        }

        // Losing every crossgen'd assembly still exits 0 and can still leave a loadable-looking bundle, so
        // compare the staged set against the linker's closure rather than trusting the exit code.
        private void AssertTrimmedClosureIsFullyStaged(Configuration config, string frameworkDir)
        {
            string linkedDir = GetObjSubDir(config, "linked");
            if (!Directory.Exists(linkedDir))
                return;

            HashSet<string> staged = new(GetStagedAssemblyNames(frameworkDir), System.StringComparer.Ordinal);
            string[] missing = Directory.EnumerateFiles(linkedDir, "*.dll")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !staged.Contains(name!))
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToArray()!;

            Assert.True(missing.Length == 0,
                $"Assemblies in the trimmed closure but missing from '{frameworkDir}': {string.Join(", ", missing)}");
        }

        // The dev loop serves the runtime pack's prebuilt native/r2r images; only publish crossgens per app.
        private void AssertPerAppCrossgenRan(Configuration config, bool expected)
        {
            string r2rDir = GetObjSubDir(config, "R2R");
            int imageCount = Directory.Exists(r2rDir) ? Directory.EnumerateFiles(r2rDir).Count() : 0;

            if (expected)
                Assert.True(imageCount > 0, $"Expected per-app ReadyToRun images under '{r2rDir}'.");
            else
                Assert.True(imageCount == 0, $"Expected no per-app crossgen2 output, found {imageCount} file(s) under '{r2rDir}'.");
        }

        private string EvaluatePublishReadyToRunCrossgen2ExtraArgs(Configuration config, string projectName, string extraMSBuildArgs)
        {
            string[] commonArgs = [$"-p:Configuration={config}", "-p:_IsPublishing=true", "-nr:false", extraMSBuildArgs];

            // MSBuild skips -restore when -getProperty is used without a target, so restore separately. The WebAssembly
            // SDK pack, which sets these arguments, is imported from the restored packages.
            using (ToolCommand restore = new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir))
            {
                restore.ExecuteWithCapturedOutput(["msbuild", "-t:Restore", $"-bl:{Path.Combine(_logPath, $"{projectName}-restore.binlog")}", .. commonArgs])
                    .EnsureSuccessful();
            }

            List<string> stdout = [];
            using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .WithOutputDataReceived(line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        stdout.Add(line);
                });
            cmd.ExecuteWithCapturedOutput(["msbuild", "-getProperty:PublishReadyToRunCrossgen2ExtraArgs",
                    $"-bl:{Path.Combine(_logPath, $"{projectName}-evaluate.binlog")}", .. commonArgs])
                .EnsureSuccessful();
            return Assert.Single(stdout);
        }

        private static void AssertCrossgen2DebugInfoArgs(Configuration config, string crossgen2Args, bool expectStripInliningInfo, bool expectStripDebugInfo)
        {
            string expectedWasmDebugInfo = config == Configuration.Debug ? "--wasm-debug-info=all" : "--wasm-debug-info=none";
            Assert.Contains(expectedWasmDebugInfo, crossgen2Args);
            Assert.Equal(expectStripInliningInfo, crossgen2Args.Contains("--strip-inlining-info", System.StringComparison.Ordinal));
            Assert.Equal(expectStripDebugInfo, crossgen2Args.Contains("--strip-debug-info", System.StringComparison.Ordinal));
            Assert.Contains("--opt-cross-module:*", crossgen2Args);
        }

        private static string GetCrossgen2ExtraCommandLineArgs(string binlogPath)
        {
            SL.Build build = SL.BinaryLog.ReadBuild(binlogPath);
            List<string> extraArgs = [];
            build.VisitAllChildren<SL.Task>(task =>
            {
                if (task.Name != "RunReadyToRunCompiler")
                    return;

                task.VisitAllChildren<SL.Property>(parameter =>
                {
                    if (parameter.Name == "Crossgen2ExtraCommandLineArgs")
                        extraArgs.Add(parameter.Value);
                });
            });

            Assert.NotEmpty(extraArgs);
            return string.Join(" ", extraArgs);
        }

        // Total table bytes across the canonical per-app R2R outputs. The base SDK names them .dll while the
        // wasm-aware task shim names them .wasm. Measuring the fingerprinted publish directory would count
        // files retained from earlier incremental publishes.
        private static long SumR2RTableBytes(string r2rDir)
        {
            long total = 0;
            foreach (string image in Directory.EnumerateFiles(r2rDir)
                .Where(path => Path.GetExtension(path) is ".dll" or ".wasm"))
            {
                total += GetReadyToRunTableSize(image);
            }
            return total;
        }

        private static int GetReadyToRunTableSize(string webcilPath)
        {
            using FileStream stream = File.OpenRead(webcilPath);
            Assert.True(WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out string? failureReason), failureReason);
            return tableSize;
        }

        private static void AssertCoreLibReadyToRun(string frameworkDir, bool expectReadyToRun)
        {
            string? coreLib = Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm").FirstOrDefault();
            Assert.True(coreLib is not null, $"Expected a System.Private.CoreLib webcil under '{frameworkDir}'.");

            int tableSize = GetReadyToRunTableSize(coreLib!);
            if (expectReadyToRun)
                Assert.True(tableSize > 0, $"Expected a ReadyToRun table in '{coreLib}', but the R2R table size was 0.");
            else
                Assert.Equal(0, tableSize);
        }

        private static void AssertPrebuiltFrameworkReadyToRun(string webcilDir, string buildOutput)
        {
            string? runtimePackDir = ProjectProviderBase.TryGetRuntimePackDirFromBuildOutput(buildOutput);
            Assert.NotNull(runtimePackDir);
            string prebuiltDir = Path.Combine(runtimePackDir!, "runtimes", "browser-wasm", "native", "r2r");
            Assert.True(Directory.Exists(prebuiltDir), $"Expected prebuilt ReadyToRun images in '{prebuiltDir}'.");
            AssertCoreLibReadyToRun(webcilDir, expectReadyToRun: true);
            Assert.True(File.Exists(Path.Combine(webcilDir, "BlazorBasicTestApp.wasm")));

            foreach (string webcilPath in Directory.EnumerateFiles(webcilDir, "*.wasm"))
            {
                string prebuiltPath = Path.Combine(prebuiltDir, Path.GetFileName(webcilPath));
                if (File.Exists(prebuiltPath))
                    Assert.Equal(File.ReadAllBytes(prebuiltPath), File.ReadAllBytes(webcilPath));
                else
                    Assert.Equal(0, GetReadyToRunTableSize(webcilPath));
            }
        }

        // Component stubs share the per-assembly <name>.wasm output names, so a mode switch must recompile them
        // instead of treating them as up-to-date per-assembly images that probe for a pruned composite owner.
        // Switch back afterwards so the caller still runs the composite app. Only obj is incremental here: clear the
        // published _framework so each mode's fingerprinted assets don't accumulate beside the other's.
        private void AssertSwitchingCompositeModeRecompiles(ProjectInfo info, Configuration config, string extraArgs, string frameworkDir)
        {
            string coreLibImage = Path.Combine(GetObjSubDir(config, "R2R"), "System.Private.CoreLib.wasm");
            System.DateTime compositeStubTime = File.GetLastWriteTimeUtc(coreLibImage);

            Directory.Delete(frameworkDir, recursive: true);
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: $"{extraArgs} -p:PublishReadyToRunComposite=false"));
            Assert.True(File.GetLastWriteTimeUtc(coreLibImage) > compositeStubTime,
                $"'{coreLibImage}' was not recompiled after switching from composite to per-assembly ReadyToRun.");

            Directory.Delete(frameworkDir, recursive: true);
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
            AssertCompositeReadyToRun(frameworkDir);
        }

        // The boot config flags exactly the composite owner, delivered via coreAssembly under its crossgen2 name so
        // component stubs can probe for it; an assembly merely named *.r2r stays an ordinary, unflagged assembly.
        private void AssertCompositeReadyToRun(string frameworkDir)
        {
            AssetsData assets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
            WebcilAsset composite = Assert.Single(assets.coreAssembly, asset => asset.isCompositeImage == true);
            Assert.Equal("BlazorBasicTestApp.r2r.wasm", composite.virtualPath);
            if (EnvironmentVariables.UseFingerprinting)
                Assert.Matches(@"^BlazorBasicTestApp\.r2r\.[a-z0-9]{10}\.wasm$", composite.name);
            Assert.DoesNotContain(assets.assembly, asset => asset.isCompositeImage == true);
            Assert.Contains(assets.coreAssembly,
                asset => asset.virtualPath?.StartsWith("System.Private.CoreLib", System.StringComparison.Ordinal) == true);
            Assert.Contains(assets.assembly, asset => asset.virtualPath == "R2rSuffixLibrary.r2r.wasm");

            string compositePath = Path.Combine(frameworkDir, composite.name);
            Assert.True(GetReadyToRunTableSize(compositePath) > 0, $"Expected compiled methods in '{compositePath}'.");
            Assert.True(Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm").Any(),
                $"Expected a component stub for System.Private.CoreLib in '{frameworkDir}'.");
        }

        private const string SuffixLibraryLoadedMessage = "Loaded R2rSuffixLibrary.r2r: 42";

        // Make the app call into the R2rSuffixLibrary test asset (referenced for composite runs) so the run proves
        // it loaded as a component assembly rather than being mistaken for the composite owner.
        private void LogR2RSuffixLibraryMarker()
            => UpdateFile("Program.cs", new Dictionary<string, string>
            {
                { "var builder", "System.Console.WriteLine($\"Loaded {typeof(R2rSuffixLibraryMarker).Assembly.GetName().Name}: {R2rSuffixLibraryMarker.Value}\");\nvar builder" }
            });
    }
}
