// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.NET.Sdk.WebAssembly;
using Microsoft.NET.WebAssembly.Webcil;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using SL = Microsoft.Build.Logging.StructuredLogger;
using TaskItem = Microsoft.Build.Utilities.TaskItem;

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
        [InlineData(Configuration.Release, /*trimmed*/ true)]
        [InlineData(Configuration.Release, /*trimmed*/ false)]
        [TestCategory("no-workload")]
        public Task PublishRunAllPagesComposite(Configuration config, bool trimmed)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: false, composite: true);

        [Fact]
        [TestCategory("no-workload")]
        public Task PublishCompositeWithoutBundle()
            => PublishRunAllPagesCore(Configuration.Release, trimmed: true, nativeRelink: false, composite: true, bundle: false);

        [Fact]
        [TestCategory("no-workload")]
        public Task PublishCompositeUsingSdkTargets()
            => PublishRunAllPagesCore(Configuration.Release, trimmed: true, nativeRelink: false, composite: true, useSdkTargets: true);

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData(Configuration.Release, /*trimmed*/ true, /*composite*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ false, /*composite*/ false)]
        [InlineData(Configuration.Release, /*trimmed*/ true, /*composite*/ true)]
        [TestCategory("workload")]
        public Task PublishRunAllPagesNativeRelink(Configuration config, bool trimmed, bool composite)
            => PublishRunAllPagesCore(config, trimmed, nativeRelink: true, composite: composite);

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
        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData(Configuration.Release)]
        [TestCategory("wasm-opt"), TestCategory("workload")]
        public async Task PublishRunAllPagesCompositeWithWorkload(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "r2r_pub_composite_workload",
                extraProperties: "<PublishReadyToRun>true</PublishReadyToRun><PublishReadyToRunComposite>true</PublishReadyToRunComposite>",
                extraItems: """<ProjectReference Include="../R2rSuffixLibrary/R2rSuffixLibrary.csproj" />""");
            LogR2RSuffixLibraryMarker();

            string extraArgs = GetR2RBuildArgs(config, composite: true);
            (string _, string publishOutput) = BlazorPublish(info, config,
                new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs), isNativeBuild: true);
            string r2rDir = GetObjSubDir(config, "R2R");
            string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
            AssertReadyToRunWasmOpt(r2rDir, publishOutput, composite: true);
            AssertCompositeReadyToRun(frameworkDir);
            Assert.True(File.Exists(Path.Combine(GetObjSubDir(config, "wasm"), "for-publish", "dotnet.native.wasm")));

            if (EnvironmentVariables.UseWasmOpt)
            {
                string owner = Path.Combine(r2rDir, "BlazorBasicTestApp.r2r.wasm");
                string bundle = Path.Combine(r2rDir, "BlazorBasicTestApp.components.wasm");
                string stamp = owner + ".wasm-opt.stamp";
                System.DateTime ownerTime = File.GetLastWriteTimeUtc(owner);
                System.DateTime bundleTime = File.GetLastWriteTimeUtc(bundle);
                System.DateTime stampTime = File.GetLastWriteTimeUtc(stamp);

                (_, publishOutput) = BlazorPublish(info, config,
                    new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs), isNativeBuild: true);
                Assert.Equal(ownerTime, File.GetLastWriteTimeUtc(owner));
                Assert.Equal(bundleTime, File.GetLastWriteTimeUtc(bundle));
                Assert.Equal(stampTime, File.GetLastWriteTimeUtc(stamp));
                Assert.DoesNotContain("Running wasm-opt on", publishOutput);

                File.SetLastWriteTimeUtc(bundle, System.DateTime.UnixEpoch);
                (_, publishOutput) = BlazorPublish(info, config,
                    new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs), isNativeBuild: true);
                Assert.True(File.GetLastWriteTimeUtc(owner) > ownerTime, "A stale bundle must recompile the optimized composite image.");
                AssertReadyToRunWasmOpt(r2rDir, publishOutput, composite: true);
                AssertCompositeReadyToRun(frameworkDir);
            }

            bool suffixLibraryLoaded = false;
            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                OnConsoleMessage: (_, msg) => suffixLibraryLoaded |= msg.Contains(SuffixLibraryLoadedMessage),
                ExecuteAfterLoaded: (_, page) => InteractAllPagesAsync(page)));
            Assert.True(suffixLibraryLoaded, $"'{SuffixLibraryLoadedMessage}' was not logged; R2rSuffixLibrary.r2r did not load as a component assembly.");
        }

        private async Task PublishRunAllPagesCore(Configuration config, bool trimmed, bool nativeRelink, bool composite, bool bundle = true, bool useSdkTargets = false)
        {
            // Publish runs per-app crossgen2 for the whole closure, trimmed or not: even the untrimmed CoreLib
            // is a per-app image, not the runtime pack's. nativeRelink also relinks dotnet.native.wasm.
            string label = $"r2r_pub_{(trimmed ? "trim" : "notrim")}{(nativeRelink ? "_native" : "")}{(composite ? "_composite" : "")}{(bundle ? "" : "_no_bundle")}{(useSdkTargets ? "_sdk_targets" : "")}";
            string extraItems = composite
                ? """<ProjectReference Include="../R2rSuffixLibrary/R2rSuffixLibrary.csproj" />"""
                : string.Empty;
            string compositeProperty = composite ? string.Empty : "<PublishReadyToRunComposite>false</PublishReadyToRunComposite>";
            string bundleProperty = bundle ? string.Empty : "<WasmBundleCompositeComponents>false</WasmBundleCompositeComponents>";
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, label,
                extraProperties: $"<PublishReadyToRun>true</PublishReadyToRun>{compositeProperty}<PublishTrimmed>{(trimmed ? "true" : "false")}</PublishTrimmed>{bundleProperty}",
                extraItems: extraItems);
            if (composite)
                LogR2RSuffixLibraryMarker();
            string extraArgs = GetR2RBuildArgs(config, composite && !useSdkTargets);
            if (useSdkTargets)
                extraArgs += " -p:CompressionEnabled=true";
            if (nativeRelink)
                extraArgs += " -p:WasmBuildNative=true";
            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs),
                isNativeBuild: nativeRelink ? true : (bool?)null);
            if (nativeRelink)
                Assert.True(File.Exists(Path.Combine(GetObjSubDir(config, "wasm"), "for-publish", "dotnet.native.wasm")));

            string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
            if (composite)
                AssertCompositeReadyToRun(frameworkDir, bundle);
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

            string[] initialCompositeFiles = [];
            string userFile = Path.Combine(frameworkDir, "user-owned.txt");
            if (useSdkTargets)
            {
                AssetsData assets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
                initialCompositeFiles = assets.coreAssembly
                    .Where(asset => asset.isCompositeImage == true || asset.isWebcilBundle == true)
                    .Select(asset => Path.Combine(frameworkDir, asset.name))
                    .ToArray();
                Assert.Equal(2, initialCompositeFiles.Length);
                foreach (string path in initialCompositeFiles)
                {
                    Assert.True(File.Exists(path + ".br"), $"Missing Brotli asset for '{path}'.");
                    Assert.True(File.Exists(path + ".gz"), $"Missing gzip asset for '{path}'.");
                }
                File.WriteAllText(userFile, "preserve");
            }

            string expectedSuffixMessage = SuffixLibraryLoadedMessage;
            if (trimmed && !nativeRelink)
            {
                string ownerImage = Path.Combine(GetObjSubDir(config, "R2R"), "BlazorBasicTestApp.r2r.wasm");
                string bundlePath = Path.Combine(GetObjSubDir(config, "R2R"), "BlazorBasicTestApp.components.wasm");
                if (composite && bundle)
                {
                    File.SetLastWriteTimeUtc(ownerImage, File.GetLastWriteTimeUtc(bundlePath).AddMilliseconds(1));
                    Assert.True(File.GetLastWriteTimeUtc(ownerImage) > File.GetLastWriteTimeUtc(bundlePath));
                }
                System.DateTime previousOwnerTime = composite ? File.GetLastWriteTimeUtc(ownerImage) : default;
                BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
                if (composite)
                {
                    AssertCompositeReadyToRun(frameworkDir, bundle);
                    Assert.Equal(previousOwnerTime, File.GetLastWriteTimeUtc(ownerImage));
                }
                AssertNoDuplicateAssemblies(frameworkDir);
                if (composite && bundle)
                {
                    byte[] staleBundle = File.ReadAllBytes(bundlePath);
                    string suffixSource = Path.Combine(_projectDir, "..", "R2rSuffixLibrary", "R2rSuffixLibraryMarker.cs");
                    string originalSource = File.ReadAllText(suffixSource);
                    string updatedSource = originalSource.Replace("=> 42;", "=> 43;");
                    Assert.NotEqual(originalSource, updatedSource);
                    File.WriteAllText(suffixSource, updatedSource);
                    BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
                    Assert.True(File.GetLastWriteTimeUtc(ownerImage) > previousOwnerTime, "Changing a component must recompile the composite image.");
                    AssertCompositeReadyToRun(frameworkDir);
                    AssertNoDuplicateAssemblies(frameworkDir);
                    if (useSdkTargets && EnvironmentVariables.UseFingerprinting)
                    {
                        foreach (string path in initialCompositeFiles)
                        {
                            Assert.False(File.Exists(path), $"Stale asset remains: '{path}'.");
                            Assert.False(File.Exists(path + ".br"), $"Stale Brotli asset remains: '{path}.br'.");
                            Assert.False(File.Exists(path + ".gz"), $"Stale gzip asset remains: '{path}.gz'.");
                        }
                    }
                    expectedSuffixMessage = "Loaded R2rSuffixLibrary.r2r: 43";

                    File.WriteAllBytes(bundlePath, staleBundle);
                    File.SetLastWriteTimeUtc(bundlePath, System.DateTime.UnixEpoch);
                    previousOwnerTime = File.GetLastWriteTimeUtc(ownerImage);
                    BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
                    Assert.True(File.GetLastWriteTimeUtc(ownerImage) > previousOwnerTime, "A stale component bundle must recompile the composite image.");
                    Assert.False(staleBundle.SequenceEqual(File.ReadAllBytes(bundlePath)), "The component bundle was not regenerated.");
                    AssertCompositeReadyToRun(frameworkDir);

                    File.Delete(bundlePath);
                    BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
                    Assert.True(File.Exists(bundlePath), "A missing component bundle must trigger composite recompilation.");
                    AssertCompositeReadyToRun(frameworkDir);
                }
                if (composite && bundle)
                {
                    AssertSwitchingBundleModeRecompiles(info, config, extraArgs, frameworkDir);
                    AssertSwitchingCompositeModeRecompiles(info, config, extraArgs, frameworkDir);
                }
            }
            if (useSdkTargets)
                Assert.Equal("preserve", File.ReadAllText(userFile));

            bool suffixLibraryLoaded = false;
            await RunForPublishWithWebServer(new BlazorRunOptions(config,
                CheckCounter: false,
                OnConsoleMessage: (_, msg) => suffixLibraryLoaded |= msg.Contains(expectedSuffixMessage),
                ExecuteAfterLoaded: async (_, page) =>
                {
                    await InteractAllPagesAsync(page);
                    if (composite && bundle)
                    {
                        int[] image = await page.EvaluateAsync<int[]>("""
                            () => {
                                const bytes = globalThis.getDotnetRuntime(0).INTERNAL.getLoadedAssemblyBytes("R2rSuffixLibrary.r2r.dll");
                                if (!bytes) return [];
                                bytes[0] = 0;
                                const copy = globalThis.getDotnetRuntime(0).INTERNAL.getLoadedAssemblyBytes("R2rSuffixLibrary.r2r.dll");
                                return copy ? Array.from(copy) : [];
                            }
                            """);
                        using var reader = new WebcilReader(new MemoryStream(image.Select(value => checked((byte)value)).ToArray()));
                        var metadata = reader.GetMetadataReader();
                        Assert.Equal("R2rSuffixLibrary.r2r", metadata.GetString(metadata.GetAssemblyDefinition().Name));
                    }
                }));
            if (composite)
                Assert.True(suffixLibraryLoaded, $"'{expectedSuffixMessage}' was not logged; R2rSuffixLibrary.r2r did not load as a component assembly.");
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
            string[] linked = Directory.EnumerateFiles(linkedDir, "*.dll").ToArray();
            AssetsData assets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
            WebcilAsset? bundle = assets.coreAssembly?.SingleOrDefault(asset => asset.isWebcilBundle == true);
            if (bundle is not null)
            {
                var filter = new FilterWebcilBundleComponents
                {
                    BuildEngine = new BundleTestBuildEngine(),
                    BundlePath = Path.Combine(frameworkDir, bundle.name),
                    Candidates = linked.Select(path => (ITaskItem)new TaskItem(path)).ToArray(),
                };
                Assert.True(filter.Execute());
                linked = filter.UnbundledCandidates.Select(item => item.ItemSpec).ToArray();
            }
            string[] missing = linked
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

        private void AssertSwitchingBundleModeRecompiles(ProjectInfo info, Configuration config, string extraArgs, string frameworkDir)
        {
            string ownerImage = Path.Combine(GetObjSubDir(config, "R2R"), "BlazorBasicTestApp.r2r.wasm");
            System.DateTime bundledTime = File.GetLastWriteTimeUtc(ownerImage);

            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: $"{extraArgs} -p:WasmBundleCompositeComponents=false"));
            AssertCompositeReadyToRun(frameworkDir, bundle: false);
            Assert.Empty(Directory.EnumerateFiles(frameworkDir, "BlazorBasicTestApp.components.*"));
            AssertNoDuplicateAssemblies(frameworkDir);
            System.DateTime unbundledTime = File.GetLastWriteTimeUtc(ownerImage);
            Assert.True(unbundledTime > bundledTime, "Disabling the Webcil bundle must recompile the composite image.");

            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
            AssertCompositeReadyToRun(frameworkDir);
            Assert.Empty(Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm*"));
            AssertNoDuplicateAssemblies(frameworkDir);
            Assert.True(File.GetLastWriteTimeUtc(ownerImage) > unbundledTime, "Enabling the Webcil bundle must recompile the composite image.");
        }

        // Component stubs share the per-assembly <name>.wasm output names, so a mode switch must recompile them
        // instead of treating them as up-to-date per-assembly images that probe for a pruned composite owner.
        // Switch back afterwards so the caller still runs the composite app.
        private void AssertSwitchingCompositeModeRecompiles(ProjectInfo info, Configuration config, string extraArgs, string frameworkDir)
        {
            string coreLibImage = Path.Combine(GetObjSubDir(config, "R2R"), "System.Private.CoreLib.wasm");
            string ownerImage = Path.Combine(GetObjSubDir(config, "R2R"), "BlazorBasicTestApp.r2r.wasm");
            System.DateTime compositeTime = File.GetLastWriteTimeUtc(ownerImage);

            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: $"{extraArgs} -p:PublishReadyToRunComposite=false"));
            Assert.True(File.GetLastWriteTimeUtc(coreLibImage) > compositeTime,
                $"'{coreLibImage}' was not recompiled after switching from composite to per-assembly ReadyToRun.");
            Assert.Empty(Directory.EnumerateFiles(frameworkDir, "BlazorBasicTestApp.r2r.*"));
            Assert.Empty(Directory.EnumerateFiles(frameworkDir, "BlazorBasicTestApp.components.*"));
            AssertNoDuplicateAssemblies(frameworkDir);

            BlazorPublish(info, config, new PublishOptions(UseCache: false, ExtraMSBuildArgs: extraArgs));
            AssertCompositeReadyToRun(frameworkDir);
            Assert.Empty(Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm*"));
            AssertNoDuplicateAssemblies(frameworkDir);
        }

        // The boot config flags exactly the composite owner, delivered via coreAssembly under its crossgen2 name so
        // component stubs can probe for it; an assembly merely named *.r2r stays an ordinary, unflagged assembly.
        private void AssertCompositeReadyToRun(string frameworkDir, bool bundle = true)
        {
            AssetsData assets = (AssetsData)_provider.GetBootJson(_provider.GetBootConfigPath(frameworkDir)).resources;
            WebcilAsset composite = Assert.Single(assets.coreAssembly, asset => asset.isCompositeImage == true);
            Assert.Equal("BlazorBasicTestApp.r2r.wasm", composite.virtualPath);
            if (EnvironmentVariables.UseFingerprinting)
                Assert.Matches(@"^BlazorBasicTestApp\.r2r\.[a-z0-9]{10}\.wasm$", composite.name);
            Assert.DoesNotContain(assets.assembly, asset => asset.isCompositeImage == true);
            string compositePath = Path.Combine(frameworkDir, composite.name);
            Assert.True(GetReadyToRunTableSize(compositePath) > 0, $"Expected compiled methods in '{compositePath}'.");
            if (bundle)
            {
                WebcilAsset components = Assert.Single(assets.coreAssembly, asset => asset.isWebcilBundle == true);
                Assert.Equal("BlazorBasicTestApp.components.wasm", components.virtualPath);
                Assert.Equal(0, GetReadyToRunTableSize(Path.Combine(frameworkDir, components.name)));
                Assert.DoesNotContain(assets.coreAssembly, asset => asset.virtualPath?.StartsWith("System.Private.CoreLib", System.StringComparison.Ordinal) == true);
                Assert.DoesNotContain(assets.assembly, asset => asset.virtualPath == "R2rSuffixLibrary.r2r.wasm");
                Assert.Empty(Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm"));
            }
            else
            {
                Assert.DoesNotContain(assets.coreAssembly, asset => asset.isWebcilBundle == true);
                Assert.Contains(assets.coreAssembly, asset => asset.virtualPath?.StartsWith("System.Private.CoreLib", System.StringComparison.Ordinal) == true);
                Assert.Contains(assets.assembly, asset => asset.virtualPath == "R2rSuffixLibrary.r2r.wasm");
                Assert.NotEmpty(Directory.EnumerateFiles(frameworkDir, "System.Private.CoreLib*.wasm"));
            }
        }

        private sealed class BundleTestBuildEngine : IBuildEngine
        {
            public bool ContinueOnError => false;
            public int LineNumberOfTaskNode => 0;
            public int ColumnNumberOfTaskNode => 0;
            public string ProjectFileOfTaskNode => string.Empty;
            public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
                => throw new System.NotSupportedException();
            public void LogCustomEvent(CustomBuildEventArgs e) { }
            public void LogErrorEvent(BuildErrorEventArgs e) => Assert.Fail(e.Message ?? "Build error");
            public void LogMessageEvent(BuildMessageEventArgs e) { }
            public void LogWarningEvent(BuildWarningEventArgs e) => Assert.Fail(e.Message ?? "Build warning");
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
