// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class InvariantGlobalizationTests : WasmTemplateTestsBase
    {
        public InvariantGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantGlobalizationTestData(bool aot)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .Where(item => !(item.ElementAt(0) is string config && config == "Debug" && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        // TODO: check that icu bits have been linked out
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false })]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ true })]
        public async Task AOT_InvariantGlobalization(string config, bool aot, bool? invariantGlobalization)
            => await TestInvariantGlobalization(config, aot, invariantGlobalization);

        // TODO: What else should we use to verify a relinked build?
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false })]
        public async Task RelinkingWithoutAOT(string config, bool aot, bool? invariantGlobalization)
            => await TestInvariantGlobalization(config, aot, invariantGlobalization, isNativeBuild: true);

        private async Task TestInvariantGlobalization(string config, bool aot, bool? invariantGlobalization, bool isNativeBuild = false)
        {
            string extraProperties = isNativeBuild ? "<WasmBuildNative>true</WasmBuildNative>" : "";
            if (invariantGlobalization != null)
                extraProperties = $"{extraProperties}<InvariantGlobalization>{invariantGlobalization}</InvariantGlobalization>";

            string prefix = $"invariant_{invariantGlobalization?.ToString() ?? "unset"}";
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", prefix, "App", extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "InvariantGlobalization.cs"));

            bool isPublish = true;
            // invariantGlobalization triggers native build
            isNativeBuild = isNativeBuild || invariantGlobalization == true;
            BuildTemplateProject(info,
                        new BuildProjectOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish, isNativeBuild: isNativeBuild),
                            IsPublish: isPublish,
                            GlobalizationMode: invariantGlobalization == true ? GlobalizationMode.Invariant : GlobalizationMode.Sharded
                        ));

            RunResult output = await RunForPublishWithWebServer(new(info.Configuration, TestScenario: "DotnetRun", ExpectedExitCode: 42));
            if (invariantGlobalization == true)
            {
                Assert.Contains(output.TestOutput, m => m.Contains("Could not create es-ES culture"));
                Assert.Contains(output.TestOutput, m => m.Contains("CurrentCulture.NativeName: Invariant Language (Invariant Country)"));
            }
            else
            {
                Assert.Contains(output.TestOutput, m => m.Contains("es-ES: Is Invariant LCID: False"));
                // ignoring the last line of the output which prints the current culture
            }
        }
    }
}
