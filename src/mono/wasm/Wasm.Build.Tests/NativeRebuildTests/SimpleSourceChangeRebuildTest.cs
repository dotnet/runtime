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
    public class SimpleSourceChangeRebuildTest : NativeRebuildTestsBase
    {
        public SimpleSourceChangeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        [ActiveIssue("File sizes don't match: dotnet.native.wasm size should be same as from obj/for-publish but is not")]
        public async void SimpleStringChangeInSource(Configuration config, bool aot, bool nativeRelink, bool invariant)
        {
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "rebuild_simple");  
            BuildPaths paths = await FirstNativeBuildAndRun(info, nativeRelink, invariant);

            string mainAssembly = $"{info.ProjectName}{ProjectProviderBase.WasmAssemblyExtension}";
            var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: true);
            pathsDict.UpdateTo(unchanged: false, mainAssembly);
            pathsDict.UpdateTo(unchanged: !aot, "dotnet.native.wasm", "dotnet.native.js");
        
            if (aot)
                pathsDict.UpdateTo(unchanged: false, $"{mainAssembly}.bc", $"{mainAssembly}.o");

            var originalStat = StatFiles(pathsDict);

            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "SimpleSourceChange.cs"));

            // Rebuild
            Rebuild(info, nativeRelink, invariant);
            var newStat = StatFiles(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new (info.Configuration, TestScenario: "DotnetRun", ExpectedExitCode: 55));
        }
    }
}
