// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.NET.WebAssembly.Webcil;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    // CoreCLR browser-wasm ships ReadyToRun images as webcil-in-wasm; a non-zero R2R table size in the
    // System.Private.CoreLib webcil is the marker that R2R was produced and staged. See dotnet/runtime#121257.
    // These tests play the role of the local A06-local-R2R-Blazor sample: R2R in both build and publish,
    // with and without IL trimming, driving all pages (Home/Counter/Weather) in a real browser with no
    // exceptions. CoreCLR only.
    public class ReadyToRunTests : BlazorWasmTestBase
    {
        private const int InteractionTimeoutMs = 60_000;

        public ReadyToRunTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
            _enablePerTestCleanup = true;
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release)]
        [TestCategory("no-workload")]
        public async Task BuildRunAllPages(Configuration config)
        {
            // A build stages the prebuilt framework R2R images from the runtime pack (no per-app crossgen2).
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_build",
                extraProperties: "<PublishReadyToRun>true</PublishReadyToRun>");
            BlazorBuild(info, config);

            AssertCoreLibReadyToRun(GetBuildWebcilDir(config), expectReadyToRun: true);

            await RunForBuildWithDotnetRun(new BlazorRunOptions(config,
                CheckCounter: false,
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [InlineData(Configuration.Release, /*trimmed*/ false)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPages(Configuration config, bool trimmed)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: false);

        // Native relink does not trigger for CoreCLR Blazor apps: the relink targets gate on
        // IsBrowserWasmProject, which Blazor leaves unset (it resolves the wasm RID late). See the issue.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/133185", typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPagesNativeRelink(Configuration config, bool trimmed)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: true);

        private async Task PublishRunAllPagesCore(Configuration config, bool trimmed, bool nativeRelink)
        {
            // Publish runs per-app crossgen2: trimmed => per-app R2R closure incl. a trimmed CoreLib;
            // untrimmed => the runtime-pack R2R CoreLib. nativeRelink also relinks dotnet.native.wasm.
            string label = $"r2r_pub_{(trimmed ? "trim" : "notrim")}{(nativeRelink ? "_native" : "")}";
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, label,
                extraProperties: $"<PublishReadyToRun>true</PublishReadyToRun><PublishTrimmed>{(trimmed ? "true" : "false")}</PublishTrimmed>");
            string extraArgs = GetR2RBuildArgs(config);
            if (nativeRelink)
            {
                // CoreCLR relinks dotnet.native.wasm via the in-tree targets + EMSDK_PATH, not the browser
                // workload; WasmBuildNative=true otherwise forces UsingBrowserRuntimeWorkload=true, which
                // demands the (uninstalled) wasm-tools workload and disables the CoreCLR relink targets.
                extraArgs += " -p:WasmBuildNative=true -p:UsingBrowserRuntimeWorkload=false";
            }
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));

            AssertCoreLibReadyToRun(GetBlazorBinFrameworkDir(config, forPublish: true), expectReadyToRun: true);

            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release)]
        [TestCategory("no-workload")]
        public void FrameworkAssembliesAreNotReadyToRunWhenDisabled(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_off",
                extraProperties: "<PublishReadyToRun>false</PublishReadyToRun>");
            BlazorBuild(info, config);

            AssertCoreLibReadyToRun(GetBuildWebcilDir(config), expectReadyToRun: false);
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

        // In-tree publish crossgen2: the base SDK can't resolve a wasm crossgen2 and emits composite R2R
        // (which strips the assembly manifest and won't load), so (1) point the CoreCLR R2R override
        // (Microsoft.NET.Sdk.WebAssembly.Browser.CoreCLR.ReadyToRun.targets) at the crossgen2 built under
        // BASE_DIR/coreclr, and (2) activate the runtime's wasm-aware Crossgen2Tasks shim (non-composite) via
        // Crossgen2SdkOverride{Props,Targets}Path. All inert if BASE_DIR / the directories aren't present.
        private static string GetR2RBuildArgs(Configuration config)
        {
            string? baseDir = EnvironmentVariables.BaseDir;
            if (string.IsNullOrEmpty(baseDir))
                return string.Empty;

            string hostArch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            string crossgenDir = Path.Combine(baseDir, "coreclr", $"browser.wasm.{config}", hostArch, "crossgen2");
            string shimDir = Path.Combine(baseDir, "Crossgen2Tasks", config.ToString());
            string shimProps = Path.Combine(shimDir, "Microsoft.NET.CrossGen.props");
            string shimTargets = Path.Combine(shimDir, "Microsoft.NET.CrossGen.targets");
            return $"-p:Crossgen2InBuildDir={crossgenDir} -p:Crossgen2SdkOverridePropsPath={shimProps} -p:Crossgen2SdkOverrideTargetsPath={shimTargets}";
        }

        private static void AssertCoreLibReadyToRun(string frameworkDir, bool expectReadyToRun)
        {
            string? coreLib = Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm").FirstOrDefault();
            Assert.True(coreLib is not null, $"Expected a System.Private.CoreLib webcil under '{frameworkDir}'.");

            using FileStream stream = File.OpenRead(coreLib!);
            bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out string? failureReason);
            Assert.True(ok, failureReason);

            if (expectReadyToRun)
                Assert.True(tableSize > 0, $"Expected a ReadyToRun table in '{coreLib}', but the R2R table size was 0.");
            else
                Assert.Equal(0, tableSize);
        }
    }
}
