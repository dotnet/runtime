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
                .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        // TODO: check that icu bits have been linked out
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false })]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ true })]
        public async Task AOT_InvariantGlobalization(Configuration config, bool aot, bool? invariantGlobalization)
            => await TestInvariantGlobalization(config, aot, invariantGlobalization);

        // TODO: What else should we use to verify a relinked build?
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false })]
        public async Task RelinkingWithoutAOT(Configuration config, bool aot, bool? invariantGlobalization)
            => await TestInvariantGlobalization(config, aot, invariantGlobalization, isNativeBuild: true);

        private async Task TestInvariantGlobalization(Configuration config, bool aot, bool? invariantGlobalization, bool? isNativeBuild = null)
        {
            string extraProperties = isNativeBuild == true ? "<WasmBuildNative>true</WasmBuildNative>" : "";
            if (invariantGlobalization != null)
            {
                extraProperties = $"{extraProperties}<InvariantGlobalization>{invariantGlobalization}</InvariantGlobalization>";
            }
            if (invariantGlobalization == true)
            {
                if (isNativeBuild == false)
                    throw new System.ArgumentException("InvariantGlobalization=true requires a native build");
                // -p:InvariantGlobalization=true triggers native build, isNativeBuild is not undefined anymore
                isNativeBuild = true;
            }

            string prefix = $"invariant_{invariantGlobalization?.ToString() ?? "unset"}";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "InvariantGlobalization.cs"));

            var globalizationMode = invariantGlobalization == true ? GlobalizationMode.Invariant : GlobalizationMode.Sharded;
            PublishProject(info, config, new PublishOptions(GlobalizationMode: globalizationMode, AOT: aot), isNativeBuild: isNativeBuild);

            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42));
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
