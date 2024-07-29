// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests;

public class OptimizationFlagChangeTests : NativeRebuildTestsBase
{
    public OptimizationFlagChangeTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static IEnumerable<object?[]> FlagsOnlyChangeData(bool aot)
        => ConfigWithAOTData(aot, config: "Release").Multiply(
                    new object[] { /*cflags*/ "/p:EmccCompileOptimizationFlag=-O1", /*ldflags*/ "" },
                    new object[] { /*cflags*/ "",                                   /*ldflags*/ "/p:EmccLinkOptimizationFlag=-O1" }
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
        var pathsDict = _provider.GetFilesTable(buildArgs, paths, unchanged: false);
        pathsDict.UpdateTo(unchanged: true, mainAssembly, "icall-table.h", "pinvoke-table.h", "driver-gen.c");
        if (cflags.Length == 0)
            pathsDict.UpdateTo(unchanged: true, "pinvoke.o", "corebindings.o", "driver.o", "runtime.o");

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

        var originalStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

        // Rebuild

        string output = Rebuild(nativeRelink: true, invariant: false, buildArgs, id, extraBuildArgs: $" {cflags} {ldflags}", verbosity: "normal");
        var newStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));
        _provider.CompareStat(originalStat, newStat, pathsDict.Values);

        string runOutput = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        TestUtils.AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput,
                            contains: buildArgs.AOT);
    }
}
