// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "" },
                        new object[] { /*cflags*/ "",                      /*ldflags*/ "/p:EmccExtraLDFlags=-g" },
                        new object[] { /*cflags*/ "/p:EmccExtraCFlags=-g", /*ldflags*/ "/p:EmccExtraLDFlags=-g" }
            ).WithRunHosts(RunHost.Chrome).UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ false)]
        [MemberData(nameof(FlagsChangesForNativeRelinkingData), parameters: /*aot*/ true)]
        public void ExtraEmccFlagsSetButNoRealChange(BuildArgs buildArgs, string extraCFlags, string extraLDFlags, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_flags_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: true, invariant: false, buildArgs, id);
            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            if (extraLDFlags.Length > 0)
                pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // Rebuild

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            string extraBuildArgs = $" {extraCFlags} {extraLDFlags}";
            string output = Rebuild(nativeRelink: true, invariant: false, buildArgs, id, extraBuildArgs: extraBuildArgs, verbosity: "normal");

            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));
            CompareStat(originalStat, newStat, pathsDict.Values);

            // cflags: pinvoke get's compiled, but doesn't overwrite pinvoke.o
            // and thus doesn't cause relinking
            AssertSubstring("pinvoke.c -> pinvoke.o", output, contains: extraCFlags.Length > 0);

            // ldflags: link step args change, so it should trigger relink
            AssertSubstring("Linking with emcc", output, contains: extraLDFlags.Length > 0);

            if (buildArgs.AOT)
            {
                // ExtraEmccLDFlags does not affect .bc files
                Assert.DoesNotContain("Compiling assembly bitcode files", output);
            }

            string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput,
                                contains: buildArgs.AOT);
        }
    }
}
