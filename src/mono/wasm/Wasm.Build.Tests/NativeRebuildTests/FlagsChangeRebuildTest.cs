// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;
using System.Linq;
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
            => ConfigWithAOTData(aot, config: "Release").Multiply(
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "" }
                        // File sizes don't match: dotnet.native.wasm size should be same as from obj/for-publish but is not
                        // new object[] { /*cflags*/ "",                      /*ldflags*/ "/p:EmccExtraLDFlags=-g" }
                        // new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "/p:EmccExtraLDFlags=-g" }
            ).UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ false)]
        // Found statically linked AOT module: failed
        // [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ true)]
        public async void ExtraEmccFlagsSetButNoRealChange(string config, bool aot, string extraCFlags, string extraLDFlags)
        {
            string prefix = $"rebuild_flags_{config}";
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, prefix);
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();
            BuildPaths paths = await FirstNativeBuildAndRun(info, nativeRelink: true, invariant: false);
            var pathsDict = GetFilesTable(info, paths, unchanged: true);
            if (extraLDFlags.Length > 0)
                pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js");

            var originalStat = StatFiles(pathsDict);

            // Rebuild
            string mainAssembly = $"{info.ProjectName}.dll";
            string extraBuildArgs = $" {extraCFlags} {extraLDFlags}";
            string output = Rebuild(info, nativeRelink: true, invariant: false, extraBuildArgs: extraBuildArgs, verbosity: "normal");
            
            pathsDict = GetFilesTable(info, paths, unchanged: true);
            var newStat = StatFiles(pathsDict);
            CompareStat(originalStat, newStat, pathsDict);
            
            // cflags: pinvoke get's compiled, but doesn't overwrite pinvoke.o
            // and thus doesn't cause relinking
            TestUtils.AssertSubstring("pinvoke.c -> pinvoke.o", output, contains: extraCFlags.Length > 0);
            
            // ldflags: link step args change, so it should trigger relink
            TestUtils.AssertSubstring("Linking with emcc", output, contains: extraLDFlags.Length > 0);
            
            if (info.AOT)
            {
                // ExtraEmccLDFlags does not affect .bc files
                Assert.DoesNotContain("Compiling assembly bitcode files", output);
            }
            
            RunResult runOutput = await RunForPublishWithWebServer(new (info.Configuration, ExpectedExitCode: 42));
            TestUtils.AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput.TestOutput,
                                contains: info.AOT);
        }
    }
}
