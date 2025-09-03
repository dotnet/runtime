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

        [Theory]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ false)]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ true)]
        public async Task ExtraEmccFlagsSetButNoRealChange(Configuration config, bool aot, string extraCFlags, string extraLDFlags)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild_flags");
            BuildPaths paths = await FirstNativeBuildAndRun(info, config, aot, requestNativeRelink: true, invariant: false);
            var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: true);
            if (extraLDFlags.Length != 0 || extraCFlags.Length != 0)
                pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js");

            if (extraCFlags.Length != 0)
                pathsDict.UpdateTo(unchanged: false, "driver.o", "runtime.o", "corebindings.o", "pinvoke.o");

            var originalStat = StatFiles(pathsDict);

            // Rebuild
            string mainAssembly = $"{info.ProjectName}.dll";
            string extraBuildArgs = $" {extraCFlags} {extraLDFlags}";
            string output = Rebuild(info, config, aot, requestNativeRelink: true, invariant: false, extraBuildArgs: extraBuildArgs, assertAppBundle: dotnetNativeFilesUnchanged);

            var newStat = StatFilesAfterRebuild(pathsDict);
            CompareStat(originalStat, newStat, pathsDict);
            
            // cflags: pinvoke get's compiled, but doesn't overwrite pinvoke.o
            // and thus doesn't cause relinking
            TestUtils.AssertSubstring("pinvoke.c -> pinvoke.o", output, contains: extraCFlags.Length > 0);

            // ldflags or cflags: link step args change, so it should trigger relink
            TestUtils.AssertSubstring("Linking with emcc", output, contains: extraLDFlags.Length > 0 || extraCFlags.Length > 0);

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
