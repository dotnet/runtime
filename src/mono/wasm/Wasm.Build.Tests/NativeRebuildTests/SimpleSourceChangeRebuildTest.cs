// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    public class SimpleSourceChangeRebuildTest : NativeRebuildTestsBase
    {
        public SimpleSourceChangeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public async Task SimpleStringChangeInSourceAsync(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_simple_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = await FirstNativeBuildAsync(s_mainReturns42, nativeRelink, invariant: invariant, buildArgs, id);

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            var pathsDict = _provider.GetFilesTable(buildArgs, paths, unchanged: true);
            pathsDict.UpdateTo(unchanged: false, mainAssembly);
            pathsDict.UpdateTo(unchanged: !buildArgs.AOT, "dotnet.native.wasm", "dotnet.native.js");

            if (buildArgs.AOT)
                pathsDict.UpdateTo(unchanged: false, $"{mainAssembly}.bc", $"{mainAssembly}.o");

            var originalStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            // Changes
            string mainResults55 = @"
                public class TestClass {
                    public static int Main()
                    {
                        return 55;
                    }
                }";
            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), mainResults55);

            // Rebuild
            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            _provider.CompareStat(originalStat, newStat, pathsDict.Values);
            await RunAndTestWasmAppAsync(buildArgs, buildDir: _projectDir, expectedExitCode: 55, host: host, id: id);
        }
    }
}
