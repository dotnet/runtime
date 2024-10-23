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
                    // ToDo: File sizes don't match: dotnet.native.wasm size should be same as from obj/for-publish but is not
                    new object[] { /*cflags*/ "/p:EmccCompileOptimizationFlag=-O1", /*ldflags*/ "" },
                    new object[] { /*cflags*/ "",                                   /*ldflags*/ "/p:EmccLinkOptimizationFlag=-O1" }
        ).UnwrapItemsAsArrays();

    [Theory]
    [MemberData(nameof(FlagsOnlyChangeData), parameters: /*aot*/ false)]
    [MemberData(nameof(FlagsOnlyChangeData), parameters: /*aot*/ true)]
    public async void OptimizationFlagChange(string config, bool aot, string cflags, string ldflags)
    {
        string prefix = $"rebuild_flags_{config}";
        ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, prefix);
        UpdateBrowserProgramFile();
        UpdateBrowserMainJs();
        
        // force _WasmDevel=false, so we don't get -O0
        BuildPaths paths = await FirstNativeBuildAndRun(info, nativeRelink: true, invariant: false, extraBuildArgs: "/p:_WasmDevel=false");

        string mainAssembly = $"{info.ProjectName}{ProjectProviderBase.WasmAssemblyExtension}";
        var pathsDict = GetFilesTable(info, paths, unchanged: false);
        pathsDict.UpdateTo(unchanged: true, mainAssembly, "icall-table.h", "pinvoke-table.h", "driver-gen.c");
        if (cflags.Length == 0)
            pathsDict.UpdateTo(unchanged: true, "pinvoke.o", "corebindings.o", "driver.o", "runtime.o");

        pathsDict.Remove(mainAssembly);
        if (info.AOT)
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

        string output = Rebuild(info, nativeRelink: true, invariant: false, extraBuildArgs: $" {cflags} {ldflags}", verbosity: "normal");
        var newStat = StatFiles(pathsDict);
        CompareStat(originalStat, newStat, pathsDict);

        string runOutput = await RunForPublishWithWebServer(new (info.Configuration, ExpectedExitCode: 42));
        TestUtils.AssertSubstring($"Found statically linked AOT module '{Path.GetFileNameWithoutExtension(mainAssembly)}'", runOutput,
                            contains: info.AOT);
    }
}
