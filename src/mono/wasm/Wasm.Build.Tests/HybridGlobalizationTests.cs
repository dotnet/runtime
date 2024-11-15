// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class HybridGlobalizationTests : WasmTemplateTestsBase
    {
        // FOR REVIEWER:
        // This file will be deteled as a part of HybridGlobalization removal, so issues don't have to get logged

        public HybridGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> HybridGlobalizationTestData(bool aot)
            => ConfigWithAOTData(aot).UnwrapItemsAsArrays();

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        // Expected to find /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Release/net9.0/linux-x64/wbt artifacts/hybrid_Debug_False_g2xwxpxr_lus_鿀蜒枛遫䡫煉/obj/Debug/net9.0/wasm/for-build/dotnet.globalization.js
        [ActiveIssue("dotnet.globalization.js not found")]
        public async Task AOT_HybridGlobalizationTests(string config, bool aot)
            => await TestHybridGlobalizationTests(config, aot);

        [Theory]
        [BuildAndRun(aot: false)]
        [ActiveIssue("dotnet.globalization.js not found")]
        public async Task RelinkingWithoutAOT(string config, bool aot)
            => await TestHybridGlobalizationTests(config, aot, isNativeBuild: true);

        private async Task TestHybridGlobalizationTests(string config, bool aot, bool isNativeBuild = false)
        {
            string extraProperties = $"<HybridGlobalization>true</HybridGlobalization>";
            if (isNativeBuild)
                extraProperties += "<WasmBuildNative>true</WasmBuildNative>";

            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, "hybrid", extraProperties: extraProperties);
            ReplaceFile("Program.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "HybridGlobalization.cs"));
            UpdateBrowserMainJs();

            bool isPublish = true;
            BuildProject(info,
                        new BuildOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish, isNativeBuild: isNativeBuild),
                            IsPublish: isPublish,
                            GlobalizationMode: GlobalizationMode.Hybrid
                        ));

            RunResult output = await RunForPublishWithWebServer(new(info.Configuration, ExpectedExitCode: 42));
            Assert.Contains(output.TestOutput, m => m.Contains("HybridGlobalization works, thrown exception as expected"));
        }
    }
}
