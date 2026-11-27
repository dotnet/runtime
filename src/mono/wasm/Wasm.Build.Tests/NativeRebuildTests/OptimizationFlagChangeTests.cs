// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests;

[TestCategory("native")]
public class OptimizationFlagChangeTests : NativeRebuildTestsBase
{
    public OptimizationFlagChangeTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static IEnumerable<object?[]> FlagsOnlyChangeData(bool aot)
        => ConfigWithAOTData(aot, config: Configuration.Release).Multiply(
                    new object[] { /*cflags*/ "/p:EmccCompileOptimizationFlag=-O1", /*ldflags*/ "" },
                    new object[] { /*cflags*/ "",                                   /*ldflags*/ "/p:EmccLinkOptimizationFlag=-O1" }
        ).UnwrapItemsAsArrays();

    public static IEnumerable<object?[]> FlagsOnlyChangeDataForCurrentRuntime()
        => FlagsOnlyChangeData(aot: false);

    [Theory]
    [MemberData(nameof(FlagsOnlyChangeDataForCurrentRuntime))]
    public async Task OptimizationFlagChange(Configuration config, bool aot, string cflags, string ldflags)
    {
        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild_flags");
        // Use the runtime's default optimization level instead of the -O0 used for rebuild tests.
        string optElevationArg = "/p:_WasmDevel=false";
        BuildPaths paths = await FirstNativeBuildAndRun(info, config, aot, requestNativeRelink: true, invariant: false, extraBuildArgs: optElevationArg);

        string mainAssembly = $"{info.ProjectName}{ProjectProviderBase.WasmAssemblyExtension}";
        var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: false);
        pathsDict.UpdateTo(unchanged: true,
            mainAssembly,
            "callhelpers-generator.rsp",
            "callhelpers-interp-to-managed.cpp",
            "callhelpers-pinvoke.cpp",
            "callhelpers-reverse.cpp");
        if (cflags.Length == 0)
        {
            pathsDict.UpdateTo(unchanged: true,
                "callhelpers-interp-to-managed.o",
                "callhelpers-pinvoke.o",
                "callhelpers-reverse.o",
                "emcc-compile-generated.rsp");
        }
        else
        {
            // Recompilation can produce identical bytes at both optimization levels.
            pathsDict.Remove("callhelpers-interp-to-managed.o");
            pathsDict.UpdateTo(unchanged: true, "emcc-link.rsp");
        }

        pathsDict.Remove(mainAssembly);
        if (aot)
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
        var originalStat = StatFiles(pathsDict);

        // Rebuild
        string output = Rebuild(info,
                                config,
                                aot,
                                requestNativeRelink: true,
                                invariant: false,
                                extraBuildArgs: $" {cflags} {ldflags} {optElevationArg}",
                                assertAppBundle: false); // optimization flags change changes the size of dotnet.native.wasm
        var newStat = StatFilesAfterRebuild(pathsDict);
        CompareStat(originalStat, newStat, pathsDict);

        TestUtils.AssertSubstring("callhelpers-interp-to-managed.cpp -> callhelpers-interp-to-managed.o",
            output, contains: cflags.Length > 0);

        RunResult runOutput = await RunForPublishWithWebServer(new BrowserRunOptions(config, aot, TestScenario: "DotnetRun"));
        TestUtils.AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput.ConsoleOutput,
                            contains: aot);
    }
}
