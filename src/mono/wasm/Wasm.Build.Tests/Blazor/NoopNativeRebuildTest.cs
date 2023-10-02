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
            string id = $"blz_rebuild_{config}_{GetRandomId()}";
            string projectFile = CreateBlazorWasmTemplateProject(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            string objDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm");

            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
            File.Move(Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build.binlog"),
                        Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build-first.binlog"));

            var pathsDict = _provider.GetFilesTable(true, objDir);
            pathsDict.Remove("runtime-icall-table.h");
            var originalStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // build again
            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
            var newStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            _provider.CompareStat(originalStat, newStat, pathsDict.Values);
        }


        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BlazorOnlyLinkRebuild(string config)
        {
            string id = $"blz_relink_{config}_{GetRandomId()}";
            string projectFile = CreateBlazorWasmTemplateProject(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            string objDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFrameworkForBlazor, "wasm");

            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked), "-p:EmccLinkOptimizationFlag=-O2");
            File.Move(Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build.binlog"),
                        Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-build-first.binlog"));

            var pathsDict = _provider.GetFilesTable(true, objDir);
            pathsDict.Remove("runtime-icall-table.h");
            pathsDict.UpdateTo(unchanged: false, "dotnet.native.wasm", "dotnet.native.js", "emcc-link.rsp");

            var originalStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // build again
            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked), "-p:EmccLinkOptimizationFlag=-O1");
            var newStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            _provider.CompareStat(originalStat, newStat, pathsDict.Values);
        }
    }
}
