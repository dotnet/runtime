// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor
{
    public class NoopNativeRebuildTest : BlazorWasmTestBase
    {
        public NoopNativeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BlazorNoopRebuild(string config)
        {
            string extraProperties = "<WasmBuildNative>true</WasmBuildNative>";
            ProjectInfo info = CopyTestAsset(config, aot: false, BasicTestApp, "blz_rebuild", extraProperties: extraProperties);
            BlazorBuild(info, isNativeBuild: true);
            string projectDir = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(info.ProjectFilePath)))!;
            File.Move(Path.Combine(s_buildEnv.LogRootPath, projectDir, $"{info.ProjectName}-build.binlog"),
                        Path.Combine(s_buildEnv.LogRootPath, projectDir, $"{info.ProjectName}-build-first.binlog"));

            string objDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm");
            var pathsDict = _provider.GetFilesTable(true, objDir);
            pathsDict.Remove("runtime-icall-table.h");
            var originalStat = _provider.StatFiles(pathsDict);

            // build again
            BlazorBuild(info, useCache: false, isNativeBuild: true);
            var newStat = _provider.StatFiles(pathsDict);

            _provider.CompareStat(originalStat, newStat, pathsDict);
        }


        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BlazorOnlyLinkRebuild(string config)
        {
            string extraProperties = "<WasmBuildNative>true</WasmBuildNative>";
            ProjectInfo info = CopyTestAsset(config, aot: false, BasicTestApp, "blz_relink", extraProperties: extraProperties);
            BlazorBuild(info, isNativeBuild: true, extraArgs: "-p:EmccLinkOptimizationFlag=-O2");
            string projectDir = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(info.ProjectFilePath)))!;
            File.Move(Path.Combine(s_buildEnv.LogRootPath, projectDir, $"{info.ProjectName}-build.binlog"),
                        Path.Combine(s_buildEnv.LogRootPath, projectDir, $"{info.ProjectName}-build-first.binlog"));

            string objDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm");
            var pathsDict = _provider.GetFilesTable(true, objDir);
            pathsDict.Remove("runtime-icall-table.h");
            pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js", "emcc-link.rsp");
            var originalStat = _provider.StatFiles(pathsDict);

            // build again
            BlazorBuild(info, useCache: false, isNativeBuild: true, extraArgs: "-p:EmccLinkOptimizationFlag=-O1");
            var newStat = _provider.StatFiles(pathsDict);

            _provider.CompareStat(originalStat, newStat, pathsDict);
        }
    }
}
