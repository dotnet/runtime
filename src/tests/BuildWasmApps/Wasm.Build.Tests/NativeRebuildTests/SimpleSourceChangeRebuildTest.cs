// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class SimpleSourceChangeRebuildTest : NativeRebuildTestsBase
    {
        public SimpleSourceChangeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void SimpleStringChangeInSource(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_simple_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink, invariant: invariant, buildArgs, id);

            string mainAssembly = $"{buildArgs.ProjectName}.dll";
            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            pathsDict.UpdateTo(unchanged: false, mainAssembly);
            pathsDict.UpdateTo(unchanged: !buildArgs.AOT, "dotnet.wasm", "dotnet.js");

            if (buildArgs.AOT)
                pathsDict.UpdateTo(unchanged: false, $"{mainAssembly}.bc", $"{mainAssembly}.o");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

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
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 55, host: host, id: id);
        }
    }
}
