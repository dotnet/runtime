// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

            string webcilDir = GetBuildWebcilDir(config);
            AssertCoreLibReadyToRun(webcilDir, expectReadyToRun: true);
            AssertNoDuplicateAssemblies(webcilDir);
            AssertPerAppCrossgenRan(config, expected: false);

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

        // CoreCLR relinks dotnet.native.wasm for Blazor when WasmBuildNative=true. The relink triggers
        // (BrowserWasmApp.CoreCLR.targets) key on WasmBuildNative in addition to the bare-RID
        // IsBrowserWasmProject, so a late-resolved wasm RID cannot make the relink a silent no-op. See
        // dotnet/runtime#133185.
        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [InlineData(Configuration.Release, /*trimmed*/ false)]
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
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs),
                // Assert the native runtime was actually relinked (from obj), not the runtime-pack prebuilt,
                // so the relink is proven rather than silently skipped. See dotnet/runtime#133185.
                isNativeBuild: nativeRelink ? true : (bool?)null);

            string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
            AssertCoreLibReadyToRun(frameworkDir, expectReadyToRun: true);
            AssertNoDuplicateAssemblies(frameworkDir);
            AssertNoManagedAssembliesOutsideFramework(frameworkDir);
            AssertTrimmedClosureIsFullyStaged(config, frameworkDir);
            AssertPerAppCrossgenRan(config, expected: true);

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
            AssertPerAppCrossgenRan(config, expected: false);
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
