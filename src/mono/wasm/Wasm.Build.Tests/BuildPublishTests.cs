// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BuildPublishTests : WasmTemplateTestsBase
    {
        public BuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [InlineData(false)]
        [InlineData(true)]
        [TestCategory("no-workload")]
        public async Task SharedMemoryIsAppOwnedAndOptIn(bool nativeRelink)
        {
            const Configuration Config = Configuration.Debug;
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, Config, aot: false, "shared_opt_in",
                extraProperties: "<PublishReadyToRun>true</PublishReadyToRun><WasmFingerprintAssets>true</WasmFingerprintAssets>",
                extraItems: $"""
                    <PackageReference Include="Microsoft.NET.Runtime.WebAssembly.Sdk"
                                      Version="{s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework)}"
                                      GeneratePathProperty="true" PrivateAssets="all" />
                    """,
                insertAtEnd: """
                    <Import Project="$(PkgMicrosoft_NET_Runtime_WebAssembly_Sdk)\Sdk\Sdk.targets"
                            Condition="'$(WasmBuildNative)' == 'true' and '$(PkgMicrosoft_NET_Runtime_WebAssembly_Sdk)' != ''" />
                    """);
            UpdateFile("Program.cs", """
                using System.Runtime.InteropServices.JavaScript;
                using System.Threading.Tasks;

                internal static partial class Program
                {
                    public static int Main() => 0;
                    [JSImport("increment", "test")]
                    internal static partial int Import(int value);
                    [JSImport("delayed", "test")]
                    internal static partial Task<int> Delayed(int value);
                    [JSExport]
                    internal static int Increment(int value) => value + 1;
                    [JSExport]
                    internal static int ThroughImport(int value) => Import(value);
                    [JSExport]
                    internal static Task<int> ThroughAsyncImport(int value) => Delayed(value);
                }
                """);
            UpdateFile(Path.Combine("wwwroot", "main.js"), $$"""
                import { dotnet, exit } from "./_framework/dotnet.js";
                const shared = new URLSearchParams(location.search).get("shared") === "true";
                const check = (value, message) => { if (!value) throw new Error(message); };
                const builder = dotnet.withConfig({ logExitCode: true, exitOnUnhandledError: true });
                if (shared) builder.withWorker();
                const runtime = await builder.create();
                check(crossOriginIsolated === shared, "Unexpected isolation mode");
                const config = runtime.getConfig();
                for (const asset of [config.resources.wasmNative[0], config.resources.coreAssembly[0]]) {
                    const url = asset.resolvedUrl ?? new URL("_framework/" + asset.name, document.baseURI).href;
                    const response = await fetch(url);
                    check(response.ok, "Asset fetch failed");
                    const bytes = await response.arrayBuffer();
                    const digest = new Uint8Array(await crypto.subtle.digest("SHA-256", bytes));
                    const hash = "sha256-" + btoa(String.fromCharCode(...digest));
                    check(hash === asset.hash, "Integrity does not describe deployed bytes");
                }
                runtime.setModuleImports("test", {
                    increment: value => value + 1,
                    delayed: async value => value + 1
                });
                const exports = (await runtime.getAssemblyExports("{{info.ProjectName}}")).Program;
                check(exports.Increment(41) === 42, "Scalar export failed");
                check(exports.ThroughImport(41) === 42, "Scalar import failed");
                const results = await Promise.all(Array.from({ length: 70 }, (_, value) => exports.ThroughAsyncImport(value)));
                check(results.every((value, index) => value === index + 1), "Async import failed");
                if (shared) {
                    check(runtime.INTERNAL.workerMemoryViews.getViews().u8.buffer instanceof SharedArrayBuffer, "Memory is not shared");
                } else {
                    check(runtime.localHeapViewU8().buffer instanceof ArrayBuffer, "Ordinary memory is shared");
                }
                console.log("TestOutput -> Opt-in scalar worker interop passed");
                const code = await runtime.runMainAndExit();
                exit(code);
                """);

            foreach (bool shared in new[] { true, false, true })
            {
                string args = $"-p:WasmEnableSharedMemory={shared.ToString().ToLowerInvariant()} -p:WasmBuildNative={nativeRelink.ToString().ToLowerInvariant()} -p:UsingBrowserRuntimeWorkload=false";
                // Shared copies intentionally differ from runtime-pack/relink bytes; verify the live memory below.
                BuildProject(info, Config, new BuildOptions(UseCache: false, AssertAppBundle: !shared,
                    ExtraMSBuildArgs: args + " -p:WasmBuildOnlyAfterPublish=false"), isNativeBuild: nativeRelink);
                PublishProject(info, Config, new PublishOptions(UseCache: false, AssertAppBundle: !shared, ExtraMSBuildArgs: args), isNativeBuild: nativeRelink);
                RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(Config,
                    BrowserQueryString: new System.Collections.Specialized.NameValueCollection { ["shared"] = shared ? "true" : "false" },
                    ExtraArgs: shared ? "--web-server-use-cop" : ""));
                Assert.Contains("Opt-in scalar worker interop passed", result.TestOutput);
            }
        }

        [Theory]
        [BuildAndRun(config: Configuration.Debug, aot: true)]
        [TestCategory("native"), TestCategory("mono")]
        public void Wasm_CannotAOT_InDebug(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "no_aot_in_debug");
            (string _, string buildOutput) = PublishProject(info, config, new PublishOptions(AOT: aot, ExpectSuccess: false));
            Assert.Contains("AOT is not supported in debug configuration", buildOutput);
        }

        [Theory]
        [BuildAndRun(config: Configuration.Release)]
        [BuildAndRun(config: Configuration.Debug)]
        public async Task BuildThenPublishNoAOT(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "build_publish");
            BuildProject(info, config);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun");
            await RunForBuildWithDotnetRun(runOptions);

            PublishProject(info, config, new PublishOptions(UseCache: false));
            await RunForPublishWithWebServer(runOptions);
        }

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native"), TestCategory("mono")]
        public async Task BuildThenPublishWithAOT(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "build_publish");
            
            bool isPublish = false;
            (_, string output) = BuildProject(info, config, new BuildOptions(Label: "first_build", AOT: aot), isNativeBuild: aot);
            
            BuildPaths paths = GetBuildPaths(config, forPublish: isPublish);
            // With CopyToOutputDirectory=Never, framework files aren't copied to bin/_framework/
            // during build. They live in obj subdirs: dotnet.native.* in obj/wasm/for-build/
            // (for native rebuilds like Release+AOT), and JS/source maps in obj/{config}/{tfm}/fx/{source-id}/_framework/.
            // The boot config (dotnet.js) is at obj/{config}/{tfm}/dotnet.js.
            string fxFrameworkDir = WasmSdkBasedProjectProvider.GetMaterializedFrameworkDir(paths.ObjDir);

            BuildPaths buildObjPaths = paths with { BinFrameworkDir = fxFrameworkDir };
            IDictionary<string, (string fullPath, bool unchanged)> pathsDict =
                GetFilesTable(info.ProjectName, aot, buildObjPaths, unchanged: false, bootConfigDir: paths.ObjDir);

            // dotnet.native.* are produced by the native rebuild into obj/wasm/for-build/ using
            // their canonical (non-fingerprinted) names — fingerprinting is applied later when
            // publishing to bin.
            foreach (var nativeName in new[] { "dotnet.native.wasm", "dotnet.native.js" })
            {
                if (pathsDict.TryGetValue(nativeName, out var entry))
                {
                    pathsDict[nativeName] = (Path.Combine(paths.ObjWasmDir, nativeName), entry.unchanged);
                }
            }

            // dotnet.runtime.js lives in the materialized fx dir under its canonical (non-fingerprinted)
            // name during build — fingerprinting is applied later when publishing to bin. GetFilesTable
            // rewrites this entry to the fingerprinted name (from the boot config, which already reflects
            // the publish layout), so override it back to the unfingerprinted obj/fx path.
            if (pathsDict.TryGetValue("dotnet.runtime.js", out var runtimeJsEntry))
            {
                pathsDict["dotnet.runtime.js"] = (Path.Combine(fxFrameworkDir, "dotnet.runtime.js"), runtimeJsEntry.unchanged);
            }
            
            string mainDll = $"{info.ProjectName}.dll";
            var firstBuildStat = StatFiles(pathsDict);
            Assert.True(firstBuildStat["pinvoke.o"].Exists);
            Assert.False(firstBuildStat[$"{mainDll}.bc"].Exists);

            CheckOutputForNativeBuild(expectAOT: false, expectRelinking: isPublish || aot, info.ProjectName, output);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun");
            await RunForBuildWithDotnetRun(runOptions);

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));
    
            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            // relink by default for Release+publish
            isPublish = true;
            (_, output) = PublishProject(info, config, new PublishOptions(Label: "first_publish", UseCache: false, AOT: aot));
            
            // publish has different paths ("for-publish", not "for-build")
            paths = GetBuildPaths(config, forPublish: isPublish);
            pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: false);  
            IDictionary<string, FileStat> publishStat = StatFiles(pathsDict);
            Assert.True(publishStat["pinvoke.o"].Exists);
            Assert.True(publishStat[$"{mainDll}.bc"].Exists);
            CheckOutputForNativeBuild(expectAOT: true, expectRelinking: isPublish || aot, info.ProjectName, output);
            
            // source maps are created for build but not for publish, make sure CompareStat won't expect them in publish:
            pathsDict["dotnet.js.map"] = (pathsDict["dotnet.js.map"].fullPath, unchanged: false);
            pathsDict["dotnet.runtime.js.map"] = (pathsDict["dotnet.runtime.js.map"].fullPath, unchanged: false);
            CompareStat(firstBuildStat, publishStat, pathsDict);
            await RunForPublishWithWebServer(runOptions);

            // second build
            isPublish = false;
            (_, output) = BuildProject(info, config, new BuildOptions(Label: "second_build", UseCache: false, AOT: aot), isNativeBuild: aot);
            var secondBuildStat = StatFiles(pathsDict);
            
            // no relinking, or AOT
            CheckOutputForNativeBuild(expectAOT: false, expectRelinking: isPublish || aot, info.ProjectName, output);

            // no native files changed
            pathsDict.UpdateTo(unchanged: true);
            CompareStat(publishStat, secondBuildStat, pathsDict);
        }

        void CheckOutputForNativeBuild(bool expectAOT, bool expectRelinking, string projectName, string buildOutput)
        {
            TestUtils.AssertMatches(@$"{projectName}.dll -> {projectName}.dll.bc", buildOutput, contains: expectAOT);
            TestUtils.AssertMatches(@$"{projectName}.dll.bc -> {projectName}.dll.o", buildOutput, contains: expectAOT);
            TestUtils.AssertMatches("pinvoke.c -> pinvoke.o", buildOutput, contains: expectRelinking || expectAOT);
        }
    }
}
