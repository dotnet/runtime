// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class ReferenceNewAssemblyRebuildTest : NativeRebuildTestsBase
    {
        public ReferenceNewAssemblyRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void ReferenceNewAssembly(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_tasks_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink, invariant: invariant, buildArgs, id);

            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: false);
            pathsDict.UpdateTo(unchanged: true, "corebindings.o");
            if (!buildArgs.AOT) // relinking
                pathsDict.UpdateTo(unchanged: true, "driver-gen.c");

            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            string programText =
            @$"
                using System;
                using System.Text.Json;
                public class Test
                {{
                    public static int Main()
                    {{" +
             @"          string json = ""{ \""name\"": \""value\"" }"";" +
             @"          var jdoc = JsonDocument.Parse($""{json}"", new JsonDocumentOptions());" +
            @$"          Console.WriteLine($""json: {{jdoc}}"");
                        return 42;
                    }}
                }}";
            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText);

            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }
    }
}
