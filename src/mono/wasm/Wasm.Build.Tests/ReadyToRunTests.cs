// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.WebAssembly;
using Microsoft.NET.WebAssembly.Webcil;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    // CoreCLR browser-wasm ships ReadyToRun images as webcil-in-wasm; a non-zero R2R table size in the
    // System.Private.CoreLib webcil is the marker that R2R was produced and staged. These tests cover R2R in
    // both build and publish, with and without IL trimming, driving all pages in a real browser. CoreCLR only.
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
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: false, composite: false);

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release, /*trimmed*/ true, /*nativeRelink*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ false, /*nativeRelink*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ true, /*nativeRelink*/ true)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPagesComposite(Configuration config, bool trimmed, bool nativeRelink)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink, composite: true);

        // CoreCLR relinks dotnet.native.wasm for Blazor when WasmBuildNative=true; the relink is driven by the
        // IsBrowserWasmProject triggers in BrowserWasmApp.CoreCLR.targets. AssertBundle(isNativeBuild: true)
        // proves the served dotnet.native.wasm was relinked rather than the runtime-pack prebuilt.
        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [InlineData(Configuration.Release, /*trimmed*/ false)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPagesNativeRelink(Configuration config, bool trimmed)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: true, composite: false);

        private async Task PublishRunAllPagesCore(Configuration config, bool trimmed, bool nativeRelink, bool composite)
        {
            // Publish runs per-app crossgen2 for the whole closure, trimmed or not: even the untrimmed CoreLib
            // is a per-app image, not the runtime pack's. nativeRelink also relinks dotnet.native.wasm.
            string label = $"r2r_pub_{(trimmed ? "trim" : "notrim")}{(nativeRelink ? "_native" : "")}{(composite ? "_composite" : "")}";
            string extraItems = composite
                ? """<ProjectReference Include="../R2rSuffixLibrary/R2rSuffixLibrary.csproj" />"""
                : string.Empty;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, label,
                extraProperties: $"<PublishReadyToRun>true</PublishReadyToRun><PublishReadyToRunComposite>{(composite ? "true" : "false")}</PublishReadyToRunComposite><PublishTrimmed>{(trimmed ? "true" : "false")}</PublishTrimmed>",
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

            if (trimmed && !nativeRelink)
            {
                BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
                if (composite)
                    AssertCompositeReadyToRun(frameworkDir);
                AssertNoDuplicateAssemblies(frameworkDir);
            }

            bool suffixLibraryLoaded = false;
            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                OnConsoleMessage: (_, msg) => suffixLibraryLoaded |= msg.Contains(SuffixLibraryLoadedMessage),
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
            if (composite)
                Assert.True(suffixLibraryLoaded, $"'{SuffixLibraryLoadedMessage}' was not logged; R2rSuffixLibrary.r2r did not load as a component assembly.");
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

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
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

        // Wire the in-build crossgen2 when this leg shipped it, and for composite the wasm-aware Crossgen2Tasks shim:
        // composite needs the shim's wasm output naming (<entry>.r2r.wasm owner, <name>.wasm stubs), which the base
        // SDK's ReadyToRun tasks don't implement yet (dotnet/sdk#56395). Per-assembly R2R keeps the base SDK tasks so
        // that path stays covered. Each is passed only when present under BASE_DIR: the no-workload leg resolves
        // crossgen2 itself from the SDK pack (the SDK restores it when PublishReadyToRun is set), so passing a
        // non-existent Crossgen2InBuildDir there would break the call-helpers generator. All inert if BASE_DIR is unset.
        private static string GetR2RBuildArgs(Configuration config, bool composite)
        {
            string? baseDir = EnvironmentVariables.BaseDir;
            if (string.IsNullOrEmpty(baseDir))
                return string.Empty;

            string hostArch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            string crossgenDir = Path.Combine(baseDir, "coreclr", $"browser.wasm.{config}", hostArch, "crossgen2");
            string shimDir = Path.Combine(baseDir, "Crossgen2Tasks", config.ToString());
            string shimProps = Path.Combine(shimDir, "Microsoft.NET.CrossGen.props");
            string shimTargets = Path.Combine(shimDir, "Microsoft.NET.CrossGen.targets");

            var args = new List<string>();
            if (Directory.Exists(crossgenDir))
                args.Add($"-p:Crossgen2InBuildDir=\"{crossgenDir}\"");
            if (composite && File.Exists(shimProps))
                args.Add($"-p:Crossgen2SdkOverridePropsPath=\"{shimProps}\"");
            if (composite && File.Exists(shimTargets))
                args.Add($"-p:Crossgen2SdkOverrideTargetsPath=\"{shimTargets}\"");
            return string.Join(" ", args);
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
