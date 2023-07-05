// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    public class NoopNativeRebuildTest : NativeRebuildTestsBase
    {
        public NoopNativeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void NoOpRebuildForNativeBuilds(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_noop_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: nativeRelink, invariant: invariant, buildArgs, id);

            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BlazorNoopRebuild(string config)
        {
            string id = $"blz_rebuild_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateBlazorWasmTemplateProject(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            string objDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm");

            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
            File.Move(Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build.binlog"),
                        Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build-first.binlog"));

            var pathsDict = GetFilesTable(true, objDir);
            pathsDict.Remove("runtime-icall-table.h");
            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // build again
            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
        }


        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BlazorOnlyLinkRebuild(string config)
        {
            string id = $"blz_relink_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateBlazorWasmTemplateProject(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            string objDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm");

            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked), "-p:EmccLinkOptimizationFlag=-O2");
            File.Move(Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build.binlog"),
                        Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build-first.binlog"));

            var pathsDict = GetFilesTable(true, objDir);
            pathsDict.Remove("runtime-icall-table.h");
            pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js", "emcc-link.rsp");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // build again
            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked), "-p:EmccLinkOptimizationFlag=-O1");
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
        }
    }
}
