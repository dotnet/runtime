// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    [TestCategory("native")]
    public class FlagsChangeRebuildTests : NativeRebuildTestsBase
    {
        public FlagsChangeRebuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> FlagsChangesForNativeRelinkingData(bool aot)
            => ConfigWithAOTData(aot, config: Configuration.Release).Multiply(
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "" },
                        new object[] { /*cflags*/ "",                      /*ldflags*/ "/p:EmccExtraLDFlags=-g" },
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "/p:EmccExtraLDFlags=-g" }
            ).UnwrapItemsAsArrays();

        public static IEnumerable<object?[]> FlagsChangesForCurrentRuntime()
        {
            IEnumerable<object?[]> data = FlagsChangesForNativeRelinkingData(aot: false);
            return IsCoreClrRuntime ? data : data.Concat(FlagsChangesForNativeRelinkingData(aot: true));
        }

        [Theory]
        [MemberData(nameof(FlagsChangesForCurrentRuntime))]
        public async Task ExtraEmccFlagsSetButNoRealChange(Configuration config, bool aot, string extraCFlags, string extraLDFlags)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild_flags");
            BuildPaths paths = await FirstNativeBuildAndRun(info, config, aot, requestNativeRelink: true, invariant: false);
            var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: true);
            // With these defaults, Mono already compiles with -g; CoreCLR does not.
            bool dotnetNativeFilesUnchanged = extraLDFlags.Length == 0 && !(IsCoreClrRuntime && extraCFlags.Length > 0);
            if (!dotnetNativeFilesUnchanged)
            {
                pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js");
                if (IsCoreClrRuntime)
                    pathsDict.UpdateTo(unchanged: false, "dotnet.native.js.symbols");
            }

            if (IsCoreClrRuntime)
            {
                if (extraCFlags.Length > 0)
                {
                    pathsDict.UpdateTo(unchanged: false,
                        "callhelpers-interp-to-managed.o",
                        "callhelpers-pinvoke.o",
                        "callhelpers-reverse.o",
                        "emcc-compile-generated.rsp");
                }
                if (extraLDFlags.Length > 0)
                    pathsDict.UpdateTo(unchanged: false, "emcc-link.rsp");
            }

            var originalStat = StatFiles(pathsDict);

            // Rebuild
            string mainAssembly = $"{info.ProjectName}.dll";
            string extraBuildArgs = $" {extraCFlags} {extraLDFlags}";
            string output = Rebuild(info, config, aot, requestNativeRelink: true, invariant: false, extraBuildArgs: extraBuildArgs, assertAppBundle: dotnetNativeFilesUnchanged);

            var newStat = StatFilesAfterRebuild(pathsDict);
            CompareStat(originalStat, newStat, pathsDict);

            // check that emscripten emulator for sockets and pipe was trimmed from dotnet.native.js
            if (config == Configuration.Release)
            {
                Assert.True(newStat.TryGetValue("dotnet.native.js", out var dotnetNativeJsStat));
                var dotnetNativeJs = File.ReadAllText(dotnetNativeJsStat.FullPath);
                Assert.DoesNotContain("var SOCKFS", dotnetNativeJs);
                Assert.DoesNotContain("var PIPEFS", dotnetNativeJs);
            }

            string pinvokeCompileMessage = IsCoreClrRuntime
                ? "callhelpers-pinvoke.cpp -> callhelpers-pinvoke.o"
                : "pinvoke.c -> pinvoke.o";
            TestUtils.AssertSubstring(pinvokeCompileMessage, output, contains: extraCFlags.Length > 0);

            // ldflags: link step args change, so it should trigger relink
            TestUtils.AssertSubstring("Linking with emcc", output, contains: !dotnetNativeFilesUnchanged);
            if (aot)
            {
                // ExtraEmccLDFlags does not affect .bc files
                Assert.DoesNotContain("Compiling assembly bitcode files", output);
            }
            
            RunResult runOutput = await RunForPublishWithWebServer(new BrowserRunOptions(config, aot, TestScenario: "DotnetRun"));
            TestUtils.AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput.ConsoleOutput,
                                contains: aot);
        }
    }
}
