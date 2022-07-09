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
    public class FlagsChangeRebuildTest : NativeRebuildTestsBase
    {
        public FlagsChangeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
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
                pathsDict.UpdateTo(unchanged: false, "dotnet.wasm", "dotnet.js");

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

        public static IEnumerable<object?[]> FlagsOnlyChangeData(bool aot)
            => ConfigWithAOTData(aot, config: "Release").Multiply(
                        new object[] { /*cflags*/ "/p:EmccCompileOptimizationFlag=-O1", /*ldflags*/ "" },
                        new object[] { /*cflags*/ "",                                   /*ldflags*/ "/p:EmccLinkOptimizationFlag=-O0" }
            ).WithRunHosts(RunHost.Chrome).UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(FlagsOnlyChangeData), parameters: /*aot*/ false)]
        [MemberData(nameof(FlagsOnlyChangeData), parameters: /*aot*/ true)]
        public void OptimizationFlagChange(BuildArgs buildArgs, string cflags, string ldflags, RunHost host, string id)
        {
            // force _WasmDevel=false, so we don't get -O0
            buildArgs = buildArgs with { ProjectName = $"rebuild_flags_{buildArgs.Config}", ExtraBuildArgs = "/p:_WasmDevel=false" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: true, invariant: false, buildArgs, id);

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: false);
            pathsDict.UpdateTo(unchanged: true, mainAssembly, "icall-table.h", "pinvoke-table.h", "driver-gen.c");
            if (cflags.Length == 0)
                pathsDict.UpdateTo(unchanged: true, "pinvoke.o", "corebindings.o", "driver.o");

            pathsDict.Remove(mainAssembly);
            if (buildArgs.AOT)
            {
                // link optimization flag change affects .bc->.o files too, but
                // it might result in only *some* files being *changed,
                // so, don't check for those
                // Link optimization flag is set to Compile optimization flag, if unset
                // so, it affects .bc files too!
                foreach (string key in pathsDict.Keys.ToArray())
                {
                    if (key.EndsWith(".dll.bc", StringComparison.Ordinal) || key.EndsWith(".dll.o", StringComparison.Ordinal))
                        pathsDict.Remove(key);
                }
            }

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // Rebuild

            string output = Rebuild(nativeRelink: true, invariant: false, buildArgs, id, extraBuildArgs: $" {cflags} {ldflags}", verbosity: "normal");
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));
            CompareStat(originalStat, newStat, pathsDict.Values);

            string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
            AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput,
                                contains: buildArgs.AOT);
        }
    }
}
