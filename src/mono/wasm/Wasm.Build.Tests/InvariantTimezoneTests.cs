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
    public class InvariantTimezoneTests : WasmTemplateTestsBase
    {
        public InvariantTimezoneTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantTimezoneTestData(bool aot)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false, })]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ true })]
        public async Task AOT_InvariantTimezone(Configuration config, bool aot, bool? invariantTimezone)
            => await TestInvariantTimezone(config, aot, invariantTimezone);

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false })]
        public async Task RelinkingWithoutAOT(Configuration config, bool aot, bool? invariantTimezone)
            => await TestInvariantTimezone(config, aot, invariantTimezone, isNativeBuild: true);

        private async Task TestInvariantTimezone(Configuration config, bool aot, bool? invariantTimezone, bool? isNativeBuild = null)
        {
            string extraProperties = isNativeBuild == true ? "<WasmBuildNative>true</WasmBuildNative>" : "";
            if (invariantTimezone != null)
            {
                extraProperties = $"{extraProperties}<InvariantTimezone>{invariantTimezone}</InvariantTimezone>";
            }
            if (invariantTimezone == true)
            {
                if (isNativeBuild == false)
                    throw new System.ArgumentException("InvariantTimezone=true requires a native build");
                // -p:InvariantTimezone=true triggers native build, isNativeBuild is not undefined anymore
                isNativeBuild = true;
            }

            string prefix = $"invariant_{invariantTimezone?.ToString() ?? "unset"}";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "InvariantTimezone.cs"));
            PublishProject(info, config, isNativeBuild: isNativeBuild);

            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42));
            Assert.Contains(output.TestOutput, m => m.Contains("UTC BaseUtcOffset is 0"));
            if (invariantTimezone == true)
            {
                Assert.Contains(output.TestOutput, m => m.Contains("Could not find Asia/Tokyo"));
            }
            else
            {
                Assert.Contains(output.TestOutput, m => m.Contains("Asia/Tokyo BaseUtcOffset is 09:00:00"));
            }
        }
    }
}
