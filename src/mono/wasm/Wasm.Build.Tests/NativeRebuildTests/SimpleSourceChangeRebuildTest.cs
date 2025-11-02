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
        public async Task SimpleStringChangeInSource(Configuration config, bool aot, bool nativeRelink, bool invariant)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild_simple");
            BuildPaths paths = await FirstNativeBuildAndRun(info, config, aot, nativeRelink, invariant);

            string mainAssembly = $"{info.ProjectName}{ProjectProviderBase.WasmAssemblyExtension}";
            var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: true);
            pathsDict.UpdateTo(unchanged: false, mainAssembly);
            bool dotnetFilesSizeUnchanged = !aot;
            pathsDict.UpdateTo(unchanged: dotnetFilesSizeUnchanged, "dotnet.native.wasm", "dotnet.native.js");
        
            if (aot)
                pathsDict.UpdateTo(unchanged: false, $"{info.ProjectName}.dll.bc", $"{info.ProjectName}.dll.o");

            var originalStat = StatFiles(pathsDict);

            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "SimpleSourceChange.cs"));

            // Rebuild
            Rebuild(info, config, aot, nativeRelink, invariant, assertAppBundle: dotnetFilesSizeUnchanged);
            var newStat = StatFilesAfterRebuild(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 55));
        }
    }
}
